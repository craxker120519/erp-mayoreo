namespace ERP.Models;

public enum EstatusFactura
{
    // Único estado posible hoy: PDF generado internamente, sin timbrar ante el SAT.
    // TimbradaCfdi queda reservado para cuando se conecte un PAC real (Facturama/SW Sapien/Finkok).
    EmitidaInterna,
    TimbradaCfdi,
    Cancelada
}

public class Factura
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public int PedidoVentaId { get; set; }
    public PedidoVenta PedidoVenta { get; set; } = null!;
    public DateTime FechaEmisionUtc { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }
    public decimal TasaIva { get; set; } = 0.16m;
    public decimal Iva { get; set; }
    public decimal Total { get; set; }

    public EstatusFactura Estatus { get; set; } = EstatusFactura.EmitidaInterna;

    // Reservados para cuando exista timbrado real -- se quedan en null hasta entonces.
    public string? UuidFiscal { get; set; }
    public DateTime? FechaTimbradoUtc { get; set; }
}
