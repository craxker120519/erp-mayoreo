using System.Text;
using System.Text.Json.Serialization;
using ERP.Api.Auth;
using ERP.Services;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFolioService, FolioService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFacturaPdfService, FacturaPdfService>();
builder.Services.AddScoped<IPedidoVentaPdfService, PedidoVentaPdfService>();
builder.Services.AddScoped<IPedidoCompraPdfService, PedidoCompraPdfService>();
builder.Services.AddScoped<INotaCreditoPdfService, NotaCreditoPdfService>();
builder.Services.AddScoped<ITraspasoPdfService, TraspasoPdfService>();
builder.Services.AddScoped<IVentaMostradorPdfService, VentaMostradorPdfService>();
builder.Services.AddScoped<ITurnoCajaPdfService, TurnoCajaPdfService>();
builder.Services.AddScoped<IKardexPdfService, KardexPdfService>();
builder.Services.AddScoped<IKardexExcelService, KardexExcelService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Issuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Semilla idempotente: si nunca se ha creado ningún usuario, arranca uno Admin -- si no,
    // nadie podría entrar nunca a crear al primero desde la pantalla de administración.
    if (!await db.Usuarios.AnyAsync())
    {
        var admin = new Usuario { Username = "admin", Roles = ["Admin"] };
        admin.PasswordHash = new PasswordHasher<Usuario>().HashPassword(admin, "admin1234");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }

    // Semilla idempotente de los candados -- contraseña temporal, el Admin debe cambiarla desde
    // /administracion/usuarios en cuanto entre.
    if (!await db.CandadosAcceso.AnyAsync())
    {
        var hasher = new PasswordHasher<CandadoAcceso>();
        var facturacion = new CandadoAcceso { Nombre = "Facturacion" };
        facturacion.PasswordHash = hasher.HashPassword(facturacion, "candado1234");
        var cuentas = new CandadoAcceso { Nombre = "Cuentas" };
        cuentas.PasswordHash = hasher.HashPassword(cuentas, "candado1234");
        db.CandadosAcceso.AddRange(facturacion, cuentas);
        await db.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var storageRoot = app.Configuration["Storage:RootPath"] is { Length: > 0 } configuredRoot
    ? configuredRoot
    : Path.Combine(app.Environment.ContentRootPath, "PersistedStorage");
Directory.CreateDirectory(Path.Combine(storageRoot, "uploads"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageRoot),
    RequestPath = "/storage"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necesario para que ERP.Tests pueda usar WebApplicationFactory<Program> -- con top-level
// statements, la clase Program generada es "internal" por default, invisible desde otro proyecto.
public partial class Program;
