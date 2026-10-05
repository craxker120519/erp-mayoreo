namespace ERP.Models;

public class Existencia
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public decimal Cantidad { get; set; }
}
