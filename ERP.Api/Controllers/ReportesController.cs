using ERP.Data;
using ERP.Models;
using ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

public record ExistenciaReporte(string Producto, string Almacen, decimal Cantidad, decimal StockMinimo, string UnidadVenta);
public record VentaPorClienteReporte(string Cliente, int Pedidos, decimal Total);
public record VentaPorProductoReporte(string Producto, decimal Cantidad, decimal Total);
public record UtilidadReporte(string Producto, decimal Cantidad, decimal Ventas, decimal Costo, decimal Utilidad, decimal MargenPct);
public record CarteraVencidaReporte(string Folio, string Cliente, DateTime Fecha, DateTime Vence, decimal Saldo);
public record CuentasPorPagarReporte(string Folio, string Proveedor, DateTime Fecha, DateTime Vence, decimal Saldo);
public record KardexMovimiento(
    DateTime FechaUtc, string Almacen, TipoMovimiento Tipo, decimal Cantidad, decimal Saldo, string? Referencia, string? Nota);

[ApiController]
[Authorize]
[Route("api/reportes")]
public class ReportesController(AppDbContext db, IKardexPdfService kardexPdfService, IKardexExcelService kardexExcelService)
    : ControllerBase
{
    [HttpGet("existencias")]
    [Authorize(Roles = "Admin,Almacen,Ventas,Compras")]
    public async Task<List<ExistenciaReporte>> Existencias([FromQuery] int? productoId)
    {
        var query = db.Existencias
            .Include(e => e.Producto)
            .Include(e => e.Almacen)
            .Where(e => e.Producto.Activo)
            .AsQueryable();

        if (productoId is int id)
        {
            query = query.Where(e => e.ProductoId == id);
        }

        return await query
            .OrderBy(e => e.Producto.Nombre).ThenBy(e => e.Almacen.Nombre)
            .Select(e => new ExistenciaReporte(
                e.Producto.Nombre, e.Almacen.Nombre, e.Cantidad, e.Producto.StockMinimo, e.Producto.UnidadVenta))
            .ToListAsync();
    }

    [HttpGet("kardex/{productoId:int}")]
    [Authorize(Roles = "Admin,Almacen,Ventas,Compras")]
    public async Task<ActionResult<List<KardexMovimiento>>> Kardex(int productoId, [FromQuery] List<int>? almacenIds)
    {
        if (!await db.Productos.AnyAsync(p => p.Id == productoId))
        {
            return NotFound("Producto no encontrado.");
        }

        var resultado = await CalcularKardexAsync(productoId, almacenIds);
        return resultado;
    }

    [HttpGet("kardex/{productoId:int}/pdf")]
    [Authorize(Roles = "Admin,Almacen,Ventas,Compras")]
    public async Task<IActionResult> KardexPdf(int productoId, [FromQuery] List<int>? almacenIds)
    {
        var producto = await db.Productos.FindAsync(productoId);
        if (producto is null)
        {
            return NotFound();
        }

        var movimientos = await CalcularKardexAsync(productoId, almacenIds);
        var pdf = kardexPdfService.Generar(producto, movimientos);
        return File(pdf, "application/pdf", $"kardex-{producto.Codigo ?? producto.Id.ToString()}.pdf");
    }

    [HttpGet("kardex/{productoId:int}/excel")]
    [Authorize(Roles = "Admin,Almacen,Ventas,Compras")]
    public async Task<IActionResult> KardexExcel(int productoId, [FromQuery] List<int>? almacenIds)
    {
        var producto = await db.Productos.FindAsync(productoId);
        if (producto is null)
        {
            return NotFound();
        }

        var movimientos = await CalcularKardexAsync(productoId, almacenIds);
        var excel = kardexExcelService.Generar(producto, movimientos);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"kardex-{producto.Codigo ?? producto.Id.ToString()}.xlsx");
    }

    // Saldo acumulado independiente POR ALMACÉN -- si se piden varios almacenes a la vez, cada
    // uno lleva su propio saldo corriente (no uno mezclado entre todos), para poder mostrar la
    // separación por almacén tanto en pantalla como en el PDF/Excel.
    private async Task<List<KardexMovimiento>> CalcularKardexAsync(int productoId, List<int>? almacenIds)
    {
        var query = db.MovimientosInventario
            .Include(m => m.Almacen)
            .Where(m => m.ProductoId == productoId);

        if (almacenIds is { Count: > 0 })
        {
            query = query.Where(m => almacenIds.Contains(m.AlmacenId));
        }

        var movimientos = await query
            .OrderBy(m => m.AlmacenId).ThenBy(m => m.FechaUtc).ThenBy(m => m.Id)
            .Select(m => new { m.AlmacenId, Almacen = m.Almacen.Nombre, m.FechaUtc, m.Tipo, m.Cantidad, m.Referencia, m.Nota })
            .ToListAsync();

        var resultado = new List<KardexMovimiento>(movimientos.Count);
        foreach (var grupo in movimientos.GroupBy(m => m.AlmacenId))
        {
            var saldo = 0m;
            foreach (var m in grupo)
            {
                saldo += m.Tipo == TipoMovimiento.Entrada ? m.Cantidad : -m.Cantidad;
                resultado.Add(new KardexMovimiento(m.FechaUtc, m.Almacen, m.Tipo, m.Cantidad, saldo, m.Referencia, m.Nota));
            }
        }

        return resultado.OrderBy(m => m.Almacen).ThenBy(m => m.FechaUtc).ToList();
    }

    [HttpGet("ventas-por-cliente")]
    [Authorize(Roles = "Admin,Ventas,Facturacion,CuentasPorCobrar")]
    public async Task<List<VentaPorClienteReporte>> VentasPorCliente()
    {
        var pedidos = await db.PedidosVenta
            .Include(p => p.Cliente)
            .Where(p => p.Estatus != EstatusPedidoVenta.Cancelado)
            .ToListAsync();

        return pedidos.GroupBy(p => p.Cliente.Nombre)
            .Select(g => new VentaPorClienteReporte(g.Key, g.Count(), g.Sum(p => p.Total)))
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    [HttpGet("ventas-por-producto")]
    [Authorize(Roles = "Admin,Ventas,Facturacion,CuentasPorCobrar")]
    public async Task<List<VentaPorProductoReporte>> VentasPorProducto()
    {
        var itemsMayoreo = await db.PedidoVentaItems
            .Include(i => i.Producto)
            .Where(i => i.PedidoVenta.Estatus != EstatusPedidoVenta.Cancelado)
            .Select(i => new { i.Producto.Nombre, i.Cantidad, i.Importe })
            .ToListAsync();

        var itemsMostrador = await db.VentaMostradorItems
            .Include(i => i.Producto)
            .Select(i => new { i.Producto.Nombre, i.Cantidad, i.Importe })
            .ToListAsync();

        return itemsMayoreo.Concat(itemsMostrador)
            .GroupBy(i => i.Nombre)
            .Select(g => new VentaPorProductoReporte(g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Importe)))
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    // Usa el costo capturado en cada partida AL MOMENTO de la venta (PedidoVentaItem/VentaMostradorItem.CostoUnitario),
    // no el costo actual del producto -- así la utilidad de una venta vieja no cambia cuando el
    // costo promedio se recalcula con compras más recientes.
    [HttpGet("utilidad")]
    [Authorize(Roles = "Admin,Ventas,Facturacion,CuentasPorCobrar")]
    public async Task<List<UtilidadReporte>> Utilidad()
    {
        var itemsMayoreo = await db.PedidoVentaItems
            .Include(i => i.Producto)
            .Where(i => i.PedidoVenta.Estatus != EstatusPedidoVenta.Cancelado)
            .Select(i => new { i.Producto.Nombre, i.Cantidad, i.Importe, Costo = i.Cantidad * i.CostoUnitario })
            .ToListAsync();

        var itemsMostrador = await db.VentaMostradorItems
            .Include(i => i.Producto)
            .Select(i => new { i.Producto.Nombre, i.Cantidad, i.Importe, Costo = i.Cantidad * i.CostoUnitario })
            .ToListAsync();

        return itemsMayoreo.Concat(itemsMostrador)
            .GroupBy(i => i.Nombre)
            .Select(g =>
            {
                var ventas = g.Sum(x => x.Importe);
                var costo = g.Sum(x => x.Costo);
                var utilidad = ventas - costo;
                return new UtilidadReporte(g.Key, g.Sum(x => x.Cantidad), ventas, costo, utilidad, ventas <= 0 ? 0 : Math.Round(utilidad / ventas * 100, 1));
            })
            .OrderByDescending(r => r.Utilidad)
            .ToList();
    }

    [HttpGet("cartera-vencida")]
    [Authorize(Roles = "Admin,Ventas,Facturacion,CuentasPorCobrar")]
    public async Task<List<CarteraVencidaReporte>> CarteraVencida()
    {
        // Cotización y Pedido ya no son estatus del mismo documento -- solo una Remisión representa
        // mercancía de verdad entregada, así que solo esas cuentan como deuda real.
        var pedidos = await db.PedidosVenta
            .AsSplitQuery()
            .Include(p => p.Cliente)
            .Include(p => p.Pagos)
            .Include(p => p.CreditoRecibido)
            .Where(p => p.Estatus == EstatusPedidoVenta.Remisionado)
            .ToListAsync();

        var hoy = DateTime.UtcNow;
        return pedidos
            .Select(p => new CarteraVencidaReporte(
                p.Folio, p.Cliente.Nombre, p.FechaUtc, p.FechaUtc.AddDays(p.Cliente.DiasCredito),
                p.Total - p.Pagos.Sum(x => x.Monto) - p.CreditoRecibido.Sum(a => a.Monto)))
            .Where(r => r.Saldo > 0 && r.Vence < hoy)
            .OrderBy(r => r.Vence)
            .ToList();
    }

    [HttpGet("cuentas-por-pagar")]
    [Authorize(Roles = "Admin,Compras,CuentasPorPagar")]
    public async Task<List<CuentasPorPagarReporte>> CuentasPorPagar()
    {
        var pedidos = await db.PedidosCompra
            .Include(p => p.Proveedor)
            .Include(p => p.Pagos)
            .Where(p => p.Estatus != EstatusPedidoCompra.Cancelado)
            .ToListAsync();

        var hoy = DateTime.UtcNow;
        return pedidos
            .Select(p => new CuentasPorPagarReporte(
                p.Folio, p.Proveedor.Nombre, p.FechaUtc, p.FechaUtc.AddDays(p.Proveedor.DiasCredito),
                p.Total - p.Pagos.Sum(x => x.Monto)))
            .Where(r => r.Saldo > 0 && r.Vence < hoy)
            .OrderBy(r => r.Vence)
            .ToList();
    }

    [HttpGet("cortes-caja")]
    [Authorize(Roles = "Admin,Cajero")]
    public async Task<List<TurnoCaja>> CortesCaja() =>
        await db.TurnosCaja
            .Include(t => t.CajeroEmpleado)
            .Where(t => t.Estatus == EstatusTurno.Cerrado)
            .OrderByDescending(t => t.CerradoUtc)
            .ToListAsync();
}
