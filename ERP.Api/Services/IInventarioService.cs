using ERP.Models;

namespace ERP.Services;

public interface IInventarioService
{
    Task<Almacen> ObtenerAlmacenPrincipalAsync();

    // Devuelve el almacén con ese id, o el Principal si almacenId es null (compatibilidad con
    // pedidos/turnos creados antes de que existiera selección de almacén).
    Task<Almacen> ResolverAlmacenAsync(int? almacenId);

    Task RegistrarMovimientoAsync(
        int productoId, int almacenId, TipoMovimiento tipo, decimal cantidad, string referencia, string? nota = null);

    // Costeo promedio ponderado (el que acepta el SAT para efectos fiscales en México, a
    // diferencia de UEPS/LIFO que ya no es válido desde 2005). Recalcula Producto.Costo cada vez
    // que entra mercancía nueva -- se llama ANTES de registrar la entrada, con la existencia total
    // (todos los almacenes) todavía sin esa entrada.
    Task AplicarCosteoPromedioAsync(int productoId, decimal cantidadRecibida, decimal costoRecibido);
}
