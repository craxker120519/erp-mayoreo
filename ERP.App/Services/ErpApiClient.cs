using System.Net.Http.Headers;
using System.Net.Http.Json;
using ERP.App.Dtos;
using ERP.Models;

namespace ERP.App.Services;

public class ErpApiClient(HttpClient http, ServerSettingsService serverSettings, AppSessionService session)
    : IErpApiClient
{
    private string BaseUrl => serverSettings.BaseUrl
        ?? throw new InvalidOperationException("No se ha configurado la dirección del servidor.");

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var response = await http.PostAsJsonAsync($"{BaseUrl}/api/auth/login", new LoginRequest(username, password));
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }

    public Task<List<Cliente>> GetClientesAsync() => GetAsync<List<Cliente>>("/api/clientes");
    public Task<Cliente> CrearClienteAsync(ClienteRequest request) => PostAsync<Cliente>("/api/clientes", request);
    public Task<Cliente> ActualizarClienteAsync(int id, ClienteRequest request) =>
        PutAsync<Cliente>($"/api/clientes/{id}", request);
    public Task<Cliente> AlternarActivoClienteAsync(int id) =>
        PostAsync<Cliente>($"/api/clientes/{id}/alternar-activo", null);

    public Task<List<Proveedor>> GetProveedoresAsync() => GetAsync<List<Proveedor>>("/api/proveedores");
    public Task<Proveedor> CrearProveedorAsync(ProveedorRequest request) =>
        PostAsync<Proveedor>("/api/proveedores", request);
    public Task<Proveedor> ActualizarProveedorAsync(int id, ProveedorRequest request) =>
        PutAsync<Proveedor>($"/api/proveedores/{id}", request);
    public Task<Proveedor> AlternarActivoProveedorAsync(int id) =>
        PostAsync<Proveedor>($"/api/proveedores/{id}/alternar-activo", null);

    public Task<List<Empleado>> GetEmpleadosAsync() => GetAsync<List<Empleado>>("/api/empleados");
    public Task<Empleado> CrearEmpleadoAsync(EmpleadoRequest request) => PostAsync<Empleado>("/api/empleados", request);
    public Task<Empleado> ActualizarEmpleadoAsync(int id, EmpleadoRequest request) =>
        PutAsync<Empleado>($"/api/empleados/{id}", request);
    public Task<Empleado> AlternarActivoEmpleadoAsync(int id) =>
        PostAsync<Empleado>($"/api/empleados/{id}/alternar-activo", null);

    public Task<List<Almacen>> GetAlmacenesAsync() => GetAsync<List<Almacen>>("/api/almacenes");
    public Task<Almacen> CrearAlmacenAsync(AlmacenRequest request) => PostAsync<Almacen>("/api/almacenes", request);
    public Task<Almacen> AlternarActivoAlmacenAsync(int id) =>
        PostAsync<Almacen>($"/api/almacenes/{id}/alternar-activo", null);

    public Task<List<Producto>> GetProductosAsync() => GetAsync<List<Producto>>("/api/productos");
    public Task<Producto> GetProductoAsync(int id) => GetAsync<Producto>($"/api/productos/{id}");
    public Task<Producto> CrearProductoAsync(ProductoRequest request) => PostAsync<Producto>("/api/productos", request);
    public Task<Producto> ActualizarProductoAsync(int id, ProductoRequest request) =>
        PutAsync<Producto>($"/api/productos/{id}", request);
    public Task<Producto> AlternarActivoProductoAsync(int id) =>
        PostAsync<Producto>($"/api/productos/{id}/alternar-activo", null);

    public async Task<Producto> SubirHojaSeguridadAsync(int id, Stream contenido, string nombreArchivo)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(contenido);
        content.Add(fileContent, "archivo", nombreArchivo);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/productos/{id}/hoja-seguridad")
        {
            Content = content
        };
        AddAuth(request);

        var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<Producto>())!;
    }

    public Task<List<PedidoVenta>> GetPedidosVentaAsync() => GetAsync<List<PedidoVenta>>("/api/pedidos-venta");
    public Task<PedidoVenta> GetPedidoVentaAsync(int id) => GetAsync<PedidoVenta>($"/api/pedidos-venta/{id}");
    public Task<PedidoVenta> CrearPedidoVentaAsync(CrearPedidoVentaRequest request) =>
        PostAsync<PedidoVenta>("/api/pedidos-venta", request);
    public Task<PedidoVenta> ConvertirAPedidoAsync(int id) =>
        PostAsync<PedidoVenta>($"/api/pedidos-venta/{id}/convertir-a-pedido", null);
    public Task<PedidoVenta> RemisionarAsync(int id) =>
        PostAsync<PedidoVenta>($"/api/pedidos-venta/{id}/remisionar", null);
    public Task<PedidoVenta> CancelarPedidoVentaAsync(int id) =>
        PostAsync<PedidoVenta>($"/api/pedidos-venta/{id}/cancelar", null);
    public Task<PedidoVenta> RegistrarPagoAsync(int id, RegistrarPagoRequest request) =>
        PostAsync<PedidoVenta>($"/api/pedidos-venta/{id}/pagos", request);
    public Task<byte[]> GetPedidoVentaPdfAsync(int id) => GetBytesAsync($"/api/pedidos-venta/{id}/pdf");
    public Task<PedidoVenta> ActualizarItemsPedidoVentaAsync(int id, List<ItemRequest> items) =>
        PutAsync<PedidoVenta>($"/api/pedidos-venta/{id}/items", items);

    public Task<List<PedidoCompra>> GetPedidosCompraAsync() => GetAsync<List<PedidoCompra>>("/api/pedidos-compra");
    public Task<PedidoCompra> GetPedidoCompraAsync(int id) => GetAsync<PedidoCompra>($"/api/pedidos-compra/{id}");
    public Task<PedidoCompra> CrearPedidoCompraAsync(CrearPedidoCompraRequest request) =>
        PostAsync<PedidoCompra>("/api/pedidos-compra", request);
    public Task<PedidoCompra> RecibirPedidoCompraAsync(int id) =>
        PostAsync<PedidoCompra>($"/api/pedidos-compra/{id}/recibir", null);
    public Task<PedidoCompra> CancelarPedidoCompraAsync(int id) =>
        PostAsync<PedidoCompra>($"/api/pedidos-compra/{id}/cancelar", null);
    public Task<byte[]> GetPedidoCompraPdfAsync(int id) => GetBytesAsync($"/api/pedidos-compra/{id}/pdf");
    public Task<PedidoCompra> ActualizarItemsPedidoCompraAsync(int id, List<ItemCompraRequest> items) =>
        PutAsync<PedidoCompra>($"/api/pedidos-compra/{id}/items", items);
    public Task<PedidoCompra> RegistrarPagoProveedorAsync(int id, RegistrarPagoRequest request) =>
        PostAsync<PedidoCompra>($"/api/pedidos-compra/{id}/pagos", request);

    public async Task<TurnoCaja?> GetTurnoActualAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/turnos-caja/actual");
        AddAuth(request);
        var response = await http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TurnoCaja>();
    }

    public Task<List<TurnoCaja>> GetCortesCajaAsync() => GetAsync<List<TurnoCaja>>("/api/turnos-caja/cerrados");
    public Task<TurnoCaja> AbrirTurnoAsync(AbrirTurnoRequest request) =>
        PostAsync<TurnoCaja>("/api/turnos-caja/abrir", request);
    public Task<TurnoCaja> CerrarTurnoAsync(int id, CerrarTurnoRequest request) =>
        PostAsync<TurnoCaja>($"/api/turnos-caja/{id}/cerrar", request);
    public Task<VentaMostrador> RegistrarVentaMostradorAsync(int turnoId, CrearVentaMostradorRequest request) =>
        PostAsync<VentaMostrador>($"/api/turnos-caja/{turnoId}/ventas", request);
    public Task<byte[]> GetTurnoCajaPdfAsync(int id) => GetBytesAsync($"/api/turnos-caja/{id}/pdf");
    public Task<byte[]> GetVentaMostradorPdfAsync(int id) => GetBytesAsync($"/api/turnos-caja/ventas/{id}/pdf");

    public Task<List<Factura>> GetFacturasAsync() => GetAsync<List<Factura>>("/api/facturas");
    public Task<Factura> GetFacturaAsync(int id) => GetAsync<Factura>($"/api/facturas/{id}");
    public Task<Factura> CrearFacturaAsync(int pedidoVentaId) =>
        PostAsync<Factura>("/api/facturas", new CrearFacturaRequest(pedidoVentaId));
    public Task<Factura> CrearFacturaDirectaAsync(CrearFacturaDirectaRequest request) =>
        PostAsync<Factura>("/api/facturas/directa", request);
    public Task<Factura> CancelarFacturaAsync(int id) =>
        PostAsync<Factura>($"/api/facturas/{id}/cancelar", null);

    public Task<byte[]> GetFacturaPdfAsync(int id) => GetBytesAsync($"/api/facturas/{id}/pdf");

    public Task<List<NotaCredito>> GetNotasCreditoAsync() => GetAsync<List<NotaCredito>>("/api/notas-credito");
    public Task<NotaCredito> CrearNotaCreditoAsync(CrearNotaCreditoRequest request) =>
        PostAsync<NotaCredito>("/api/notas-credito", request);
    public Task<NotaCredito> CancelarNotaCreditoAsync(int id) =>
        PostAsync<NotaCredito>($"/api/notas-credito/{id}/cancelar", null);
    public Task<byte[]> GetNotaCreditoPdfAsync(int id) => GetBytesAsync($"/api/notas-credito/{id}/pdf");
    public Task<NotaCredito> AplicarCreditoAsync(int notaCreditoId, AplicarCreditoRequest request) =>
        PostAsync<NotaCredito>($"/api/notas-credito/{notaCreditoId}/aplicar", request);
    public Task<List<NotaCredito>> GetCreditoDisponibleClienteAsync(int clienteId) =>
        GetAsync<List<NotaCredito>>($"/api/notas-credito/disponibles-cliente/{clienteId}");

    public Task<List<TraspasoAlmacen>> GetTraspasosAsync() => GetAsync<List<TraspasoAlmacen>>("/api/traspasos");
    public Task<TraspasoAlmacen> CrearTraspasoAsync(CrearTraspasoRequest request) =>
        PostAsync<TraspasoAlmacen>("/api/traspasos", request);
    public Task<byte[]> GetTraspasoPdfAsync(int id) => GetBytesAsync($"/api/traspasos/{id}/pdf");

    public Task<List<ExistenciaReporte>> GetExistenciasAsync() =>
        GetAsync<List<ExistenciaReporte>>("/api/reportes/existencias");
    public Task<List<ExistenciaReporte>> GetExistenciasProductoAsync(int productoId) =>
        GetAsync<List<ExistenciaReporte>>($"/api/reportes/existencias?productoId={productoId}");
    public Task<List<VentaPorClienteReporte>> GetVentasPorClienteAsync() =>
        GetAsync<List<VentaPorClienteReporte>>("/api/reportes/ventas-por-cliente");
    public Task<List<VentaPorProductoReporte>> GetVentasPorProductoAsync() =>
        GetAsync<List<VentaPorProductoReporte>>("/api/reportes/ventas-por-producto");
    public Task<List<UtilidadReporte>> GetUtilidadAsync() =>
        GetAsync<List<UtilidadReporte>>("/api/reportes/utilidad");
    public Task<List<CarteraVencidaReporte>> GetCarteraVencidaAsync() =>
        GetAsync<List<CarteraVencidaReporte>>("/api/reportes/cartera-vencida");
    public Task<List<CuentasPorPagarReporte>> GetCuentasPorPagarAsync() =>
        GetAsync<List<CuentasPorPagarReporte>>("/api/reportes/cuentas-por-pagar");
    public Task<List<KardexMovimiento>> GetKardexAsync(int productoId, List<int>? almacenIds) =>
        GetAsync<List<KardexMovimiento>>($"/api/reportes/kardex/{productoId}{QueryAlmacenIds(almacenIds)}");
    public Task<byte[]> GetKardexPdfAsync(int productoId, List<int>? almacenIds) =>
        GetBytesAsync($"/api/reportes/kardex/{productoId}/pdf{QueryAlmacenIds(almacenIds)}");
    public Task<byte[]> GetKardexExcelAsync(int productoId, List<int>? almacenIds) =>
        GetBytesAsync($"/api/reportes/kardex/{productoId}/excel{QueryAlmacenIds(almacenIds)}");

    private static string QueryAlmacenIds(List<int>? almacenIds) =>
        almacenIds is { Count: > 0 } ? "?" + string.Join("&", almacenIds.Select(id => $"almacenIds={id}")) : "";

    public Task<List<BitacoraEvento>> GetBitacoraAsync() => GetAsync<List<BitacoraEvento>>("/api/bitacora");

    public Task<List<Usuario>> GetUsuariosAsync() => GetAsync<List<Usuario>>("/api/usuarios");
    public Task<Usuario> CrearUsuarioAsync(CrearUsuarioRequest request) => PostAsync<Usuario>("/api/usuarios", request);
    public Task<Usuario> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequest request) =>
        PutAsync<Usuario>($"/api/usuarios/{id}", request);
    public Task<Usuario> CambiarPasswordUsuarioAsync(int id, CambiarPasswordRequest request) =>
        PostAsync<Usuario>($"/api/usuarios/{id}/cambiar-password", request);

    public Task<bool> VerificarCandadoAsync(string nombre, string password) =>
        PostAsync<bool>("/api/candados/verificar", new VerificarCandadoRequest(nombre, password));
    public async Task CambiarPasswordCandadoAsync(string nombre, CambiarPasswordRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/api/candados/{nombre}") { Content = JsonContent.Create(request) };
        AddAuth(httpRequest);
        var response = await http.SendAsync(httpRequest);
        await EnsureSuccessAsync(response);
    }

    private async Task<byte[]> GetBytesAsync(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{path}");
        AddAuth(request);
        var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<T> GetAsync<T>(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{path}");
        AddAuth(request);
        var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> PostAsync<T>(string path, object? body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}")
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        AddAuth(request);
        var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> PutAsync<T>(string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}{path}") { Content = JsonContent.Create(body) };
        AddAuth(request);
        var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private void AddAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(session.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // El JWT expiró o ya no es válido -- cierra la sesión localmente para que
            // MainLayout regrese a LoginView en vez de mostrar un error genérico repetido.
            session.Logout();
            throw new HttpRequestException("Tu sesión expiró. Vuelve a iniciar sesión.");
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
    }
}
