using ERP.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Tests.Infrastructure;

// Corre la API de verdad (sin mocks) contra una base Postgres separada ("erp_test") -- se probó
// con curl toda la sesión que encontró los bugs reales de este proyecto (permisos combinados,
// cadena de documentos, límite de crédito); estas pruebas son esa misma verificación, pero
// repetible sola. No se usa el proveedor InMemory de EF Core porque Usuario.Roles depende de
// text[] de Postgres -- InMemory no lo emula igual, daría falsa confianza.
public class ApiFactory : WebApplicationFactory<Program>
{
    // La cadena de conexión (con password real de "erp_dev") vive en User Secrets de ESTE
    // proyecto (`dotnet user-secrets set "ConnectionStrings:Test" "..."` desde ERP.Tests), igual
    // que ya se hace en ERP.Api -- nunca escrita en un .cs que algún día se suba a git. Se lee acá
    // porque ApiFactory no es en sí una app ASP.NET Core con su propio pipeline de configuración.
    public static readonly string ConnectionString = new ConfigurationBuilder()
        .AddUserSecrets<ApiFactory>()
        .Build()
        .GetConnectionString("Test")
        ?? throw new InvalidOperationException(
            "Falta configurar la cadena de conexión de pruebas. Corre desde ERP.Tests: " +
            "dotnet user-secrets set \"ConnectionStrings:Test\" \"Host=localhost;Port=5432;Database=erp_test;Username=erp_dev;Password=...\"");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting() en vez de ConfigureAppConfiguration()/AddInMemoryCollection(): con
        // top-level statements (WebApplication.CreateBuilder dentro de Program.cs), el orden de
        // capas de configuración hace que ConfigureAppConfiguration NO gane a tiempo -- Jwt:Key
        // seguía llegando vacío (el de appsettings.json) al momento de armar JwtBearerOptions.
        // UseSetting() es la API de bajo nivel pensada justo para este caso.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", "clave-de-pruebas-nada-secreta-pero-de-al-menos-32-caracteres");
        builder.UseSetting("Jwt:Issuer", "ErpApiTests");
        builder.UseSetting("Jwt:ExpirationHours", "12");
        builder.UseSetting("Storage:RootPath", Path.Combine(Path.GetTempPath(), "erp-tests-storage"));
        builder.UseSetting("Empresa:Nombre", "Empresa de prueba");
        builder.UseSetting("Empresa:Rfc", "XAXX010101000");
    }

    // Se llama UNA vez al arrancar la corrida completa de pruebas (ver DatabaseFixture) -- dentro
    // de cada prueba, cada quien crea sus propios clientes/productos/etc. con nombres únicos, así
    // que no hace falta limpiar entre pruebas individuales.
    //
    // OJO: acceder a `Services` (la línea de abajo) es lo que dispara el arranque real de la app
    // -- ahí es cuando Program.cs corre su propio seed idempotente de "admin"/"admin1234" y de
    // los candados. Si esas 2 tablas se truncaran aquí, quedaría vacía la única cuenta con la que
    // las pruebas pueden hacer login (ese seed solo corre UNA vez, al arrancar, no en cada
    // request) -- por eso "Usuarios" y "CandadosAcceso" quedan fuera de este TRUNCATE.
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                "NotaCreditoAplicaciones", "NotaCreditoItems", "NotasCredito",
                "Pagos", "Facturas", "PedidoVentaItems", "PedidosVenta",
                "PagosProveedor", "PedidoCompraItems", "PedidosCompra",
                "TraspasoAlmacenItems", "TraspasosAlmacen",
                "VentaMostradorItems", "VentasMostrador", "TurnosCaja",
                "MovimientosInventario", "Existencias",
                "BitacoraEventos",
                "Productos", "Almacenes", "Empleados", "Proveedores", "Clientes"
            RESTART IDENTITY CASCADE;
            """);
    }
}
