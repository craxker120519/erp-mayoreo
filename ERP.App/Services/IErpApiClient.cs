using ERP.App.Dtos;
using ERP.Models;

namespace ERP.App.Services;

public interface IErpApiClient
{
    Task<LoginResponse?> LoginAsync(string username, string password);

    Task<List<Cliente>> GetClientesAsync();
    Task<Cliente> CrearClienteAsync(ClienteRequest request);
    Task<Cliente> ActualizarClienteAsync(int id, ClienteRequest request);
    Task<Cliente> AlternarActivoClienteAsync(int id);

    Task<List<Proveedor>> GetProveedoresAsync();
    Task<Proveedor> CrearProveedorAsync(ProveedorRequest request);
    Task<Proveedor> ActualizarProveedorAsync(int id, ProveedorRequest request);
    Task<Proveedor> AlternarActivoProveedorAsync(int id);

    Task<List<Empleado>> GetEmpleadosAsync();
    Task<Empleado> CrearEmpleadoAsync(EmpleadoRequest request);
    Task<Empleado> ActualizarEmpleadoAsync(int id, EmpleadoRequest request);
    Task<Empleado> AlternarActivoEmpleadoAsync(int id);

    Task<List<Almacen>> GetAlmacenesAsync();
    Task<Almacen> CrearAlmacenAsync(AlmacenRequest request);
    Task<Almacen> AlternarActivoAlmacenAsync(int id);

    Task<List<Producto>> GetProductosAsync();
    Task<Producto> GetProductoAsync(int id);
    Task<Producto> CrearProductoAsync(ProductoRequest request);
    Task<Producto> ActualizarProductoAsync(int id, ProductoRequest request);
    Task<Producto> AlternarActivoProductoAsync(int id);
    Task<Producto> SubirHojaSeguridadAsync(int id, Stream contenido, string nombreArchivo);

    Task<List<PedidoVenta>> GetPedidosVentaAsync();
    Task<PedidoVenta> GetPedidoVentaAsync(int id);
    Task<PedidoVenta> CrearPedidoVentaAsync(CrearPedidoVentaRequest request);
    Task<PedidoVenta> ConvertirAPedidoAsync(int id);
    Task<PedidoVenta> RemisionarAsync(int id);
    Task<PedidoVenta> CancelarPedidoVentaAsync(int id);
    Task<PedidoVenta> RegistrarPagoAsync(int id, RegistrarPagoRequest request);
    Task<byte[]> GetPedidoVentaPdfAsync(int id);
    Task<PedidoVenta> ActualizarItemsPedidoVentaAsync(int id, List<ItemRequest> items);

    Task<List<PedidoCompra>> GetPedidosCompraAsync();
    Task<PedidoCompra> GetPedidoCompraAsync(int id);
    Task<PedidoCompra> CrearPedidoCompraAsync(CrearPedidoCompraRequest request);
    Task<PedidoCompra> RecibirPedidoCompraAsync(int id);
    Task<PedidoCompra> CancelarPedidoCompraAsync(int id);
    Task<byte[]> GetPedidoCompraPdfAsync(int id);
    Task<PedidoCompra> ActualizarItemsPedidoCompraAsync(int id, List<ItemCompraRequest> items);
    Task<PedidoCompra> RegistrarPagoProveedorAsync(int id, RegistrarPagoRequest request);

    Task<TurnoCaja?> GetTurnoActualAsync();
    Task<List<TurnoCaja>> GetCortesCajaAsync();
    Task<TurnoCaja> AbrirTurnoAsync(AbrirTurnoRequest request);
    Task<TurnoCaja> CerrarTurnoAsync(int id, CerrarTurnoRequest request);
    Task<VentaMostrador> RegistrarVentaMostradorAsync(int turnoId, CrearVentaMostradorRequest request);
    Task<byte[]> GetTurnoCajaPdfAsync(int id);
    Task<byte[]> GetVentaMostradorPdfAsync(int id);

    Task<List<Factura>> GetFacturasAsync();
    Task<Factura> GetFacturaAsync(int id);
    Task<Factura> CrearFacturaAsync(int pedidoVentaId);
    Task<Factura> CrearFacturaDirectaAsync(CrearFacturaDirectaRequest request);
    Task<Factura> CancelarFacturaAsync(int id);
    Task<byte[]> GetFacturaPdfAsync(int id);

    Task<List<NotaCredito>> GetNotasCreditoAsync();
    Task<NotaCredito> CrearNotaCreditoAsync(CrearNotaCreditoRequest request);
    Task<NotaCredito> CancelarNotaCreditoAsync(int id);
    Task<byte[]> GetNotaCreditoPdfAsync(int id);
    Task<NotaCredito> AplicarCreditoAsync(int notaCreditoId, AplicarCreditoRequest request);
    Task<List<NotaCredito>> GetCreditoDisponibleClienteAsync(int clienteId);

    Task<List<TraspasoAlmacen>> GetTraspasosAsync();
    Task<TraspasoAlmacen> CrearTraspasoAsync(CrearTraspasoRequest request);
    Task<byte[]> GetTraspasoPdfAsync(int id);

    Task<List<ExistenciaReporte>> GetExistenciasAsync();
    Task<List<ExistenciaReporte>> GetExistenciasProductoAsync(int productoId);
    Task<List<VentaPorClienteReporte>> GetVentasPorClienteAsync();
    Task<List<VentaPorProductoReporte>> GetVentasPorProductoAsync();
    Task<List<UtilidadReporte>> GetUtilidadAsync();
    Task<List<CarteraVencidaReporte>> GetCarteraVencidaAsync();
    Task<List<CuentasPorPagarReporte>> GetCuentasPorPagarAsync();
    Task<List<KardexMovimiento>> GetKardexAsync(int productoId, List<int>? almacenIds);
    Task<byte[]> GetKardexPdfAsync(int productoId, List<int>? almacenIds);
    Task<byte[]> GetKardexExcelAsync(int productoId, List<int>? almacenIds);

    Task<List<BitacoraEvento>> GetBitacoraAsync();

    Task<List<Usuario>> GetUsuariosAsync();
    Task<Usuario> CrearUsuarioAsync(CrearUsuarioRequest request);
    Task<Usuario> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequest request);
    Task<Usuario> CambiarPasswordUsuarioAsync(int id, CambiarPasswordRequest request);

    Task<bool> VerificarCandadoAsync(string nombre, string password);
    Task CambiarPasswordCandadoAsync(string nombre, CambiarPasswordRequest request);
}
