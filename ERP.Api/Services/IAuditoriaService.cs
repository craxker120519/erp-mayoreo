namespace ERP.Services;

public interface IAuditoriaService
{
    Task RegistrarAsync(string? usuario, string accion, string entidad, int? entidadId, string? detalle = null);
}
