namespace ERP.Models;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Categoria { get; set; }

    // Presentación en la que se compra al proveedor (ej. "Tambor 200L") vs. la unidad en la
    // que se vende (ej. "Litro"). Fase 1 captura y descuenta inventario siempre en UnidadVenta;
    // FactorConversion queda listo para cuando se reciba compra por presentación completa.
    public string UnidadCompra { get; set; } = string.Empty;
    public string UnidadVenta { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; } = 1m;

    public decimal Precio { get; set; }
    public decimal Costo { get; set; }
    public decimal StockMinimo { get; set; }

    // Para el complemento Carta Porte con materiales peligrosos (fuera de alcance de Fase 1,
    // pero la bandera se captura desde ya porque es dato del producto, no del sistema fiscal).
    public bool EsPeligroso { get; set; }

    public string? HojaSeguridadRuta { get; set; }
    public string? HojaSeguridadNombreOriginal { get; set; }

    public bool Activo { get; set; } = true;
}
