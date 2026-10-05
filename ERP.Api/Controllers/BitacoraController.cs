using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/bitacora")]
public class BitacoraController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<BitacoraEvento>> GetAll([FromQuery] int limite = 200) =>
        await db.BitacoraEventos
            .OrderByDescending(b => b.FechaUtc)
            .Take(Math.Clamp(limite, 1, 1000))
            .ToListAsync();
}
