using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/empleados")]
public class EmpleadosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<Empleado>> GetAll() =>
        await db.Empleados.OrderBy(e => e.Nombre).ToListAsync();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Empleado>> Create(EmpleadoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var empleado = new Empleado { Nombre = request.Nombre.Trim() };
        db.Empleados.Add(empleado);
        await db.SaveChangesAsync();
        return empleado;
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Empleado>> Update(int id, EmpleadoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var empleado = await db.Empleados.FindAsync(id);
        if (empleado is null)
        {
            return NotFound();
        }

        empleado.Nombre = request.Nombre.Trim();
        await db.SaveChangesAsync();
        return empleado;
    }

    [HttpPost("{id:int}/alternar-activo")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Empleado>> AlternarActivo(int id)
    {
        var empleado = await db.Empleados.FindAsync(id);
        if (empleado is null)
        {
            return NotFound();
        }

        empleado.Activo = !empleado.Activo;
        await db.SaveChangesAsync();
        return empleado;
    }
}
