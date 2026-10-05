using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

// Los módulos válidos que se pueden asignar -- "Admin" es especial (acceso total, no es un
// departamento) y no se ofrece aquí como checkbox normal, se marca aparte.
public static class Modulos
{
    public static readonly string[] Departamentos =
        ["Compras", "Ventas", "Facturacion", "CuentasPorCobrar", "CuentasPorPagar", "Almacen", "Cajero"];
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/usuarios")]
public class UsuariosController(AppDbContext db) : ControllerBase
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    // No proyectar PasswordHash aquí -- aunque está hasheado, no hay razón para que salga del
    // servidor en un listado; Create/CambiarPassword sí regresan el Usuario completo, pero eso es
    // un eco inmediato de la propia operación que se acaba de hacer, no un listado expuesto.
    [HttpGet]
    public async Task<List<UsuarioResponse>> GetAll() =>
        await db.Usuarios
            .OrderBy(u => u.Username)
            .Select(u => new UsuarioResponse(u.Id, u.Username, u.Roles, u.Activo))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create(CrearUsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Usuario y contraseña son obligatorios.");
        }

        if (await db.Usuarios.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
        {
            return BadRequest("Ya existe un usuario con ese nombre.");
        }

        var usuario = new Usuario
        {
            Username = request.Username.Trim(),
            Roles = request.Roles.Distinct().ToList()
        };
        usuario.PasswordHash = Hasher.HashPassword(usuario, request.Password);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Usuario>> Update(int id, ActualizarUsuarioRequest request)
    {
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Roles = request.Roles.Distinct().ToList();
        usuario.Activo = request.Activo;
        await db.SaveChangesAsync();
        return usuario;
    }

    [HttpPost("{id:int}/cambiar-password")]
    public async Task<ActionResult<Usuario>> CambiarPassword(int id, CambiarPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PasswordNueva))
        {
            return BadRequest("La nueva contraseña no puede estar vacía.");
        }

        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.PasswordHash = Hasher.HashPassword(usuario, request.PasswordNueva);
        await db.SaveChangesAsync();
        return usuario;
    }
}
