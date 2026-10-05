namespace ERP.Models;

public class TraspasoAlmacen
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;

    public int AlmacenOrigenId { get; set; }
    public Almacen AlmacenOrigen { get; set; } = null!;
    public int AlmacenDestinoId { get; set; }
    public Almacen AlmacenDestino { get; set; } = null!;

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string? Nota { get; set; }

    public ICollection<TraspasoAlmacenItem> Items { get; set; } = new List<TraspasoAlmacenItem>();
}

public class TraspasoAlmacenItem
{
    public int Id { get; set; }
    public int TraspasoAlmacenId { get; set; }
    public TraspasoAlmacen TraspasoAlmacen { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public decimal Cantidad { get; set; }
}
