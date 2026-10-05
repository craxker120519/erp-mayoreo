namespace ERP.Models;

public enum EstatusNotaCredito
{
    Emitida,
    Cancelada
}

public class NotaCredito
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public int PedidoVentaId { get; set; }
    public PedidoVenta PedidoVenta { get; set; } = null!;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string? Motivo { get; set; }

    // true = devolución física (regresa a existencia); false = bonificación/ajuste de precio,
    // no toca inventario. Se decide una sola vez al emitir la nota.
    public bool DevuelveInventario { get; set; } = true;

    public decimal Subtotal { get; set; }
    public decimal TasaIva { get; set; } = 0.16m;
    public decimal Iva { get; set; }
    public decimal Total { get; set; }

    public EstatusNotaCredito Estatus { get; set; } = EstatusNotaCredito.Emitida;

    public ICollection<NotaCreditoItem> Items { get; set; } = new List<NotaCreditoItem>();

    // Cuánto de esta nota ya se aplicó como pago a alguna compra (la de origen u otra del mismo
    // cliente) -- ver NotaCreditoAplicacion. No se resta sola de PedidoVenta.PedidoVentaId; el
    // crédito solo se descuenta de un pedido cuando se aplica explícitamente.
    public ICollection<NotaCreditoAplicacion> Aplicaciones { get; set; } = new List<NotaCreditoAplicacion>();

    // No mapeado por EF -- se calcula en memoria a partir de Aplicaciones (requiere haberlas
    // incluido con .Include). Se serializa igual como campo normal del JSON de la nota.
    public decimal Disponible => Total - Aplicaciones.Sum(a => a.Monto);
}

// Registra que $Monto del crédito de una nota se usó como pago en PedidoVentaDestino -- puede ser
// el mismo pedido de origen de la nota, o cualquier otro pedido del mismo cliente ("usar la nota
// de crédito en una compra nueva"). Una nota puede aplicarse en varias partes a varios pedidos
// distintos, mientras no exceda su Total.
public class NotaCreditoAplicacion
{
    public int Id { get; set; }
    public int NotaCreditoId { get; set; }
    public NotaCredito NotaCredito { get; set; } = null!;
    public int PedidoVentaDestinoId { get; set; }
    public PedidoVenta PedidoVentaDestino { get; set; } = null!;
    public decimal Monto { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}

public class NotaCreditoItem
{
    public int Id { get; set; }
    public int NotaCreditoId { get; set; }
    public NotaCredito NotaCredito { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }
}
