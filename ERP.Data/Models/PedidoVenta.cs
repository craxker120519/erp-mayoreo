using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public enum EstatusPedidoVenta
{
    Cotizacion,
    Pedido,
    Remisionado,
    Cancelado
}

public class PedidoVenta
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Null = almacén "Principal" por defecto -- ver nota en PedidoCompra.AlmacenId.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }

    // FechaUtc = cuándo se creó ESTE documento específico (esta etapa) -- ya no "cuándo se creó
    // la cotización que le dio origen a todo esto".
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    // Esto ahora es el TIPO de documento, fijo desde que se crea -- Cotización/Pedido/Remisión son
    // documentos separados con folio propio (COT/PV/REM, ver FolioService), no estatus de un mismo
    // registro que se sobreescribe. "Convertir a pedido" y "Remisionar" crean un documento NUEVO
    // encadenado (DocumentoOrigenId) en vez de mutar este -- así el documento anterior se conserva
    // intacto tal como estaba en ese momento, para poder consultarlo después.
    public EstatusPedidoVenta Estatus { get; set; } = EstatusPedidoVenta.Cotizacion;
    public string? Nota { get; set; }
    public decimal Total { get; set; }

    // El documento del que este nació (ej. el Pedido apunta a su Cotización de origen). Null si
    // se creó directo (cotización nueva, o Factura directa que salta los pasos intermedios).
    public int? DocumentoOrigenId { get; set; }
    public PedidoVenta? DocumentoOrigen { get; set; }

    // El/los documento(s) que nacieron DE este -- normalmente 0 o 1 (una cotización se convierte a
    // UN pedido), pero no se restringe a 1 por si en el futuro hace falta re-cotizar. Si esta
    // colección no está vacía, este documento ya "avanzó" -- se muestra como historial/solo lectura,
    // ya no es el punto activo de la cadena.
    public ICollection<PedidoVenta> DocumentosGenerados { get; set; } = new List<PedidoVenta>();

    // Cadena completa (Cotización→Pedido→Remisión) YA APLANADA, calculada del lado del servidor
    // (PedidosVentaController.CargarConDetalle) -- no mapeada a la base de datos. Existe porque
    // Cotización y Pedido se referencian mutuamente (Pedido.DocumentoOrigen == Cotización,
    // Cotización.DocumentosGenerados contiene a Pedido) -- un ciclo real, que System.Text.Json
    // (ReferenceHandler.IgnoreCycles) rompe reemplazando el elemento repetido dentro de la
    // colección por `null`, no la colección completa -- así que `DocumentoOrigen.DocumentosGenerados`
    // podía traer un `null` aunque `Count` fuera mayor a 0, y cualquier `.First().Id` sobre eso
    // tronaba en el cliente. Esta lista, en cambio, nunca se serializa con referencias circulares.
    [NotMapped]
    public List<PedidoVentaCadenaItem>? Cadena { get; set; }

    public ICollection<PedidoVentaItem> Items { get; set; } = new List<PedidoVentaItem>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    // Notas de crédito que se ORIGINARON contra este pedido (para trazabilidad -- ya no reducen
    // el saldo de este pedido automáticamente, ver NotaCreditoAplicacion/CreditoRecibido).
    public ICollection<NotaCredito> NotasCredito { get; set; } = new List<NotaCredito>();

    // Crédito de nota(s) -- de este pedido o de cualquier otro del mismo cliente -- que sí se
    // aplicó como pago aquí. Esto es lo que de verdad descuenta el saldo pendiente.
    public ICollection<NotaCreditoAplicacion> CreditoRecibido { get; set; } = new List<NotaCreditoAplicacion>();
}

public class PedidoVentaCadenaItem
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public EstatusPedidoVenta Estatus { get; set; }
    public DateTime FechaUtc { get; set; }
}

public class PedidoVentaItem
{
    public int Id { get; set; }
    public int PedidoVentaId { get; set; }
    public PedidoVenta PedidoVenta { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }

    // Costo promedio del producto AL MOMENTO de esta venta (copiado de Producto.Costo al crear el
    // pedido, igual que PrecioUnitario) -- necesario para que el reporte de utilidad sea exacto:
    // el costo del producto cambia con cada compra, así que usar el costo ACTUAL para ventas
    // viejas daría una utilidad incorrecta.
    public decimal CostoUnitario { get; set; }
}
