using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Facturacion,CuentasPorCobrar")]
[Route("api/notas-credito")]
public class NotasCreditoController(
    AppDbContext db, IFolioService folioService, IInventarioService inventario, INotaCreditoPdfService pdfService,
    IAuditoriaService auditoria)
    : ControllerBase
{
    // Include(n.PedidoVenta) hace que el fixup de EF (dentro de esta misma consulta, no evitable
    // con AsNoTracking) meta esta MISMA NotaCredito dentro de PedidoVenta.NotasCredito -- ciclo
    // genuino que ReferenceHandler.IgnoreCycles resuelve anulando ese elemento del array
    // (`notasCredito: [null]`). Se limpia esa colección después de cargar; nada la usa anidada.
    [HttpGet]
    [Authorize(Roles = "Admin,Facturacion")]
    public async Task<List<NotaCredito>> GetAll()
    {
        var notas = await db.NotasCredito
            .AsNoTracking()
            .AsSplitQuery()
            .Include(n => n.PedidoVenta).ThenInclude(p => p.Cliente)
            .Include(n => n.Items).ThenInclude(i => i.Producto)
            .Include(n => n.Aplicaciones)
            .OrderByDescending(n => n.FechaUtc)
            .ToListAsync();

        foreach (var nota in notas)
        {
            nota.PedidoVenta.NotasCredito.Clear();
        }

        return notas;
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Facturacion,CuentasPorCobrar")]
    public async Task<ActionResult<NotaCredito>> GetById(int id)
    {
        var nota = await CargarConDetalle(id);
        return nota is null ? NotFound() : nota;
    }

    // Notas con crédito todavía sin aplicar (Disponible > 0) del mismo cliente que `clienteId` --
    // usado para ofrecer "aplicar crédito" al ver/pagar CUALQUIER pedido de ese cliente, no solo
    // el pedido de origen de la nota.
    [HttpGet("disponibles-cliente/{clienteId:int}")]
    public async Task<List<NotaCredito>> DisponiblesCliente(int clienteId)
    {
        var notas = await db.NotasCredito
            .AsNoTracking()
            .Include(n => n.PedidoVenta)
            .Include(n => n.Aplicaciones)
            .Where(n => n.PedidoVenta.ClienteId == clienteId && n.Estatus == EstatusNotaCredito.Emitida)
            .ToListAsync();

        foreach (var nota in notas)
        {
            nota.PedidoVenta.NotasCredito.Clear();
        }

        return notas.Where(n => n.Disponible > 0).OrderBy(n => n.FechaUtc).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Facturacion")]
    public async Task<ActionResult<NotaCredito>> Create(CrearNotaCreditoRequest request)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("La nota de crédito necesita al menos un producto.");
        }

        var pedido = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Items)
            .Include(p => p.NotasCredito).ThenInclude(n => n.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoVentaId);

        if (pedido is null)
        {
            return NotFound("Pedido no encontrado.");
        }

        if (pedido.Estatus != EstatusPedidoVenta.Remisionado)
        {
            return BadRequest("Solo se puede generar una nota de crédito contra un pedido ya remisionado.");
        }

        var items = new List<NotaCreditoItem>();
        foreach (var solicitado in request.Items)
        {
            var itemOriginal = pedido.Items.FirstOrDefault(i => i.ProductoId == solicitado.ProductoId);
            if (itemOriginal is null)
            {
                return BadRequest($"El producto {solicitado.ProductoId} no forma parte de este pedido.");
            }

            var yaAcreditado = pedido.NotasCredito
                .Where(n => n.Estatus != EstatusNotaCredito.Cancelada)
                .SelectMany(n => n.Items)
                .Where(i => i.ProductoId == solicitado.ProductoId)
                .Sum(i => i.Cantidad);

            var disponible = itemOriginal.Cantidad - yaAcreditado;
            if (solicitado.Cantidad <= 0 || solicitado.Cantidad > disponible)
            {
                return BadRequest(
                    $"Cantidad inválida para {itemOriginal.Producto?.Nombre ?? "producto"}: disponible para acreditar {disponible}.");
            }

            items.Add(new NotaCreditoItem
            {
                ProductoId = solicitado.ProductoId,
                Cantidad = solicitado.Cantidad,
                PrecioUnitario = itemOriginal.PrecioUnitario,
                Importe = solicitado.Cantidad * itemOriginal.PrecioUnitario
            });
        }

        const decimal tasaIva = 0.16m;
        var subtotal = items.Sum(i => i.Importe);
        var iva = Math.Round(subtotal * tasaIva, 2);

        var nota = new NotaCredito
        {
            Folio = await folioService.GenerarFolioNotaCreditoAsync(),
            PedidoVentaId = pedido.Id,
            Motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim(),
            DevuelveInventario = request.DevuelveInventario,
            Subtotal = subtotal,
            TasaIva = tasaIva,
            Iva = iva,
            Total = subtotal + iva,
            Items = items
        };

        db.NotasCredito.Add(nota);
        await db.SaveChangesAsync();

        if (request.DevuelveInventario)
        {
            var almacen = await inventario.ResolverAlmacenAsync(pedido.AlmacenId);
            foreach (var item in items)
            {
                await inventario.RegistrarMovimientoAsync(
                    item.ProductoId, almacen.Id, TipoMovimiento.Entrada, item.Cantidad, nota.Folio, "Devolución por nota de crédito");
            }
        }

        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear", "NotaCredito", nota.Id, nota.Folio);
        return await GetById(nota.Id);
    }

    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = "Admin,Facturacion")]
    public async Task<ActionResult<NotaCredito>> Cancelar(int id)
    {
        var nota = await CargarConDetalle(id);
        if (nota is null)
        {
            return NotFound();
        }

        if (nota.Aplicaciones.Sum(a => a.Monto) > 0)
        {
            return BadRequest("Esta nota ya se aplicó como crédito a una compra -- no se puede cancelar.");
        }

        nota.Estatus = EstatusNotaCredito.Cancelada;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Cancelar", "NotaCredito", nota.Id, nota.Folio);
        return await GetById(id);
    }

    // Usa (parte de) el crédito disponible de esta nota como pago de OTRO pedido del mismo
    // cliente -- o del mismo pedido de origen, si todavía tiene saldo pendiente. "Usar la nota de
    // crédito en una compra nueva".
    [HttpPost("{id:int}/aplicar")]
    public async Task<ActionResult<NotaCredito>> Aplicar(int id, AplicarCreditoRequest request)
    {
        var nota = await CargarConDetalle(id);
        if (nota is null)
        {
            return NotFound();
        }

        if (nota.Estatus != EstatusNotaCredito.Emitida)
        {
            return BadRequest("La nota no está vigente.");
        }

        var pedidoDestino = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Pagos)
            .Include(p => p.CreditoRecibido)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoVentaId);
        if (pedidoDestino is null)
        {
            return NotFound("Pedido destino no encontrado.");
        }

        if (pedidoDestino.ClienteId != nota.PedidoVenta.ClienteId)
        {
            return BadRequest("Esta nota de crédito pertenece a otro cliente.");
        }

        if (pedidoDestino.Estatus == EstatusPedidoVenta.Cancelado)
        {
            return BadRequest("No se puede aplicar crédito a un pedido cancelado.");
        }

        var saldoPedido = pedidoDestino.Total
            - pedidoDestino.Pagos.Sum(p => p.Monto)
            - pedidoDestino.CreditoRecibido.Sum(a => a.Monto);

        if (request.Monto <= 0 || request.Monto > nota.Disponible || request.Monto > saldoPedido)
        {
            return BadRequest($"Monto inválido: crédito disponible {nota.Disponible:C}, saldo del pedido {saldoPedido:C}.");
        }

        db.NotaCreditoAplicaciones.Add(new NotaCreditoAplicacion
        {
            NotaCreditoId = id,
            PedidoVentaDestinoId = request.PedidoVentaId,
            Monto = request.Monto
        });
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Aplicar crédito", "NotaCredito", nota.Id,
            $"{request.Monto:C} aplicado a pedido #{request.PedidoVentaId}");

        return await GetById(id);
    }

    [HttpGet("{id:int}/pdf")]
    [Authorize(Roles = "Admin,Facturacion")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var nota = await CargarConDetalle(id);
        if (nota is null)
        {
            return NotFound();
        }

        var pdf = pdfService.Generar(nota);
        return File(pdf, "application/pdf", $"{nota.Folio}.pdf");
    }

    // Ver el comentario en GetAll: Include(n.PedidoVenta) hace que el fixup de esta misma consulta
    // meta esta NotaCredito dentro de PedidoVenta.NotasCredito -- se limpia por la misma razón.
    private async Task<NotaCredito?> CargarConDetalle(int id)
    {
        var nota = await db.NotasCredito
            .AsNoTracking()
            .AsSplitQuery()
            .Include(n => n.PedidoVenta).ThenInclude(p => p.Cliente)
            .Include(n => n.Items).ThenInclude(i => i.Producto)
            .Include(n => n.Aplicaciones)
            .FirstOrDefaultAsync(n => n.Id == id);

        nota?.PedidoVenta.NotasCredito.Clear();
        return nota;
    }
}
