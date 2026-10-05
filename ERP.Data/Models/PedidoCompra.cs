namespace ERP.Models;

public enum EstatusPedidoCompra
{
    Solicitado,
    Recibido,
    Cancelado
}

// No toda compra es mercancía para revender -- una laptop, papelería o un servicio nunca deben
// subir al inventario ni tener "producto" en el catálogo de ventas. `Inventario` es el
// comportamiento de siempre (Recibir suma existencia y recalcula costeo promedio); `GastoInterno`
// solo lleva el registro de la compra y su pago a proveedor, sin tocar inventario para nada.
public enum TipoPedidoCompra
{
    Inventario,
    GastoInterno
}

public class PedidoCompra
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    // Null = almacén "Principal" por defecto (compatibilidad con pedidos de antes de que
    // existiera selección de almacén). Se resuelve en InventarioService.ResolverAlmacenAsync.
    // Solo aplica cuando Tipo == Inventario -- un gasto interno no tiene almacén de destino.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }

    public TipoPedidoCompra Tipo { get; set; } = TipoPedidoCompra.Inventario;

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public EstatusPedidoCompra Estatus { get; set; } = EstatusPedidoCompra.Solicitado;
    public string? Nota { get; set; }
    public decimal Total { get; set; }

    // Igual que PedidoVenta.RemisionadoUtc -- fecha de la transición Solicitado -> Recibido, para
    // la línea de tiempo del documento. Null = todavía no se ha recibido.
    public DateTime? RecibidoUtc { get; set; }

    public ICollection<PedidoCompraItem> Items { get; set; } = new List<PedidoCompraItem>();
    public ICollection<PagoProveedor> Pagos { get; set; } = new List<PagoProveedor>();
}

public class PedidoCompraItem
{
    public int Id { get; set; }
    public int PedidoCompraId { get; set; }
    public PedidoCompra PedidoCompra { get; set; } = null!;

    // Null cuando el pedido es GastoInterno -- ahí no hay producto del catálogo de ventas, solo
    // una Descripcion libre (ej. "Laptop Dell Latitude", "Papelería de oficina").
    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string? Descripcion { get; set; }

    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Importe { get; set; }
}

public class PagoProveedor
{
    public int Id { get; set; }
    public int PedidoCompraId { get; set; }
    public PedidoCompra PedidoCompra { get; set; } = null!;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Nota { get; set; }
}
