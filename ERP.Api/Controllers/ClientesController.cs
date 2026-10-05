using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Ventas,Facturacion")]
[Route("api/clientes")]
public class ClientesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<Cliente>> GetAll() =>
        await db.Clientes.OrderBy(c => c.Nombre).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Cliente>> Create(ClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Rfc = request.Rfc?.Trim(),
            Tipo = request.Tipo,
            Telefono = request.Telefono?.Trim(),
            Email = request.Email?.Trim(),
            Direccion = request.Direccion?.Trim(),
            DiasCredito = request.DiasCredito,
            LimiteCredito = request.LimiteCredito
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return cliente;
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Cliente>> Update(int id, ClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null)
        {
            return NotFound();
        }

        cliente.Nombre = request.Nombre.Trim();
        cliente.Rfc = request.Rfc?.Trim();
        cliente.Tipo = request.Tipo;
        cliente.Telefono = request.Telefono?.Trim();
        cliente.Email = request.Email?.Trim();
        cliente.Direccion = request.Direccion?.Trim();
        cliente.DiasCredito = request.DiasCredito;
        cliente.LimiteCredito = request.LimiteCredito;
        await db.SaveChangesAsync();
        return cliente;
    }

    [HttpPost("{id:int}/alternar-activo")]
    public async Task<ActionResult<Cliente>> AlternarActivo(int id)
    {
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null)
        {
            return NotFound();
        }

        cliente.Activo = !cliente.Activo;
        await db.SaveChangesAsync();
        return cliente;
    }
}
