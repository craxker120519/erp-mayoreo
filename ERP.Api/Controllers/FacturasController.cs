using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Facturacion")]
[Route("api/facturas")]
public class FacturasController(
    AppDbContext db, IFolioService folioService, IFacturaPdfService pdfService, IAuditoriaService auditoria,
    IInventarioService inventario)
    : ControllerBase
{
    // Ver el comentario en CargarConDetalle: Include(f.PedidoVenta) hace que cada fila termine con
    // esa MISMA Factura dentro de PedidoVenta.Facturas -- se limpia por la misma razón.
    [HttpGet]
    public async Task<List<Factura>> GetAll()
    {
        var facturas = await db.Facturas
            .AsNoTracking()
            .Include(f => f.PedidoVenta).ThenInclude(p => p.Cliente)
            .OrderByDescending(f => f.FechaEmisionUtc)
            .ToListAsync();

        foreach (var factura in facturas)
        {
            factura.PedidoVenta.Facturas.Clear();
        }

        return facturas;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Factura>> GetById(int id)
    {
        var factura = await CargarConDetalle(id);
        return factura is null ? NotFound() : factura;
    }

    [HttpPost]
    public async Task<ActionResult<Factura>> Create(CrearFacturaRequest request)
    {
        var pedido = await db.PedidosVenta
            .Include(p => p.Items).ThenInclude(i => i.Producto)
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoVentaId);

        if (pedido is null)
        {
            return NotFound("Pedido no encontrado.");
        }

        if (pedido.Estatus != EstatusPedidoVenta.Remisionado)
        {
            return BadRequest("Solo se puede facturar un pedido ya remisionado.");
        }

        var yaFacturado = await db.Facturas.AnyAsync(f =>
            f.PedidoVentaId == pedido.Id && f.Estatus != EstatusFactura.Cancelada);
        if (yaFacturado)
        {
            return BadRequest("Este pedido ya tiene una factura vigente.");
        }

        const decimal tasaIva = 0.16m;
        var subtotal = pedido.Total;
        var iva = Math.Round(subtotal * tasaIva, 2);

        var factura = new Factura
        {
            Folio = await folioService.GenerarFolioFacturaAsync(),
            PedidoVentaId = pedido.Id,
            Subtotal = subtotal,
            TasaIva = tasaIva,
            Iva = iva,
            Total = subtotal + iva
        };

        db.Facturas.Add(factura);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear", "Factura", factura.Id, factura.Folio);
        return await GetById(factura.Id);
    }

    // Factura "directa" -- salta cotización/pedido/remisión manual: arma el PedidoVenta, corre las
    // MISMAS validaciones que el flujo normal (stock, límite de crédito), lo remisiona y factura en
    // una sola llamada. Sigue quedando un PedidoVenta detrás (Estatus=Remisionado) -- no es un
    // documento sin pedido, solo se salta la captura manual de cada paso. El pedido resultante se ve
    // igual que cualquier otro en Ventas mayoreo, con su línea de tiempo completa.
    [HttpPost("directa")]
    public async Task<ActionResult<Factura>> CrearDirecta(CrearFacturaDirectaRequest request)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("La factura necesita al menos un producto.");
        }

        if (request.Items.Any(i => i.Cantidad <= 0))
        {
            return BadRequest("La cantidad de cada producto debe ser mayor a cero.");
        }

        var cliente = await db.Clientes.FindAsync(request.ClienteId);
        if (cliente is null)
        {
            return BadRequest("El cliente no existe.");
        }

        var productos = await db.Productos
            .Where(p => request.Items.Select(i => i.ProductoId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        if (request.Items.Any(i => !productos.ContainsKey(i.ProductoId)))
        {
            return BadRequest("Alguno de los productos no existe.");
        }

        var items = request.Items.Select(i => new PedidoVentaItem
        {
            ProductoId = i.ProductoId,
            Cantidad = i.Cantidad,
            PrecioUnitario = productos[i.ProductoId].Precio,
            Importe = i.Cantidad * productos[i.ProductoId].Precio,
            CostoUnitario = productos[i.ProductoId].Costo
        }).ToList();
        var total = items.Sum(i => i.Importe);

        // Mismo chequeo de límite de crédito que PedidosVentaController.Remisionar (solo cuenta el
        // documento activo de cada hilo, para no contar 2 veces una venta que ya tiene cotización +
        // pedido + remisión), pero contando TAMBIÉN el total de esta factura nueva (todavía no
        // existe como PedidoVenta guardado).
        if (cliente.LimiteCredito > 0)
        {
            var pedidosCliente = await db.PedidosVenta
                .AsSplitQuery()
                .Where(p => p.ClienteId == cliente.Id && p.Estatus != EstatusPedidoVenta.Cancelado
                    && !p.DocumentosGenerados.Any())
                .Include(p => p.Pagos)
                .Include(p => p.CreditoRecibido)
                .ToListAsync();

            var saldoExistente = pedidosCliente.Sum(p =>
                p.Total - p.Pagos.Sum(x => x.Monto) - p.CreditoRecibido.Sum(a => a.Monto));

            if (saldoExistente + total > cliente.LimiteCredito)
            {
                return BadRequest(
                    $"Esta factura dejaría al cliente con {(saldoExistente + total):C} de saldo pendiente, arriba de su límite de crédito ({cliente.LimiteCredito:C}).");
            }
        }

        var almacen = await inventario.ResolverAlmacenAsync(request.AlmacenId);
        var existencias = await db.Existencias
            .Where(e => e.AlmacenId == almacen.Id && request.Items.Select(i => i.ProductoId).Contains(e.ProductoId))
            .ToDictionaryAsync(e => e.ProductoId, e => e.Cantidad);

        var faltantes = items
            .Where(i => i.Cantidad > existencias.GetValueOrDefault(i.ProductoId))
            .ToList();
        if (faltantes.Count > 0)
        {
            var detalle = string.Join("; ", faltantes.Select(f =>
                $"{productos[f.ProductoId].Nombre} (pide {f.Cantidad}, disponible {existencias.GetValueOrDefault(f.ProductoId)})"));
            return BadRequest($"Stock insuficiente en {almacen.Nombre}: {detalle}.");
        }

        var pedido = new PedidoVenta
        {
            Folio = await folioService.GenerarFolioRemisionAsync(),
            ClienteId = cliente.Id,
            AlmacenId = request.AlmacenId,
            Nota = "Factura directa -- sin cotización previa.",
            Total = total,
            Items = items,
            Estatus = EstatusPedidoVenta.Remisionado
        };

        db.PedidosVenta.Add(pedido);
        await db.SaveChangesAsync();

        foreach (var item in items)
        {
            await inventario.RegistrarMovimientoAsync(
                item.ProductoId, almacen.Id, TipoMovimiento.Salida, item.Cantidad, pedido.Folio,
                $"Venta directa (factura) a {cliente.Nombre}");
        }

        const decimal tasaIva = 0.16m;
        var subtotal = pedido.Total;
        var iva = Math.Round(subtotal * tasaIva, 2);
        var factura = new Factura
        {
            Folio = await folioService.GenerarFolioFacturaAsync(),
            PedidoVentaId = pedido.Id,
            Subtotal = subtotal,
            TasaIva = tasaIva,
            Iva = iva,
            Total = subtotal + iva
        };

        db.Facturas.Add(factura);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear (directa)", "PedidoVenta", pedido.Id, pedido.Folio);
        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear (directa)", "Factura", factura.Id, factura.Folio);
        return await GetById(factura.Id);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<Factura>> Cancelar(int id)
    {
        var factura = await db.Facturas.FindAsync(id);
        if (factura is null)
        {
            return NotFound();
        }

        factura.Estatus = EstatusFactura.Cancelada;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Cancelar", "Factura", factura.Id, factura.Folio);
        return await GetById(id);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var factura = await CargarConDetalle(id);
        if (factura is null)
        {
            return NotFound();
        }

        var pdf = pdfService.Generar(factura);
        return File(pdf, "application/pdf", $"{factura.Folio}.pdf");
    }

    // Al incluir f.PedidoVenta, EF hace fixup de relación DENTRO de esta misma consulta (pasa
    // aunque sea AsNoTracking -- eso solo evita el rastreo entre consultas distintas, no el fixup
    // que ocurre al armar ESTA) y termina poniendo esta MISMA Factura raíz dentro de
    // PedidoVenta.Facturas -- un ciclo genuino donde la raíz reaparece como elemento de su propio
    // array. ReferenceHandler.IgnoreCycles anula ESE elemento del array (no solo la propiedad),
    // dejando `facturas: [null]` en el JSON -- mismo bug de fondo que ya se cazó para
    // DocumentoOrigen/DocumentosGenerados en PedidosVentaController. Nada en la app hoy lee
    // pedidoVenta.facturas anidado desde una respuesta de Factura, así que se limpia esa colección
    // después de cargar en vez de arrastrar el riesgo -- más simple que aplanar a un DTO aparte.
    private async Task<Factura?> CargarConDetalle(int id)
    {
        var factura = await db.Facturas
            .AsNoTracking()
            .Include(f => f.PedidoVenta).ThenInclude(p => p.Cliente)
            .Include(f => f.PedidoVenta).ThenInclude(p => p.Items).ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(f => f.Id == id);

        factura?.PedidoVenta.Facturas.Clear();
        return factura;
    }
}
