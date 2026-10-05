using ERP.Api.Dtos;
using ERP.Services;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Compras,CuentasPorPagar")]
[Route("api/pedidos-compra")]
public class PedidosCompraController(
    AppDbContext db, IFolioService folioService, IInventarioService inventario, IPedidoCompraPdfService pdfService,
    IAuditoriaService auditoria)
    : ControllerBase
{
    [HttpGet]
    public async Task<List<PedidoCompra>> GetAll() =>
        await db.PedidosCompra
            .Include(p => p.Proveedor)
            .Include(p => p.Almacen)
            .Include(p => p.Pagos)
            .OrderByDescending(p => p.FechaUtc)
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoCompra>> GetById(int id)
    {
        var pedido = await db.PedidosCompra
            .AsSplitQuery()
            .Include(p => p.Proveedor)
            .Include(p => p.Almacen)
            .Include(p => p.Items).ThenInclude(i => i.Producto)
            .Include(p => p.Pagos)
            .FirstOrDefaultAsync(p => p.Id == id);

        return pedido is null ? NotFound() : pedido;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Compras")]
    public async Task<ActionResult<PedidoCompra>> Create(CrearPedidoCompraRequest request)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("La orden necesita al menos una partida.");
        }

        if (request.Items.Any(i => i.Cantidad <= 0 || i.CostoUnitario < 0))
        {
            return BadRequest("La cantidad de cada partida debe ser mayor a cero y el costo no puede ser negativo.");
        }

        var items = await ArmarItemsAsync(request.Tipo, request.Items);
        if (items is null)
        {
            return BadRequest(request.Tipo == TipoPedidoCompra.Inventario
                ? "Alguno de los productos no existe."
                : "Cada partida de un gasto interno necesita una descripción.");
        }

        var pedido = new PedidoCompra
        {
            Folio = await folioService.GenerarFolioPedidoCompraAsync(),
            ProveedorId = request.ProveedorId,
            AlmacenId = request.Tipo == TipoPedidoCompra.Inventario ? request.AlmacenId : null,
            Tipo = request.Tipo,
            Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim(),
            Total = items.Sum(i => i.Importe),
            Items = items
        };

        db.PedidosCompra.Add(pedido);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear", "PedidoCompra", pedido.Id, pedido.Folio);
        return await GetById(pedido.Id);
    }

    // Reemplaza por completo las partidas de una orden todavía no recibida -- ver la misma idea
    // en PedidosVentaController.ActualizarItems.
    [HttpPut("{id:int}/items")]
    [Authorize(Roles = "Admin,Compras")]
    public async Task<ActionResult<PedidoCompra>> ActualizarItems(int id, List<ItemCompraRequest> items)
    {
        if (items.Count == 0)
        {
            return BadRequest("La orden necesita al menos una partida.");
        }

        if (items.Any(i => i.Cantidad <= 0 || i.CostoUnitario < 0))
        {
            return BadRequest("La cantidad de cada partida debe ser mayor a cero y el costo no puede ser negativo.");
        }

        var pedido = await db.PedidosCompra.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null)
        {
            return NotFound();
        }

        if (pedido.Estatus != EstatusPedidoCompra.Solicitado)
        {
            return BadRequest("Solo se pueden editar las partidas de una orden todavía no recibida.");
        }

        var nuevosItems = await ArmarItemsAsync(pedido.Tipo, items);
        if (nuevosItems is null)
        {
            return BadRequest(pedido.Tipo == TipoPedidoCompra.Inventario
                ? "Alguno de los productos no existe."
                : "Cada partida de un gasto interno necesita una descripción.");
        }

        db.PedidoCompraItems.RemoveRange(pedido.Items);
        pedido.Items = nuevosItems;
        pedido.Total = pedido.Items.Sum(i => i.Importe);

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Editar partidas", "PedidoCompra", pedido.Id, pedido.Folio);
        return await GetById(id);
    }

    // Null = alguna partida no pasó la validación de su tipo (producto inexistente para
    // Inventario, o descripción vacía para GastoInterno) -- el caller decide el mensaje 400.
    private async Task<List<PedidoCompraItem>?> ArmarItemsAsync(TipoPedidoCompra tipo, List<ItemCompraRequest> items)
    {
        if (tipo == TipoPedidoCompra.Inventario)
        {
            var idsValidos = await db.Productos
                .Where(p => items.Select(i => i.ProductoId).Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();
            if (items.Any(i => i.ProductoId is not int pid || !idsValidos.Contains(pid)))
            {
                return null;
            }

            return items.Select(i => new PedidoCompraItem
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                CostoUnitario = i.CostoUnitario,
                Importe = i.Cantidad * i.CostoUnitario
            }).ToList();
        }

        if (items.Any(i => string.IsNullOrWhiteSpace(i.Descripcion)))
        {
            return null;
        }

        return items.Select(i => new PedidoCompraItem
        {
            Descripcion = i.Descripcion!.Trim(),
            Cantidad = i.Cantidad,
            CostoUnitario = i.CostoUnitario,
            Importe = i.Cantidad * i.CostoUnitario
        }).ToList();
    }

    [HttpPost("{id:int}/recibir")]
    [Authorize(Roles = "Admin,Compras")]
    public async Task<ActionResult<PedidoCompra>> Recibir(int id)
    {
        var pedido = await db.PedidosCompra.Include(p => p.Items).Include(p => p.Proveedor).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null)
        {
            return NotFound();
        }

        // Un gasto interno (laptop, papelería, servicios) nunca toca inventario -- "Recibir" ahí
        // solo marca la orden como cerrada/pagable, sin costeo ni movimiento de almacén.
        if (pedido.Tipo == TipoPedidoCompra.Inventario)
        {
            var almacen = await inventario.ResolverAlmacenAsync(pedido.AlmacenId);
            foreach (var item in pedido.Items)
            {
                // El costeo se aplica ANTES de registrar la entrada -- necesita la existencia
                // total previa a esta recepción para ponderar correctamente.
                await inventario.AplicarCosteoPromedioAsync(item.ProductoId!.Value, item.Cantidad, item.CostoUnitario);
                await inventario.RegistrarMovimientoAsync(
                    item.ProductoId!.Value, almacen.Id, TipoMovimiento.Entrada, item.Cantidad, pedido.Folio,
                    $"Compra a {pedido.Proveedor.Nombre}");
            }
        }

        pedido.Estatus = EstatusPedidoCompra.Recibido;
        pedido.RecibidoUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Recibir", "PedidoCompra", pedido.Id, pedido.Folio);
        return await GetById(id);
    }

    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = "Admin,Compras")]
    public async Task<ActionResult<PedidoCompra>> Cancelar(int id)
    {
        var pedido = await db.PedidosCompra.FindAsync(id);
        if (pedido is null)
        {
            return NotFound();
        }

        pedido.Estatus = EstatusPedidoCompra.Cancelado;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Cancelar", "PedidoCompra", pedido.Id, pedido.Folio);
        return await GetById(id);
    }

    [HttpPost("{id:int}/pagos")]
    public async Task<ActionResult<PedidoCompra>> RegistrarPago(int id, RegistrarPagoRequest request)
    {
        var pedido = await db.PedidosCompra.Include(p => p.Pagos).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null)
        {
            return NotFound();
        }

        var saldo = pedido.Total - pedido.Pagos.Sum(p => p.Monto);
        if (request.Monto <= 0 || request.Monto > saldo)
        {
            return BadRequest("Monto inválido.");
        }

        db.PagosProveedor.Add(new PagoProveedor
        {
            PedidoCompraId = id,
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
}
