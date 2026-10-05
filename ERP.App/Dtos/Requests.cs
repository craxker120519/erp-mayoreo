using ERP.Models;

namespace ERP.App.Dtos;

// Mismos contratos que ERP.Api/Dtos/Requests.cs -- se duplican aquí porque MAUI no comparte
// proyecto con el Web API (evita arrastrar dependencias de ASP.NET Core al cliente nativo).
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, List<string> Roles);

public record CrearUsuarioRequest(string Username, string Password, List<string> Roles);
public record ActualizarUsuarioRequest(List<string> Roles, bool Activo);
public record CambiarPasswordRequest(string PasswordNueva);
public record VerificarCandadoRequest(string Nombre, string Password);

public record ClienteRequest(
    string Nombre, string? Rfc, TipoCliente Tipo, string? Telefono, string? Email,
    string? Direccion, int DiasCredito, decimal LimiteCredito);

public record ProveedorRequest(string Nombre, string? Rfc, string? Telefono, string? Email, string? CondicionesPago, int DiasCredito);

public record ProductoRequest(
    string Nombre, string? Codigo, string? Categoria, string UnidadCompra, string UnidadVenta,
    decimal FactorConversion, decimal Precio, decimal Costo, decimal StockMinimo, bool EsPeligroso);

public record EmpleadoRequest(string Nombre);
public record AlmacenRequest(string Nombre);

public record ItemRequest(int ProductoId, decimal Cantidad);
public record ItemCompraRequest(int? ProductoId, string? Descripcion, decimal Cantidad, decimal CostoUnitario);

public record CrearPedidoVentaRequest(int ClienteId, int? AlmacenId, string? Nota, List<ItemRequest> Items);
public record CrearFacturaDirectaRequest(int ClienteId, int? AlmacenId, List<ItemRequest> Items);
public record CrearPedidoCompraRequest(int ProveedorId, int? AlmacenId, TipoPedidoCompra Tipo, string? Nota, List<ItemCompraRequest> Items);
public record RegistrarPagoRequest(decimal Monto, string? MetodoPago);

public record AbrirTurnoRequest(int CajeroEmpleadoId, int? AlmacenId, decimal MontoInicial);
public record CerrarTurnoRequest(decimal MontoContado);
public record CrearVentaMostradorRequest(int? ClienteId, MetodoPago MetodoPago, List<ItemRequest> Items);

public record ExistenciaReporte(string Producto, string Almacen, decimal Cantidad, decimal StockMinimo, string UnidadVenta);
public record VentaPorClienteReporte(string Cliente, int Pedidos, decimal Total);
public record VentaPorProductoReporte(string Producto, decimal Cantidad, decimal Total);
public record UtilidadReporte(string Producto, decimal Cantidad, decimal Ventas, decimal Costo, decimal Utilidad, decimal MargenPct);
public record CarteraVencidaReporte(string Folio, string Cliente, DateTime Fecha, DateTime Vence, decimal Saldo);
public record CuentasPorPagarReporte(string Folio, string Proveedor, DateTime Fecha, DateTime Vence, decimal Saldo);
public record KardexMovimiento(
    DateTime FechaUtc, string Almacen, TipoMovimiento Tipo, decimal Cantidad, decimal Saldo, string? Referencia, string? Nota);

public record CrearFacturaRequest(int PedidoVentaId);

public record ItemTraspasoRequest(int ProductoId, decimal Cantidad);
public record CrearTraspasoRequest(
    int AlmacenOrigenId, int AlmacenDestinoId, string? Nota, List<ItemTraspasoRequest> Items);

public record ItemNotaCreditoRequest(int ProductoId, decimal Cantidad);
public record CrearNotaCreditoRequest(
    int PedidoVentaId, string? Motivo, bool DevuelveInventario, List<ItemNotaCreditoRequest> Items);
public record AplicarCreditoRequest(int PedidoVentaId, decimal Monto);
