using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

// Contraseña propia del candado (no la del login) -- cualquier usuario autenticado puede intentar
// verificarla (el permiso real de VER la sección ya lo dio su rol; el candado es una segunda
// puerta encima de eso), pero solo Admin puede cambiarla.
[ApiController]
[Authorize]
[Route("api/candados")]
public class CandadosController(AppDbContext db) : ControllerBase
{
    private static readonly PasswordHasher<CandadoAcceso> Hasher = new();

    [HttpPost("verificar")]
    public async Task<ActionResult<bool>> Verificar(VerificarCandadoRequest request)
    {
        var candado = await db.CandadosAcceso.FirstOrDefaultAsync(c => c.Nombre == request.Nombre);
        if (candado is null)
        {
            return NotFound();
        }

        var resultado = Hasher.VerifyHashedPassword(candado, candado.PasswordHash, request.Password);
        return resultado != PasswordVerificationResult.Failed;
    }

    [HttpPut("{nombre}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CambiarPassword(string nombre, CambiarPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PasswordNueva))
        {
            return BadRequest("La nueva contraseña no puede estar vacía.");
        }

        var candado = await db.CandadosAcceso.FirstOrDefaultAsync(c => c.Nombre == nombre);
        if (candado is null)
        {
            return NotFound();
        }

        candado.PasswordHash = Hasher.HashPassword(candado, request.PasswordNueva);
        await db.SaveChangesAsync();
        return Ok();
    }
}
