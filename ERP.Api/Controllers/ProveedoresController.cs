using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Compras")]
[Route("api/proveedores")]
public class ProveedoresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<Proveedor>> GetAll() =>
        await db.Proveedores.OrderBy(p => p.Nombre).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Proveedor>> Create(ProveedorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var proveedor = new Proveedor
        {
            Nombre = request.Nombre.Trim(),
            Rfc = request.Rfc?.Trim(),
            Telefono = request.Telefono?.Trim(),
            Email = request.Email?.Trim(),
            CondicionesPago = request.CondicionesPago?.Trim(),
            DiasCredito = request.DiasCredito
        };
        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync();
        return proveedor;
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Proveedor>> Update(int id, ProveedorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var proveedor = await db.Proveedores.FindAsync(id);
        if (proveedor is null)
        {
            return NotFound();
        }

        proveedor.Nombre = request.Nombre.Trim();
        proveedor.Rfc = request.Rfc?.Trim();
        proveedor.Telefono = request.Telefono?.Trim();
        proveedor.Email = request.Email?.Trim();
        proveedor.CondicionesPago = request.CondicionesPago?.Trim();
        proveedor.DiasCredito = request.DiasCredito;
        await db.SaveChangesAsync();
        return proveedor;
    }

    [HttpPost("{id:int}/alternar-activo")]
    public async Task<ActionResult<Proveedor>> AlternarActivo(int id)
    {
        var proveedor = await db.Proveedores.FindAsync(id);
        if (proveedor is null)
        {
            return NotFound();
        }

        proveedor.Activo = !proveedor.Activo;
        await db.SaveChangesAsync();
        return proveedor;
    }
}
