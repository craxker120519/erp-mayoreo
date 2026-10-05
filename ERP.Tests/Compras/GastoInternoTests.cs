using System.Net;
using System.Net.Http.Json;
using ERP.Api.Controllers;
using ERP.Api.Dtos;
using ERP.Models;
using ERP.Tests.Infrastructure;

namespace ERP.Tests.Compras;

// Cubre la petición del usuario: no toda orden de compra es mercancía para revender (laptop,
// papelería). Una orden "GastoInterno" no debe requerir producto del catálogo ni afectar
// inventario al recibirse -- a diferencia de una orden normal ("Inventario").
[Collection("Api")]
public class GastoInternoTests(DatabaseFixture fixture)
{
    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task Recibir_una_orden_de_gasto_interno_no_toca_inventario()
    {
        var admin = await AuthHelper.AdminClientAsync(Factory);
        var proveedor = await Seeding.CrearProveedorAsync(admin);
        var producto = await Seeding.CrearProductoAsync(admin);
        await Seeding.DarEntradaStockAsync(admin, proveedor.Id, producto.Id, cantidad: 20, costo: 10);

        var existenciaAntes = await ExistenciaAsync(admin, producto.Id);

        var crear = await admin.PostAsJsonAsync("/api/pedidos-compra", new CrearPedidoCompraRequest(
            proveedor.Id, null, TipoPedidoCompra.GastoInterno, "Equipo de oficina",
            [new ItemCompraRequest(null, "Laptop Dell Latitude", 1, 18000)]));
        crear.EnsureSuccessStatusCode();
        var orden = (await crear.Content.ReadFromJsonAsync<PedidoCompra>())!;
        Assert.Equal(TipoPedidoCompra.GastoInterno, orden.Tipo);
        Assert.Null(orden.Items.First().ProductoId);
        Assert.Equal("Laptop Dell Latitude", orden.Items.First().Descripcion);

        var recibir = await admin.PostAsync($"/api/pedidos-compra/{orden.Id}/recibir", null);
        recibir.EnsureSuccessStatusCode();

        var existenciaDespues = await ExistenciaAsync(admin, producto.Id);
        Assert.Equal(existenciaAntes, existenciaDespues);
    }

    [Fact]
    public async Task Una_orden_de_gasto_interno_sin_descripcion_se_rechaza()
    {
        var admin = await AuthHelper.AdminClientAsync(Factory);
        var proveedor = await Seeding.CrearProveedorAsync(admin);

        var crear = await admin.PostAsJsonAsync("/api/pedidos-compra", new CrearPedidoCompraRequest(
            proveedor.Id, null, TipoPedidoCompra.GastoInterno, null,
            [new ItemCompraRequest(null, null, 1, 100)]));

        Assert.Equal(HttpStatusCode.BadRequest, crear.StatusCode);
    }

    [Fact]
    public async Task Recibir_una_orden_normal_de_inventario_si_suma_existencia()
    {
        var admin = await AuthHelper.AdminClientAsync(Factory);
        var proveedor = await Seeding.CrearProveedorAsync(admin);
        var producto = await Seeding.CrearProductoAsync(admin);

        var existenciaAntes = await ExistenciaAsync(admin, producto.Id);
        await Seeding.DarEntradaStockAsync(admin, proveedor.Id, producto.Id, cantidad: 7, costo: 10);
        var existenciaDespues = await ExistenciaAsync(admin, producto.Id);

        Assert.Equal(existenciaAntes + 7, existenciaDespues);
    }

    private static async Task<decimal> ExistenciaAsync(HttpClient admin, int productoId)
    {
        var response = await admin.GetAsync($"/api/reportes/existencias?productoId={productoId}");
        response.EnsureSuccessStatusCode();
        var existencias = await response.Content.ReadFromJsonAsync<List<ExistenciaReporte>>();
        return existencias!.Sum(e => e.Cantidad);
    }
}
