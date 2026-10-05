namespace ERP.Models;

public enum EstatusTurno
{
    Abierto,
    Cerrado
}

public class TurnoCaja
{
    public int Id { get; set; }
    public int CajeroEmpleadoId { get; set; }
    public Empleado CajeroEmpleado { get; set; } = null!;

    // Null = almacén "Principal" por defecto -- ver nota en PedidoCompra.AlmacenId.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }

    public DateTime AbiertoUtc { get; set; } = DateTime.UtcNow;
    public decimal MontoInicial { get; set; }
    public DateTime? CerradoUtc { get; set; }
    public decimal? MontoContado { get; set; }
    public decimal? MontoEsperado { get; set; }
    public decimal? Diferencia { get; set; }
    public EstatusTurno Estatus { get; set; } = EstatusTurno.Abierto;

    public ICollection<VentaMostrador> Ventas { get; set; } = new List<VentaMostrador>();
}
