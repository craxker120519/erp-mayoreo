using ERP.Data;
using ERP.Models;

namespace ERP.Services;

public class AuditoriaService(AppDbContext db) : IAuditoriaService
{
    public async Task RegistrarAsync(string? usuario, string accion, string entidad, int? entidadId, string? detalle = null)
    {
        db.BitacoraEventos.Add(new BitacoraEvento
        {
            Usuario = string.IsNullOrWhiteSpace(usuario) ? "(desconocido)" : usuario,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle
        });
        await db.SaveChangesAsync();
    }
}
