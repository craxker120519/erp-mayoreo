namespace ERP.Models;

public enum TipoMovimiento
{
    Entrada,
    Salida
}

public class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public TipoMovimiento Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string? Referencia { get; set; }
    public string? Nota { get; set; }
}
