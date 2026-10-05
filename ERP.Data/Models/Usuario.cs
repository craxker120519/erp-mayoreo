namespace ERP.Models;

// Reemplaza el login basado en configuración (AdminAccess:Users) -- ahora vive en la base de
// datos para que el Admin pueda crear usuarios y asignarles módulos desde la app, sin tocar
// appsettings/User Secrets. Roles es una lista libre de módulos (Compras, Ventas, Facturacion,
// CuentasPorCobrar, CuentasPorPagar, Almacen, Cajero, o el especial Admin) -- una persona puede
// tener varios a la vez, ej. ["Almacen","Facturacion"].
public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool Activo { get; set; } = true;
}
