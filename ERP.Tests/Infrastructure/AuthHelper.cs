using System.Net.Http.Headers;
using System.Net.Http.Json;
using ERP.Api.Dtos;

namespace ERP.Tests.Infrastructure;

public static class AuthHelper
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "admin1234";

    public static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    public static void AuthenticateAs(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.AuthenticateAs(token);
        return client;
    }

    // Crea un usuario nuevo con exactamente los módulos indicados (puede ser más de uno, para
    // probar combinaciones reales) y regresa un HttpClient ya autenticado como ese usuario.
    public static async Task<HttpClient> CreateUserClientAsync(ApiFactory factory, params string[] roles)
    {
        var admin = await AdminClientAsync(factory);
        var username = Unique.Nombre("test-user");
        const string password = "prueba1234";

        var createResponse = await admin.PostAsJsonAsync("/api/usuarios",
            new CrearUsuarioRequest(username, password, roles.ToList()));
        createResponse.EnsureSuccessStatusCode();

        var client = factory.CreateClient();
        var token = await LoginAsync(client, username, password);
        client.AuthenticateAs(token);
        return client;
    }
}
