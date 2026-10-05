using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Almacen")]
[Route("api/traspasos")]
public class TraspasosController(
    AppDbContext db, IFolioService folioService, IInventarioService inventario, ITraspasoPdfService pdfService,
    IAuditoriaService auditoria)
    : ControllerBase
{
    [HttpGet]
    public async Task<List<TraspasoAlmacen>> GetAll() =>
        await db.TraspasosAlmacen
            .Include(t => t.AlmacenOrigen)
            .Include(t => t.AlmacenDestino)
            .Include(t => t.Items).ThenInclude(i => i.Producto)
            .OrderByDescending(t => t.FechaUtc)
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TraspasoAlmacen>> GetById(int id)
    {
        var traspaso = await CargarConDetalle(id);
        return traspaso is null ? NotFound() : traspaso;
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var traspaso = await CargarConDetalle(id);
        if (traspaso is null)
        {
            return NotFound();
        }

        var pdf = pdfService.Generar(traspaso);
        return File(pdf, "application/pdf", $"{traspaso.Folio}.pdf");
    }

    [HttpPost]
    public async Task<ActionResult<TraspasoAlmacen>> Create(CrearTraspasoRequest request)
    {
        if (request.AlmacenOrigenId == request.AlmacenDestinoId)
        {
            return BadRequest("El almacén de origen y destino no pueden ser el mismo.");
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("El traspaso necesita al menos un producto.");
        }

        if (request.Items.Any(i => i.Cantidad <= 0))
        {
            return BadRequest("La cantidad de cada producto debe ser mayor a cero.");
        }

        if (!await db.Almacenes.AnyAsync(a => a.Id == request.AlmacenOrigenId) ||
            !await db.Almacenes.AnyAsync(a => a.Id == request.AlmacenDestinoId))
        {
            return BadRequest("El almacén de origen o destino no existe.");
        }

        var idsValidos = await db.Productos
            .Where(p => request.Items.Select(i => i.ProductoId).Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        if (request.Items.Any(i => !idsValidos.Contains(i.ProductoId)))
        {
            return BadRequest("Alguno de los productos no existe.");
        }

        var traspaso = new TraspasoAlmacen
        {
            Folio = await folioService.GenerarFolioTraspasoAsync(),
            AlmacenOrigenId = request.AlmacenOrigenId,
            AlmacenDestinoId = request.AlmacenDestinoId,
            Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim(),
            Items = request.Items.Select(i => new TraspasoAlmacenItem
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad
            }).ToList()
        };

        db.TraspasosAlmacen.Add(traspaso);
        await db.SaveChangesAsync();

        foreach (var item in traspaso.Items)
        {
            await inventario.RegistrarMovimientoAsync(
                item.ProductoId, request.AlmacenOrigenId, TipoMovimiento.Salida, item.Cantidad, traspaso.Folio,
                $"Traspaso a almacén destino (id {request.AlmacenDestinoId})");
            await inventario.RegistrarMovimientoAsync(
                item.ProductoId, request.AlmacenDestinoId, TipoMovimiento.Entrada, item.Cantidad, traspaso.Folio,
                $"Traspaso de almacén origen (id {request.AlmacenOrigenId})");
        }

        await auditoria.RegistrarAsync(User.Identity?.Name, "Crear", "Traspaso", traspaso.Id, traspaso.Folio);
        return (await CargarConDetalle(traspaso.Id))!;
    }

    private Task<TraspasoAlmacen?> CargarConDetalle(int id) =>
        db.TraspasosAlmacen
            .Include(t => t.AlmacenOrigen)
            .Include(t => t.AlmacenDestino)
            .Include(t => t.Items).ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(t => t.Id == id);
}
