namespace ERP.Models;

public enum MetodoPago
{
    Efectivo,
    Tarjeta,
    Transferencia
}

public class VentaMostrador
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public int TurnoCajaId { get; set; }
    public TurnoCaja TurnoCaja { get; set; } = null!;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public decimal Total { get; set; }

    public ICollection<VentaMostradorItem> Items { get; set; } = new List<VentaMostradorItem>();
}

public class VentaMostradorItem
{
    public int Id { get; set; }
    public int VentaMostradorId { get; set; }
    public VentaMostrador VentaMostrador { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }

    // Costo promedio del producto al momento de esta venta -- ver la misma nota en PedidoVentaItem.
    public decimal CostoUnitario { get; set; }
}
