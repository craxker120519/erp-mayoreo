using System.Text.Json.Serialization;

namespace ERP.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Rfc { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? CondicionesPago { get; set; }
    public int DiasCredito { get; set; }
    public bool Activo { get; set; } = true;

    [JsonIgnore]
    public ICollection<PedidoCompra> PedidosCompra { get; set; } = new List<PedidoCompra>();
}
