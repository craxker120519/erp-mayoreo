namespace ERP.App.Services;

// Reemplaza a AdminSessionService de la versión web: ahí la sesión vivía en el circuito de
// Blazor Server y se restauraba via ProtectedLocalStorage (API de navegador). Aquí no hay
// circuito ni navegador -- el JWT se guarda en SecureStorage y sobrevive a cerrar la app.
public class AppSessionService
{
    private const string TokenKey = "erp_jwt_token";
    private const string UsernameKey = "erp_username";
    private const string RolesKey = "erp_roles";

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    public List<string> CurrentRoles { get; private set; } = [];
    public string? Token { get; private set; }
    public event Action? AuthStateChanged;

    // Admin es especial (acceso total, no es un "módulo" más) -- el resto se checa con HasRole
    // contra la lista de módulos que trae el usuario, que puede tener varios a la vez.
    public bool IsAdmin => CurrentRoles.Contains("Admin");
    public bool HasRole(string modulo) => IsAdmin || CurrentRoles.Contains(modulo);

    // Candados desbloqueados en ESTA sesión (contraseña propia del candado, no la del login) --
    // solo en memoria a propósito, así que se resetea al cerrar sesión igual que si nunca se
    // hubiera desbloqueado (comportamiento pedido: "una vez por sesión"). La verificación real
    // contra el servidor vive en CandadoGate.razor -- aquí solo se guarda el resultado.
    private readonly HashSet<string> candadosDesbloqueados = [];
    public bool CandadoDesbloqueado(string nombre) => candadosDesbloqueados.Contains(nombre);
    public void MarcarCandadoDesbloqueado(string nombre) => candadosDesbloqueados.Add(nombre);

    public async Task<bool> TryRestoreAsync()
    {
        var token = await SecureStorage.GetAsync(TokenKey);
        var username = await SecureStorage.GetAsync(UsernameKey);
        var roles = await SecureStorage.GetAsync(RolesKey);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        Token = token;
        CurrentUsername = username;
        CurrentRoles = string.IsNullOrWhiteSpace(roles) ? [] : roles.Split(',').ToList();
        IsAuthenticated = true;
        AuthStateChanged?.Invoke();
        return true;
    }

    public async Task SetSessionAsync(string token, string username, List<string> roles)
    {
        Token = token;
        CurrentUsername = username;
        CurrentRoles = roles;
        IsAuthenticated = true;

        await SecureStorage.SetAsync(TokenKey, token);
        await SecureStorage.SetAsync(UsernameKey, username);
        await SecureStorage.SetAsync(RolesKey, string.Join(',', roles));

        AuthStateChanged?.Invoke();
    }

    public void Logout()
    {
        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(UsernameKey);
        SecureStorage.Remove(RolesKey);

        Token = null;
        CurrentUsername = null;
        CurrentRoles = [];
        IsAuthenticated = false;
        candadosDesbloqueados.Clear();
        AuthStateChanged?.Invoke();
    }
}
