using System.Net;
using System.Net.Http.Json;
using ERP.Api.Controllers;
using ERP.Api.Dtos;
using ERP.Models;
using ERP.Tests.Infrastructure;

namespace ERP.Tests.Ventas;

// Cubre el rediseño más grande de la sesión: Cotización/Pedido/Remisión pasaron de ser un solo
// PedidoVenta cambiando de Estatus a ser documentos SEPARADOS con folio propio (COT/PV/REM),
// encadenados por DocumentoOrigenId, cada uno conservado intacto aunque la venta avance. También
// cubre el bug real que salió de ese rediseño: el ciclo genuino entre DocumentoOrigen y
// DocumentosGenerados hacía que el serializador colara un `null` dentro de una colección con
// Count > 0 -- por eso PedidoVenta.Cadena (aplanada, calculada en el servidor) es la que se
// verifica aquí, no las propiedades de navegación crudas.
[Collection("Api")]
public class DocumentChainTests(DatabaseFixture fixture)
{
    private ApiFactory Factory => fixture.Factory;

    private async Task<(HttpClient Admin, Cliente Cliente, Producto Producto)> PrepararAsync(decimal limiteCredito = 0m)
    {
        var admin = await AuthHelper.AdminClientAsync(Factory);
        var cliente = await Seeding.CrearClienteAsync(admin, limiteCredito);
        var proveedor = await Seeding.CrearProveedorAsync(admin);
        var producto = await Seeding.CrearProductoAsync(admin, precio: 100m, costo: 60m);
        await Seeding.DarEntradaStockAsync(admin, proveedor.Id, producto.Id, cantidad: 50, costo: 60);
        return (admin, cliente, producto);
    }

    [Fact]
    public async Task Nueva_cotizacion_tiene_folio_COT_y_estatus_cotizacion()
    {
        var (admin, cliente, producto) = await PrepararAsync();

        var response = await admin.PostAsJsonAsync("/api/pedidos-venta",
            new CrearPedidoVentaRequest(cliente.Id, null, null, [new ItemRequest(producto.Id, 1)]));
        var cotizacion = (await response.Content.ReadFromJsonAsync<PedidoVenta>())!;

        Assert.StartsWith("COT", cotizacion.Folio);
        Assert.Equal(EstatusPedidoVenta.Cotizacion, cotizacion.Estatus);
    }

    [Fact]
    public async Task Convertir_a_pedido_crea_documento_nuevo_y_conserva_la_cotizacion_intacta()
    {
        var (admin, cliente, producto) = await PrepararAsync();
        var cotizacion = await CrearCotizacionAsync(admin, cliente.Id, producto.Id, 1);

        var pedido = await ConvertirAsync(admin, cotizacion.Id);

        Assert.StartsWith("PV", pedido.Folio);
        Assert.NotEqual(cotizacion.Id, pedido.Id);
        Assert.Equal(cotizacion.Id, pedido.DocumentoOrigenId);

        var cotizacionRecargada = await ObtenerAsync(admin, cotizacion.Id);
        Assert.Equal(EstatusPedidoVenta.Cotizacion, cotizacionRecargada.Estatus);
        Assert.Single(cotizacionRecargada.DocumentosGenerados);
        Assert.Equal(pedido.Id, cotizacionRecargada.DocumentosGenerados.First().Id);
    }

    [Fact]
    public async Task No_se_puede_editar_ni_reconvertir_una_cotizacion_ya_convertida()
    {
        var (admin, cliente, producto) = await PrepararAsync();
        var cotizacion = await CrearCotizacionAsync(admin, cliente.Id, producto.Id, 1);
        await ConvertirAsync(admin, cotizacion.Id);

        var editar = await admin.PutAsJsonAsync($"/api/pedidos-venta/{cotizacion.Id}/items",
            new List<ItemRequest> { new(producto.Id, 5) });
        var reconvertir = await admin.PostAsync($"/api/pedidos-venta/{cotizacion.Id}/convertir-a-pedido", null);
        var cancelar = await admin.PostAsync($"/api/pedidos-venta/{cotizacion.Id}/cancelar", null);

        Assert.Equal(HttpStatusCode.BadRequest, editar.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reconvertir.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cancelar.StatusCode);
    }

    [Fact]
    public async Task Remisionar_crea_documento_REM_y_descuenta_inventario_exacto()
    {
        var (admin, cliente, producto) = await PrepararAsync();
        var cotizacion = await CrearCotizacionAsync(admin, cliente.Id, producto.Id, 10);
        var pedido = await ConvertirAsync(admin, cotizacion.Id);

        var antes = await ExistenciaAsync(admin, producto.Id);
        var remision = await RemisionarAsync(admin, pedido.Id);
        var despues = await ExistenciaAsync(admin, producto.Id);

        Assert.StartsWith("REM", remision.Folio);
        Assert.Equal(EstatusPedidoVenta.Remisionado, remision.Estatus);
        Assert.Equal(antes - 10, despues);

        var reremisionar = await admin.PostAsync($"/api/pedidos-venta/{pedido.Id}/remisionar", null);
        Assert.Equal(HttpStatusCode.BadRequest, reremisionar.StatusCode);
    }

    [Fact]
    public async Task La_cadena_es_igual_e_incluye_los_3_documentos_sin_importar_cual_se_consulte()
    {
        var (admin, cliente, producto) = await PrepararAsync();
        var cotizacion = await CrearCotizacionAsync(admin, cliente.Id, producto.Id, 1);
        var pedido = await ConvertirAsync(admin, cotizacion.Id);
        var remision = await RemisionarAsync(admin, pedido.Id);

        foreach (var id in new[] { cotizacion.Id, pedido.Id, remision.Id })
        {
            var documento = await ObtenerAsync(admin, id);
            Assert.NotNull(documento.Cadena);
            Assert.Equal(3, documento.Cadena!.Count);
            Assert.Equal([cotizacion.Id, pedido.Id, remision.Id], documento.Cadena.Select(c => c.Id));
        }
    }

    [Fact]
    public async Task El_limite_de_credito_no_cuenta_la_misma_venta_varias_veces()
    {
        // Antes de la corrección, sumar el saldo pendiente del cliente sin filtrar
        // "!DocumentosGenerados.Any()" habría contado la cotización, el pedido Y la remisión de la
        // MISMA venta como 3 compromisos separados -- $270 en vez de $90 para este caso.
        var (admin, cliente, producto) = await PrepararAsync(limiteCredito: 100m);
        // Producto a $100 c/u (ver PrepararAsync) x 3 = $300, por arriba del límite de $100 --
        // debe rechazar contando esto UNA sola vez ($300), no 3 veces ($900) por tener 3
        // documentos (cotización+pedido+remisión) en su cadena.
        var cotizacion = await CrearCotizacionAsync(admin, cliente.Id, producto.Id, 3);
        var pedido = await ConvertirAsync(admin, cotizacion.Id);

        var remisionar = await admin.PostAsync($"/api/pedidos-venta/{pedido.Id}/remisionar", null);

        Assert.Equal(HttpStatusCode.BadRequest, remisionar.StatusCode);
        var mensaje = await remisionar.Content.ReadAsStringAsync();
        Assert.Contains("límite de crédito", mensaje);
        Assert.Contains("300", mensaje);
        Assert.DoesNotContain("900", mensaje);
    }

    [Fact]
    public async Task Los_pagos_solo_se_aceptan_contra_una_remision()
    {
        var (admin, cliente, producto) = await PrepararAsync();
        var cotizacion = await CrearCotizacionAsync(admin, cliente.Id, producto.Id, 1);
        var pedido = await ConvertirAsync(admin, cotizacion.Id);
        var remision = await RemisionarAsync(admin, pedido.Id);

        var pagoContraCotizacion = await admin.PostAsJsonAsync($"/api/pedidos-venta/{cotizacion.Id}/pagos",
            new RegistrarPagoRequest(10, "Efectivo"));
        var pagoContraRemision = await admin.PostAsJsonAsync($"/api/pedidos-venta/{remision.Id}/pagos",
            new RegistrarPagoRequest(remision.Total, "Efectivo"));

        Assert.Equal(HttpStatusCode.BadRequest, pagoContraCotizacion.StatusCode);
        Assert.Equal(HttpStatusCode.OK, pagoContraRemision.StatusCode);
    }

    private static async Task<PedidoVenta> CrearCotizacionAsync(HttpClient admin, int clienteId, int productoId, decimal cantidad)
    {
        var response = await admin.PostAsJsonAsync("/api/pedidos-venta",
            new CrearPedidoVentaRequest(clienteId, null, null, [new ItemRequest(productoId, cantidad)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoVenta>())!;
    }

    private static async Task<PedidoVenta> ConvertirAsync(HttpClient admin, int cotizacionId)
    {
        var response = await admin.PostAsync($"/api/pedidos-venta/{cotizacionId}/convertir-a-pedido", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoVenta>())!;
    }

    private static async Task<PedidoVenta> RemisionarAsync(HttpClient admin, int pedidoId)
    {
        var response = await admin.PostAsync($"/api/pedidos-venta/{pedidoId}/remisionar", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoVenta>())!;
    }

    private static async Task<PedidoVenta> ObtenerAsync(HttpClient admin, int id)
    {
        var response = await admin.GetAsync($"/api/pedidos-venta/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PedidoVenta>())!;
    }

    private static async Task<decimal> ExistenciaAsync(HttpClient admin, int productoId)
    {
        var response = await admin.GetAsync($"/api/reportes/existencias?productoId={productoId}");
        response.EnsureSuccessStatusCode();
        var existencias = await response.Content.ReadFromJsonAsync<List<ExistenciaReporte>>();
        return existencias!.Sum(e => e.Cantidad);
    }
}
