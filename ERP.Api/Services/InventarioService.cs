using ERP.Data;
using ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services;

// "Principal" se crea automáticamente la primera vez que se necesita un almacén y nadie ha dado
// de alta ninguno todavía -- así que un negocio que nunca usa multi-almacén sigue funcionando
// exactamente igual que antes, sin tener que crear nada a mano.
public class InventarioService(AppDbContext db) : IInventarioService
{
    public async Task<Almacen> ObtenerAlmacenPrincipalAsync()
    {
        var almacen = await db.Almacenes.FirstOrDefaultAsync(a => a.Activo);
        if (almacen is not null)
        {
            return almacen;
        }

        almacen = new Almacen { Nombre = "Principal" };
        db.Almacenes.Add(almacen);
        await db.SaveChangesAsync();
        return almacen;
    }

    public async Task<Almacen> ResolverAlmacenAsync(int? almacenId)
    {
        if (almacenId is null)
        {
            return await ObtenerAlmacenPrincipalAsync();
        }

        var almacen = await db.Almacenes.FindAsync(almacenId.Value);
        return almacen ?? await ObtenerAlmacenPrincipalAsync();
    }

    public async Task RegistrarMovimientoAsync(
        int productoId, int almacenId, TipoMovimiento tipo, decimal cantidad, string referencia, string? nota = null)
    {
        var existencia = await db.Existencias
            .FirstOrDefaultAsync(e => e.ProductoId == productoId && e.AlmacenId == almacenId);
        if (existencia is null)
        {
            existencia = new Existencia { ProductoId = productoId, AlmacenId = almacenId, Cantidad = 0 };
            db.Existencias.Add(existencia);
        }

        existencia.Cantidad += tipo == TipoMovimiento.Entrada ? cantidad : -cantidad;

        db.MovimientosInventario.Add(new MovimientoInventario
        {
            ProductoId = productoId,
            AlmacenId = almacenId,
            Tipo = tipo,
            Cantidad = cantidad,
            Referencia = referencia,
            Nota = nota
        });

        await db.SaveChangesAsync();
    }

    public async Task AplicarCosteoPromedioAsync(int productoId, decimal cantidadRecibida, decimal costoRecibido)
    {
        var producto = await db.Productos.FindAsync(productoId);
        if (producto is null)
        {
            return;
        }

        var existenciaActual = await db.Existencias
            .Where(e => e.ProductoId == productoId)
            .SumAsync(e => (decimal?)e.Cantidad) ?? 0m;

        // Si no había existencia (o quedó en negativo por algún ajuste), el costo nuevo se
        // vuelve el promedio de arranque en vez de calcular una ponderación sin sentido.
        producto.Costo = existenciaActual <= 0
            ? costoRecibido
            : Math.Round(
                ((existenciaActual * producto.Costo) + (cantidadRecibida * costoRecibido)) / (existenciaActual + cantidadRecibida),
                4);

        await db.SaveChangesAsync();
    }
}
