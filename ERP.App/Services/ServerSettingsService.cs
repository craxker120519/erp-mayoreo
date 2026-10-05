namespace ERP.App.Services;

// Un .exe instalado no tiene un appsettings.json por-ambiente como una app web desplegada --
// la IP del servidor de la oficina se captura una vez en la pantalla de conexión y se guarda
// aquí (Preferences, no es secreto, a diferencia del JWT que sí va en SecureStorage).
public class ServerSettingsService
{
    private const string BaseUrlKey = "erp_server_base_url";

    public string? BaseUrl => Preferences.Get(BaseUrlKey, null);

    public void SetBaseUrl(string baseUrl) => Preferences.Set(BaseUrlKey, baseUrl.TrimEnd('/'));
}
