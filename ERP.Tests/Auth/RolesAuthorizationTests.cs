using System.Net;
using System.Net.Http.Json;
using ERP.Api.Dtos;
using ERP.Tests.Infrastructure;

namespace ERP.Tests.Auth;

// Cubre la clase de bug real que se encontró esta sesión: los atributos [Authorize] de clase y de
// acción en ASP.NET Core se combinan con AND, no se sobreescriben -- un atributo de acción nunca
// puede AMPLIAR el acceso más allá de lo que ya permite el de clase, solo angostarlo. Rompió
// NotasCreditoController.DisponiblesCliente/Aplicar (necesitaban CuentasPorCobrar además de
// Facturacion, pero el nivel de clase solo tenía Facturacion) hasta que se corrigió ampliando la
// clase y angostando las demás acciones. Estas pruebas son justo la matriz de curl que se corrió a
// mano esa noche, hecha repetible.
[Collection("Api")]
public class RolesAuthorizationTests(DatabaseFixture fixture)
{
    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task CuentasPorCobrar_puede_ver_notas_de_credito_disponibles_de_un_cliente()
    {
        // Regresión directa del bug: antes de la corrección esto daba 403 aunque el usuario
        // tuviera exactamente el módulo CuentasPorCobrar -- el nivel de clase del controller no lo
        // incluía y el de acción no bastaba para ampliarlo.
        var admin = await AuthHelper.AdminClientAsync(Factory);
        var cliente = await Seeding.CrearClienteAsync(admin);

        var cxc = await AuthHelper.CreateUserClientAsync(Factory, "CuentasPorCobrar");
        var response = await cxc.GetAsync($"/api/notas-credito/disponibles-cliente/{cliente.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CuentasPorCobrar_puede_aplicar_credito()
    {
        // La otra mitad del mismo bug -- Aplicar() tampoco se veía afectado por el nivel de clase
        // angosto porque no tenía override propio, así que heredaba directo del nivel de clase ya
        // ampliado. Aquí solo se confirma que la autorización pasa (404 esperado, no existe la nota).
        var cxc = await AuthHelper.CreateUserClientAsync(Factory, "CuentasPorCobrar");
        var response = await cxc.PostAsJsonAsync("/api/notas-credito/999999/aplicar",
            new AplicarCreditoRequest(1, 10));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Compras_no_puede_ver_el_catalogo_de_clientes()
    {
        var compras = await AuthHelper.CreateUserClientAsync(Factory, "Compras");
        var response = await compras.GetAsync("/api/clientes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ventas_puede_crear_un_pedido_pero_CuentasPorCobrar_no()
    {
        var admin = await AuthHelper.AdminClientAsync(Factory);
        var cliente = await Seeding.CrearClienteAsync(admin);
        var producto = await Seeding.CrearProductoAsync(admin);
        var request = new CrearPedidoVentaRequest(cliente.Id, null, null,
            [new ItemRequest(producto.Id, 1)]);

        var ventas = await AuthHelper.CreateUserClientAsync(Factory, "Ventas");
        var respuestaVentas = await ventas.PostAsJsonAsync("/api/pedidos-venta", request);
        Assert.Equal(HttpStatusCode.OK, respuestaVentas.StatusCode);

        var cxc = await AuthHelper.CreateUserClientAsync(Factory, "CuentasPorCobrar");
        var respuestaCxc = await cxc.PostAsJsonAsync("/api/pedidos-venta", request);
        Assert.Equal(HttpStatusCode.Forbidden, respuestaCxc.StatusCode);
    }

    [Fact]
    public async Task Cualquier_rol_autenticado_puede_leer_productos_pero_solo_Almacen_puede_crear()
    {
        var cajero = await AuthHelper.CreateUserClientAsync(Factory, "Cajero");
        var lectura = await cajero.GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.OK, lectura.StatusCode);

        var escritura = await cajero.PostAsJsonAsync("/api/productos",
            new ProductoRequest(Unique.Nombre("X"), null, null, "Pieza", "Pieza", 1, 10, 5, 0, false));
        Assert.Equal(HttpStatusCode.Forbidden, escritura.StatusCode);

        var almacen = await AuthHelper.CreateUserClientAsync(Factory, "Almacen");
        var escrituraAlmacen = await almacen.PostAsJsonAsync("/api/productos",
            new ProductoRequest(Unique.Nombre("Y"), null, null, "Pieza", "Pieza", 1, 10, 5, 0, false));
        Assert.Equal(HttpStatusCode.OK, escrituraAlmacen.StatusCode);
    }

    [Fact]
    public async Task Un_usuario_con_dos_modulos_combinados_puede_usar_los_dos()
    {
        // El punto entero del sistema de permisos: un usuario puede tener Almacen + Facturacion a
        // la vez, no es "un rol fijo entre 3 opciones" como antes de este rediseño.
        var combinado = await AuthHelper.CreateUserClientAsync(Factory, "Almacen", "Facturacion");

        var traspasos = await combinado.GetAsync("/api/traspasos");
        var facturas = await combinado.GetAsync("/api/facturas");

        Assert.Equal(HttpStatusCode.OK, traspasos.StatusCode);
        Assert.Equal(HttpStatusCode.OK, facturas.StatusCode);
    }

    [Fact]
    public async Task Sin_token_todo_responde_401()
    {
        var anonimo = Factory.CreateClient();
        var response = await anonimo.GetAsync("/api/clientes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
