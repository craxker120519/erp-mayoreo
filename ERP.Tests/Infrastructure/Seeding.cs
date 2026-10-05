using System.Net.Http.Json;
using ERP.Api.Dtos;
using ERP.Models;

namespace ERP.Tests.Infrastructure;

// Atajos para armar los datos base que casi toda prueba necesita (producto, cliente, proveedor,
// stock) -- siempre vía la API real (POST), nunca tocando la base directo, para que las pruebas
// ejerciten exactamente el mismo camino que usa la app de verdad.
public static class Seeding
{
    public static async Task<Producto> CrearProductoAsync(
        HttpClient admin, decimal precio = 100m, decimal costo = 50m, decimal stockMinimo = 0m)
    {
        var response = await admin.PostAsJsonAsync("/api/productos", new ProductoRequest(
            Unique.Nombre("Producto"), null, null, "Pieza", "Pieza", 1, precio, costo, stockMinimo, false));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Producto>())!;
    }

    public static async Task<Cliente> CrearClienteAsync(
        HttpClient admin, decimal limiteCredito = 0m, int diasCredito = 0)
    {
        var response = await admin.PostAsJsonAsync("/api/clientes", new ClienteRequest(
            Unique.Nombre("Cliente"), null, TipoCliente.Mayoreo, null, null, null, diasCredito, limiteCredito));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Cliente>())!;
    }

    public static async Task<Proveedor> CrearProveedorAsync(HttpClient admin, int diasCredito = 0)
    {
        var response = await admin.PostAsJsonAsync("/api/proveedores", new ProveedorRequest(
            Unique.Nombre("Proveedor"), null, null, null, null, diasCredito));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Proveedor>())!;
    }

    // Crea y recibe una orden de compra (tipo Inventario) para que el producto tenga existencia
    // real con la que se pueda remisionar -- es el único camino real para dar entrada a stock.
    public static async Task DarEntradaStockAsync(
        HttpClient admin, int proveedorId, int productoId, decimal cantidad, decimal costo)
    {
        var crear = await admin.PostAsJsonAsync("/api/pedidos-compra", new CrearPedidoCompraRequest(
            proveedorId, null, TipoPedidoCompra.Inventario, "Stock para pruebas",
            [new ItemCompraRequest(productoId, null, cantidad, costo)]));
        crear.EnsureSuccessStatusCode();
        var pedido = (await crear.Content.ReadFromJsonAsync<PedidoCompra>())!;

        var recibir = await admin.PostAsync($"/api/pedidos-compra/{pedido.Id}/recibir", null);
        recibir.EnsureSuccessStatusCode();
    }
}
