using ERP.Api.Dtos;
using ERP.Services;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Cajero")]
[Route("api/turnos-caja")]
public class TurnosCajaController(
    AppDbContext db, IFolioService folioService, IInventarioService inventario,
    ITurnoCajaPdfService turnoPdfService, IVentaMostradorPdfService ventaPdfService, IAuditoriaService auditoria)
    : ControllerBase
{
    [HttpGet("actual")]
    public async Task<ActionResult<TurnoCaja>> GetActual()
    {
        var turno = await db.TurnosCaja
            .Include(t => t.CajeroEmpleado)
            .Include(t => t.Almacen)
            .Include(t => t.Ventas)
            .FirstOrDefaultAsync(t => t.Estatus == EstatusTurno.Abierto);

        return turno is null ? NotFound() : turno;
    }

    [HttpGet("cerrados")]
    public async Task<List<TurnoCaja>> GetCerrados() =>
        await db.TurnosCaja
            .Include(t => t.CajeroEmpleado)
            .Include(t => t.Almacen)
            .Where(t => t.Estatus == EstatusTurno.Cerrado)
            .OrderByDescending(t => t.CerradoUtc)
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TurnoCaja>> GetById(int id)
    {
        var turno = await CargarTurnoConDetalle(id);
        return turno is null ? NotFound() : turno;
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var turno = await CargarTurnoConDetalle(id);
        if (turno is null)
        {
            return NotFound();
        }

        var pdf = turnoPdfService.Generar(turno);
        return File(pdf, "application/pdf", $"corte-caja-{turno.Id}.pdf");
    }

    [HttpGet("ventas/{id:int}")]
    public async Task<ActionResult<VentaMostrador>> GetVenta(int id)
    {
        var venta = await CargarVentaConDetalle(id);
        return venta is null ? NotFound() : venta;
    }

    [HttpGet("ventas/{id:int}/pdf")]
    public async Task<IActionResult> GetVentaPdf(int id)
    {
        var venta = await CargarVentaConDetalle(id);
        if (venta is null)
        {
            return NotFound();
        }

        var pdf = ventaPdfService.Generar(venta);
        return File(pdf, "application/pdf", $"{venta.Folio}.pdf");
    }

    [HttpPost("abrir")]
    public async Task<ActionResult<TurnoCaja>> Abrir(AbrirTurnoRequest request)
    {
        var yaAbierto = await db.TurnosCaja.AnyAsync(t => t.Estatus == EstatusTurno.Abierto);
        if (yaAbierto)
        {
            return BadRequest("Ya hay un turno abierto.");
        }

        if (request.MontoInicial < 0)
        {
            return BadRequest("El monto inicial no puede ser negativo.");
        }

        if (!await db.Empleados.AnyAsync(e => e.Id == request.CajeroEmpleadoId))
        {
            return BadRequest("El cajero seleccionado no existe.");
        }

        var turno = new TurnoCaja
        {
            CajeroEmpleadoId = request.CajeroEmpleadoId,
            AlmacenId = request.AlmacenId,
            MontoInicial = request.MontoInicial
        };
        db.TurnosCaja.Add(turno);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Abrir turno", "TurnoCaja", turno.Id, $"Monto inicial {turno.MontoInicial:C}");
        return await GetActual();
    }

    [HttpPost("{id:int}/cerrar")]
    public async Task<ActionResult<TurnoCaja>> Cerrar(int id, CerrarTurnoRequest request)
    {
        var turno = await db.TurnosCaja.Include(t => t.Ventas).FirstOrDefaultAsync(t => t.Id == id);
        if (turno is null)
        {
            return NotFound();
        }

        var esperado = turno.MontoInicial + turno.Ventas.Where(v => v.MetodoPago == MetodoPago.Efectivo).Sum(v => v.Total);
        turno.CerradoUtc = DateTime.UtcNow;
        turno.MontoContado = request.MontoContado;
        turno.MontoEsperado = esperado;
        turno.Diferencia = request.MontoContado - esperado;
        turno.Estatus = EstatusTurno.Cerrado;

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(User.Identity?.Name, "Cerrar turno", "TurnoCaja", turno.Id,
            $"Esperado {esperado:C}, contado {request.MontoContado:C}, diferencia {turno.Diferencia:C}");
        return turno;
    }

    [HttpPost("{id:int}/ventas")]
    public async Task<ActionResult<VentaMostrador>> RegistrarVenta(int id, CrearVentaMostradorRequest request)
    {
        var turno = await db.TurnosCaja.FirstOrDefaultAsync(t => t.Id == id && t.Estatus == EstatusTurno.Abierto);
        if (turno is null)
        {
            return NotFound("No hay un turno abierto con ese id.");
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("La venta necesita al menos un producto.");
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

        var almacenVenta = await inventario.ResolverAlmacenAsync(turno.AlmacenId);
        var existencias = await db.Existencias
            .Where(e => e.AlmacenId == almacenVenta.Id && request.Items.Select(i => i.ProductoId).Contains(e.ProductoId))
            .ToDictionaryAsync(e => e.ProductoId, e => e.Cantidad);

        var faltantes = request.Items
            .Where(i => i.Cantidad > existencias.GetValueOrDefault(i.ProductoId))
            .ToList();

        if (faltantes.Count > 0)
        {
            var detalle = string.Join("; ", faltantes.Select(f =>
                $"{productos[f.ProductoId].Nombre} (pide {f.Cantidad}, disponible {existencias.GetValueOrDefault(f.ProductoId)})"));
            return BadRequest($"Stock insuficiente en {almacenVenta.Nombre}: {detalle}.");
        }

        var items = request.Items.Select(i => new VentaMostradorItem
        {
            ProductoId = i.ProductoId,
            Cantidad = i.Cantidad,
            PrecioUnitario = productos[i.ProductoId].Precio,
            Importe = i.Cantidad * productos[i.ProductoId].Precio,
            CostoUnitario = productos[i.ProductoId].Costo
        }).ToList();

        var venta = new VentaMostrador
        {
            Folio = await folioService.GenerarFolioVentaMostradorAsync(),
            TurnoCajaId = id,
            ClienteId = request.ClienteId,
            MetodoPago = request.MetodoPago,
            Total = items.Sum(i => i.Importe),
            Items = items
        };

        db.VentasMostrador.Add(venta);
        await db.SaveChangesAsync();

        var notaVenta = request.ClienteId is int clienteId
            ? $"Venta de mostrador a {(await db.Clientes.FindAsync(clienteId))?.Nombre ?? "cliente"}"
            : "Venta de mostrador (público en general)";
        foreach (var item in items)
        {
            await inventario.RegistrarMovimientoAsync(
                item.ProductoId, almacenVenta.Id, TipoMovimiento.Salida, item.Cantidad, venta.Folio, notaVenta);
        }

        return (await CargarVentaConDetalle(venta.Id))!;
    }

    private Task<TurnoCaja?> CargarTurnoConDetalle(int id) =>
        db.TurnosCaja
            .Include(t => t.CajeroEmpleado)
            .Include(t => t.Almacen)
            .Include(t => t.Ventas).ThenInclude(v => v.Items).ThenInclude(i => i.Producto)
            .Include(t => t.Ventas).ThenInclude(v => v.Cliente)
            .FirstOrDefaultAsync(t => t.Id == id);

    private Task<VentaMostrador?> CargarVentaConDetalle(int id) =>
        db.VentasMostrador
            .Include(v => v.Items).ThenInclude(i => i.Producto)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id);
}
