namespace ERP.Services;

public class FileStorageService(IConfiguration configuration, IWebHostEnvironment env) : IFileStorageService
{
    private string RootPath =>
        configuration["Storage:RootPath"] is { Length: > 0 } configured
            ? configured
            : Path.Combine(env.ContentRootPath, "PersistedStorage");

    public async Task<string> GuardarHojaSeguridadAsync(int productoId, Stream contenido, string nombreOriginal)
    {
        var extension = Path.GetExtension(nombreOriginal);
        var carpeta = Path.Combine(RootPath, "uploads", "hojas-seguridad", productoId.ToString());
        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"HojaSeguridad-{productoId}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);

        await using (var destino = File.Create(rutaFisica))
        {
            await contenido.CopyToAsync(destino);
        }

        return Path.Combine("uploads", "hojas-seguridad", productoId.ToString(), nombreArchivo).Replace('\\', '/');
    }
}
