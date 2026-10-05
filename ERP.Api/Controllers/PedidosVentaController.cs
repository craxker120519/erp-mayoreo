using ERP.Api.Dtos;
using ERP.Services;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Ventas,CuentasPorCobrar")]
[Route("api/pedidos-venta")]
public class PedidosVentaController(
    AppDbContext db, IFolioService folioService, IInventarioService inventario, IPedidoVentaPdfService pdfService,
    IAuditoriaService auditoria)
    : ControllerBase
{
    // AsNoTracking() de buena práctica en un GET de solo lectura. AsSplitQuery() es la parte que
    // de verdad importa -- con 3 colecciones incluidas (Pagos, CreditoRecibido, DocumentosGenerados)
    // en una sola consulta, EF Core arma un solo JOIN gigante cuyo producto cartesiano puede
    // materializar mal algunas filas (un `null` colándose dentro de una colección que sí tenía
    // Count > 0) -- causó un crash real en PedidoVentaDetalle.razor al recorrer la cadena de
    // documentos. Ver el comentario más completo en CargarConDetalle() más abajo.
    [HttpGet]
    public async Task<List<PedidoVenta>> GetAll() =>
        await db.PedidosVenta
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Cliente)
            .Include(p => p.Pagos)
            .Include(p => p.CreditoRecibido)
            .Include(p => p.DocumentoOrigen)
            .Include(p => p.DocumentosGenerados)
            .OrderByDescending(p => p.FechaUtc)
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoVenta>> GetById(int id)
    {
        var pedido = await CargarConDetalle(id);
        return pedido is null ? NotFound() : pedido;
    }

    // Nueva cotización -- el punto de entrada de la cadena. Cotización/Pedido/Remisión son
    // documentos separados con folio propio (COT/PV/REM), encadenados por DocumentoOrigenId --
    // ver ConvertirAPedido/Remisionar más abajo, que crean el siguiente documento en vez de
    // mutar este. Así cada etapa se conserva tal como quedó, aunque la venta avance.
    [HttpPost]
    [Authorize(Roles = "Admin,Ventas")]
    public async Task<ActionResult<PedidoVenta>> Create(CrearPedidoVentaRequest request)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("El pedido necesita al menos un producto.");
        }

        if (request.Items.Any(i => i.Cantidad <= 0))
        {
            return BadRequest("La cantidad de cada producto debe ser mayor a cero.");
        }

        var productos = await db.Productos
            .Where(p => request.Items.Select(i => i.ProductoId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (request.Items.Any(i => !productos.ContainsKey(i.ProductoId)))
        {
            return BadRequest("Alguno de los productos no existe.");
        }

        var items = ArmarItems(request.Items, productos);

        var pedido = new PedidoVenta
        {
            Folio = await folioService.GenerarFolioCotizacionAsync(),
            ClienteId = request.ClienteId,
            AlmacenId = request.AlmacenId,
            Estatus = EstatusPedidoVenta.Cotizacion,
            Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim(),
            Total = items.Sum(i => i.Importe),
            Items = items
        };

        db.PedidosVenta.Add(pedido);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear", "PedidoVenta", pedido.Id, pedido.Folio);
        return await GetById(pedido.Id);
    }

    // Reemplaza por completo las partidas de una cotización o pedido que todavía no avanzó a la
    // siguiente etapa -- para corregir un error de captura sin tener que cancelar y rehacer todo.
    // Una vez que ya se convirtió/remisionó (tiene DocumentosGenerados), queda congelado -- ya no
    // se edita, el documento nuevo es el que sigue vivo.
    [HttpPut("{id:int}/items")]
    [Authorize(Roles = "Admin,Ventas")]
    public async Task<ActionResult<PedidoVenta>> ActualizarItems(int id, List<ItemRequest> items)
    {
        if (items.Count == 0)
        {
            return BadRequest("El pedido necesita al menos un producto.");
        }

        if (items.Any(i => i.Cantidad <= 0))
        {
            return BadRequest("La cantidad de cada producto debe ser mayor a cero.");
        }

        var pedido = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Items)
            .Include(p => p.DocumentosGenerados)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null)
        {
            return NotFound();
        }

        if (pedido.Estatus is not (EstatusPedidoVenta.Cotizacion or EstatusPedidoVenta.Pedido))
        {
            return BadRequest("Solo se pueden editar las partidas de una cotización o pedido todavía no remisionado.");
        }

        if (pedido.DocumentosGenerados.Count > 0)
        {
            return BadRequest("Este documento ya avanzó a la siguiente etapa -- edita el documento nuevo, este quedó como historial.");
        }

        var productos = await db.Productos
            .Where(p => items.Select(i => i.ProductoId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        if (items.Any(i => !productos.ContainsKey(i.ProductoId)))
        {
            return BadRequest("Alguno de los productos no existe.");
        }

        db.PedidoVentaItems.RemoveRange(pedido.Items);
        pedido.Items = ArmarItems(items, productos);
        pedido.Total = pedido.Items.Sum(i => i.Importe);

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Editar partidas", "PedidoVenta", pedido.Id, pedido.Folio);
        return await GetById(id);
    }

    // Crea el documento PEDIDO a partir de una cotización vigente -- no muta la cotización, la deja
    // intacta como historial (queda marcada como "avanzada" en cuanto este nuevo documento existe).
    [HttpPost("{id:int}/convertir-a-pedido")]
    [Authorize(Roles = "Admin,Ventas")]
    public async Task<ActionResult<PedidoVenta>> ConvertirAPedido(int id)
    {
        var origen = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Items)
            .Include(p => p.DocumentosGenerados)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (origen is null)
        {
            return NotFound();
        }

        if (origen.Estatus != EstatusPedidoVenta.Cotizacion)
        {
            return BadRequest("Solo una cotización vigente se puede convertir a pedido.");
        }

        if (origen.DocumentosGenerados.Count > 0)
        {
            return BadRequest("Esta cotización ya se convirtió a pedido.");
        }

        var pedido = new PedidoVenta
        {
            Folio = await folioService.GenerarFolioPedidoVentaAsync(),
            ClienteId = origen.ClienteId,
            AlmacenId = origen.AlmacenId,
            Estatus = EstatusPedidoVenta.Pedido,
            Nota = origen.Nota,
            Total = origen.Total,
            DocumentoOrigenId = origen.Id,
            Items = origen.Items.Select(i => new PedidoVentaItem
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Importe = i.Importe,
                CostoUnitario = i.CostoUnitario
            }).ToList()
        };

        db.PedidosVenta.Add(pedido);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear (desde cotización)", "PedidoVenta", pedido.Id, pedido.Folio);
        return await GetById(pedido.Id);
    }

    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = "Admin,Ventas")]
    public async Task<ActionResult<PedidoVenta>> Cancelar(int id)
    {
        var pedido = await db.PedidosVenta.Include(p => p.DocumentosGenerados).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null)
        {
            return NotFound();
        }

        if (pedido.Estatus is not (EstatusPedidoVenta.Cotizacion or EstatusPedidoVenta.Pedido))
        {
            return BadRequest("Solo se puede cancelar una cotización o pedido todavía no remisionado.");
        }

        if (pedido.DocumentosGenerados.Count > 0)
        {
            return BadRequest("Este documento ya avanzó a la siguiente etapa -- cancela el documento nuevo si hace falta.");
        }

        pedido.Estatus = EstatusPedidoVenta.Cancelado;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Cancelar", "PedidoVenta", pedido.Id, pedido.Folio);
        return await GetById(id);
    }

    // Crea el documento REMISIÓN a partir de un pedido vigente -- aquí es donde de verdad se
    // valida crédito/stock y se descuenta inventario (igual que antes), solo que el resultado
    // queda en un documento nuevo y encadenado, no sobre el mismo pedido.
    [HttpPost("{id:int}/remisionar")]
    [Authorize(Roles = "Admin,Ventas")]
    public async Task<ActionResult<PedidoVenta>> Remisionar(int id)
    {
        var origen = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Items)
            .Include(p => p.Cliente)
            .Include(p => p.DocumentosGenerados)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (origen is null)
        {
            return NotFound();
        }

        if (origen.Estatus != EstatusPedidoVenta.Pedido)
        {
            return BadRequest("Solo un pedido vigente se puede remisionar.");
        }

        if (origen.DocumentosGenerados.Count > 0)
        {
            return BadRequest("Este pedido ya se remisionó.");
        }

        if (origen.Cliente.LimiteCredito > 0)
        {
            // Solo cuenta el documento ACTIVO de cada hilo (sin DocumentosGenerados) -- si se
            // sumaran también cotización/pedido/remisión de una misma venta que ya avanzó, se
            // contaría el mismo compromiso 2 o 3 veces.
            var pedidosCliente = await db.PedidosVenta
                .AsSplitQuery()
                .Where(p => p.ClienteId == origen.ClienteId && p.Estatus != EstatusPedidoVenta.Cancelado
                    && !p.DocumentosGenerados.Any())
                .Include(p => p.Pagos)
                .Include(p => p.CreditoRecibido)
                .ToListAsync();

            var saldoTotalCliente = pedidosCliente.Sum(p =>
                p.Total - p.Pagos.Sum(x => x.Monto) - p.CreditoRecibido.Sum(a => a.Monto));

            if (saldoTotalCliente > origen.Cliente.LimiteCredito)
            {
                return BadRequest(
                    $"Este pedido dejaría al cliente con {saldoTotalCliente:C} de saldo pendiente, arriba de su límite de crédito ({origen.Cliente.LimiteCredito:C}).");
            }
        }

        var almacen = await inventario.ResolverAlmacenAsync(origen.AlmacenId);

        var existencias = await db.Existencias
            .Where(e => e.AlmacenId == almacen.Id && origen.Items.Select(i => i.ProductoId).Contains(e.ProductoId))
            .ToDictionaryAsync(e => e.ProductoId, e => e.Cantidad);

        var faltantes = origen.Items
            .Where(i => i.Cantidad > existencias.GetValueOrDefault(i.ProductoId))
            .ToList();

        if (faltantes.Count > 0)
        {
            var productosFaltantes = await db.Productos
                .Where(p => faltantes.Select(f => f.ProductoId).Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);
            var detalle = string.Join("; ", faltantes.Select(f =>
                $"{productosFaltantes[f.ProductoId].Nombre} (pide {f.Cantidad}, disponible {existencias.GetValueOrDefault(f.ProductoId)})"));
            return BadRequest($"Stock insuficiente en {almacen.Nombre}: {detalle}.");
        }

        var remision = new PedidoVenta
        {
            Folio = await folioService.GenerarFolioRemisionAsync(),
            ClienteId = origen.ClienteId,
            AlmacenId = origen.AlmacenId,
            Estatus = EstatusPedidoVenta.Remisionado,
            Nota = origen.Nota,
            Total = origen.Total,
            DocumentoOrigenId = origen.Id,
            Items = origen.Items.Select(i => new PedidoVentaItem
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Importe = i.Importe,
                CostoUnitario = i.CostoUnitario
            }).ToList()
        };

        db.PedidosVenta.Add(remision);
        await db.SaveChangesAsync();

        foreach (var item in remision.Items)
        {
            await inventario.RegistrarMovimientoAsync(
                item.ProductoId, almacen.Id, TipoMovimiento.Salida, item.Cantidad, remision.Folio,
                $"Venta a {origen.Cliente.Nombre}");
        }

        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear (desde pedido)", "PedidoVenta", remision.Id, remision.Folio);
        return await GetById(remision.Id);
    }

    // Los pagos y el crédito aplicado solo tienen sentido contra una Remisión (es el documento que
    // de verdad representa mercancía entregada) -- una cotización o pedido todavía no es una deuda.
    [HttpPost("{id:int}/pagos")]
    public async Task<ActionResult<PedidoVenta>> RegistrarPago(int id, RegistrarPagoRequest request)
    {
        var pedido = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Pagos)
            .Include(p => p.CreditoRecibido)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null)
        {
            return NotFound();
        }

        if (pedido.Estatus != EstatusPedidoVenta.Remisionado)
        {
            return BadRequest("Solo se puede registrar un pago contra una remisión.");
        }

        var saldo = pedido.Total
            - pedido.Pagos.Sum(p => p.Monto)
            - pedido.CreditoRecibido.Sum(a => a.Monto);
        if (request.Monto <= 0 || request.Monto > saldo)
        {
            return BadRequest("Monto inválido.");
        }

        db.Pagos.Add(new Pago
        {
            PedidoVentaId = id,
            Monto = request.Monto,
            MetodoPago = string.IsNullOrWhiteSpace(request.MetodoPago) ? null : request.MetodoPago.Trim()
        });
        await db.SaveChangesAsync();
        return await GetById(id);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var pedido = (await GetById(id)).Value;
        if (pedido is null)
        {
            return NotFound();
        }

        var pdf = pdfService.Generar(pedido);
        return File(pdf, "application/pdf", $"{pedido.Folio}.pdf");
    }

    private static List<PedidoVentaItem> ArmarItems(List<ItemRequest> items, Dictionary<int, Producto> productos) =>
        items.Select(i => new PedidoVentaItem
        {
            ProductoId = i.ProductoId,
            Cantidad = i.Cantidad,
            PrecioUnitario = productos[i.ProductoId].Precio,
            Importe = i.Cantidad * productos[i.ProductoId].Precio,
            CostoUnitario = productos[i.ProductoId].Costo
        }).ToList();

    // Incluye 2 niveles de la cadena en cada dirección -- suficiente para el caso real (máximo
    // Cotización -> Pedido -> Remisión, 3 eslabones) sin importar desde cuál se esté consultando.
    // AsNoTracking()/AsSplitQuery() son buena práctica aquí pero NO evitan el problema real: Pedido
    // y Cotización se referencian mutuamente (Pedido.DocumentoOrigen == Cotización,
    // Cotización.DocumentosGenerados contiene a Pedido) -- es un ciclo genuino en el grafo de
    // objetos, no un efecto secundario de tracking o de un JOIN cartesiano. System.Text.Json
    // (ReferenceHandler.IgnoreCycles) lo rompe reemplazando el elemento REPETIDO DENTRO de la
    // colección por `null` (no la colección completa), así que `DocumentoOrigen.DocumentosGenerados`
    // podía traer `[null]` con `Count == 1` -- `CadenaConMarcaActual()` en la app tronaba al hacer
    // `.First().Id` sobre ese null. Por eso se computa `Cadena` aparte, aplanada, sin ciclos.
    private async Task<PedidoVenta?> CargarConDetalle(int id)
    {
        var pedido = await db.PedidosVenta
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Cliente)
            .Include(p => p.Almacen)
            .Include(p => p.Items).ThenInclude(i => i.Producto)
            .Include(p => p.Pagos)
            .Include(p => p.Facturas)
            .Include(p => p.NotasCredito).ThenInclude(n => n.Items).ThenInclude(i => i.Producto)
            .Include(p => p.NotasCredito).ThenInclude(n => n.Aplicaciones)
            .Include(p => p.CreditoRecibido).ThenInclude(a => a.NotaCredito)
            .Include(p => p.DocumentoOrigen).ThenInclude(o => o!.DocumentoOrigen)
            .Include(p => p.DocumentosGenerados).ThenInclude(g => g.DocumentosGenerados)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido is not null)
        {
            pedido.Cadena = ComputarCadena(pedido);
        }

        return pedido;
    }

    // Se calcula ANTES de que System.Text.Json entre en juego -- aquí el grafo de objetos en
    // memoria todavía tiene las referencias reales (no hay "null" de por medio, eso solo pasa
    // durante la serialización), así que recorrerlo en C# es seguro.
    private static List<PedidoVentaCadenaItem> ComputarCadena(PedidoVenta pedido)
    {
        var raiz = pedido;
        while (raiz.DocumentoOrigen is not null)
        {
            raiz = raiz.DocumentoOrigen;
        }

        var cadena = new List<PedidoVentaCadenaItem>();
        var actual = raiz;
        cadena.Add(new PedidoVentaCadenaItem { Id = actual.Id, Folio = actual.Folio, Estatus = actual.Estatus, FechaUtc = actual.FechaUtc });
        while (actual.DocumentosGenerados.Count > 0)
        {
            actual = actual.DocumentosGenerados.First();
            cadena.Add(new PedidoVentaCadenaItem { Id = actual.Id, Folio = actual.Folio, Estatus = actual.Estatus, FechaUtc = actual.FechaUtc });
        }

        return cadena;
    }
}
