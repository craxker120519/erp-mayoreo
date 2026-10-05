namespace ERP.Models;

// Contraseña propia del candado (no la del login de cada persona) -- se pide una vez por sesión
// para ver secciones sensibles (Facturación, Cuentas por cobrar/pagar), como una caja fuerte con
// su propia llave. Ver AppSessionService.CandadosDesbloqueados (solo en memoria, se resetea al
// cerrar sesión) y Components/Shared/CandadoGate.razor.
public class CandadoAcceso
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
