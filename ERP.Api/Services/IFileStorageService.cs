namespace ERP.Services;

public interface IFileStorageService
{
    Task<string> GuardarHojaSeguridadAsync(int productoId, Stream contenido, string nombreOriginal);
}
