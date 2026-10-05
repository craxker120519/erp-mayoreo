using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/almacenes")]
public class AlmacenesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<Almacen>> GetAll() =>
        await db.Almacenes.OrderBy(a => a.Nombre).ToListAsync();

    [HttpPost]
    [Authorize(Roles = "Admin,Almacen")]
    public async Task<ActionResult<Almacen>> Create(AlmacenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var almacen = new Almacen { Nombre = request.Nombre.Trim() };
        db.Almacenes.Add(almacen);
        await db.SaveChangesAsync();
        return almacen;
    }

    [HttpPost("{id:int}/alternar-activo")]
    [Authorize(Roles = "Admin,Almacen")]
    public async Task<ActionResult<Almacen>> AlternarActivo(int id)
    {
        var almacen = await db.Almacenes.FindAsync(id);
        if (almacen is null)
        {
            return NotFound();
        }

        almacen.Activo = !almacen.Activo;
        await db.SaveChangesAsync();
        return almacen;
    }
}
