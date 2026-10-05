using ERP.Api.Dtos;
using ERP.Services;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/productos")]
public class ProductosController(AppDbContext db, IFileStorageService fileStorage) : ControllerBase
{
    [HttpGet]
    public async Task<List<Producto>> GetAll() =>
        await db.Productos.OrderBy(p => p.Nombre).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        return producto is null ? NotFound() : producto;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Almacen")]
    public async Task<ActionResult<Producto>> Create(ProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var producto = MapFrom(request, new Producto());
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        return producto;
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Almacen")]
    public async Task<ActionResult<Producto>> Update(int id, ProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        var producto = await db.Productos.FindAsync(id);
        if (producto is null)
        {
            return NotFound();
        }

        MapFrom(request, producto);
        await db.SaveChangesAsync();
        return producto;
    }

    [HttpPost("{id:int}/alternar-activo")]
    [Authorize(Roles = "Admin,Almacen")]
    public async Task<ActionResult<Producto>> AlternarActivo(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null)
        {
            return NotFound();
        }

        producto.Activo = !producto.Activo;
        await db.SaveChangesAsync();
        return producto;
    }

    [HttpPost("{id:int}/hoja-seguridad")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Roles = "Admin,Almacen")]
    public async Task<ActionResult<Producto>> SubirHojaSeguridad(int id, IFormFile archivo)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null)
        {
            return NotFound();
        }

        await using var stream = archivo.OpenReadStream();
        var rutaRelativa = await fileStorage.GuardarHojaSeguridadAsync(id, stream, archivo.FileName);

        producto.HojaSeguridadRuta = rutaRelativa;
        producto.HojaSeguridadNombreOriginal = archivo.FileName;
        await db.SaveChangesAsync();
        return producto;
    }

    private static Producto MapFrom(ProductoRequest request, Producto producto)
    {
        producto.Nombre = request.Nombre.Trim();
        producto.Codigo = request.Codigo?.Trim();
        producto.Categoria = request.Categoria?.Trim();
        producto.UnidadCompra = request.UnidadCompra?.Trim() ?? string.Empty;
        producto.UnidadVenta = request.UnidadVenta?.Trim() ?? string.Empty;
        producto.FactorConversion = request.FactorConversion;
        producto.Precio = request.Precio;
        producto.Costo = request.Costo;
        producto.StockMinimo = request.StockMinimo;
        producto.EsPeligroso = request.EsPeligroso;
        return producto;
    }
}
