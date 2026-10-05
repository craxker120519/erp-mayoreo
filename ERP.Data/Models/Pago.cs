namespace ERP.Models;

public class Pago
{
    public int Id { get; set; }
    public int PedidoVentaId { get; set; }
    public PedidoVenta PedidoVenta { get; set; } = null!;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Nota { get; set; }
}
