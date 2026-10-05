using ERP.Api.Auth;
using ERP.Api.Dtos;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService) : ControllerBase
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower() && u.Activo);

        if (usuario is null || Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        return new LoginResponse(tokenService.GenerateToken(usuario.Username, usuario.Roles), usuario.Username, usuario.Roles);
    }
}
