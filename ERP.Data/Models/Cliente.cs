using System.Text.Json.Serialization;

namespace ERP.Models;

public enum TipoCliente
{
    Mayoreo,
    Publico
}

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Rfc { get; set; }
    public TipoCliente Tipo { get; set; } = TipoCliente.Mayoreo;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public int DiasCredito { get; set; }
    public decimal LimiteCredito { get; set; }
    public bool Activo { get; set; } = true;

    // No se serializa: solo se carga con .Include() cuando hace falta (ver PedidosVentaController),
    // así que aquí siempre saldría vacía y daría a entender que el cliente no tiene pedidos.
    [JsonIgnore]
    public ICollection<PedidoVenta> Pedidos { get; set; } = new List<PedidoVenta>();
}
