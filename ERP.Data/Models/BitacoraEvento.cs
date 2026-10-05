namespace ERP.Models;

// Registro de quién hizo qué -- necesario porque varios admins/vendedores comparten el mismo
// sistema con su propio usuario, pero antes no quedaba rastro de quién había cancelado un pedido,
// cerrado un turno, etc. Tabla independiente, sin FKs (el registro debe sobrevivir aunque se
// borre o cambie la entidad referenciada -- es historial, no un dato vivo).
public class BitacoraEvento
{
    public int Id { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string? Detalle { get; set; }
}
