using ERP.Models;

namespace ERP.Api.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, List<string> Roles);

public record CrearUsuarioRequest(string Username, string Password, List<string> Roles);
public record ActualizarUsuarioRequest(List<string> Roles, bool Activo);
public record CambiarPasswordRequest(string PasswordNueva);
public record UsuarioResponse(int Id, string Username, List<string> Roles, bool Activo);
public record VerificarCandadoRequest(string Nombre, string Password);

public record ClienteRequest(
    string Nombre, string? Rfc, TipoCliente Tipo, string? Telefono, string? Email,
    string? Direccion, int DiasCredito, decimal LimiteCredito);

public record ProveedorRequest(string Nombre, string? Rfc, string? Telefono, string? Email, string? CondicionesPago, int DiasCredito);

public record ProductoRequest(
    string Nombre, string? Codigo, string? Categoria, string UnidadCompra, string UnidadVenta,
    decimal FactorConversion, decimal Precio, decimal Costo, decimal StockMinimo, bool EsPeligroso);

public record EmpleadoRequest(string Nombre);

public record ItemRequest(int ProductoId, decimal Cantidad);
// ProductoId nulo + Descripcion = línea de un pedido GastoInterno (no existe en el catálogo de
// ventas, ej. "Laptop Dell Latitude"). Con ProductoId, Descripcion se ignora.
public record ItemCompraRequest(int? ProductoId, string? Descripcion, decimal Cantidad, decimal CostoUnitario);

public record CrearPedidoVentaRequest(int ClienteId, int? AlmacenId, string? Nota, List<ItemRequest> Items);
public record CrearFacturaDirectaRequest(int ClienteId, int? AlmacenId, List<ItemRequest> Items);
public record CrearPedidoCompraRequest(int ProveedorId, int? AlmacenId, TipoPedidoCompra Tipo, string? Nota, List<ItemCompraRequest> Items);
public record RegistrarPagoRequest(decimal Monto, string? MetodoPago);

public record AbrirTurnoRequest(int CajeroEmpleadoId, int? AlmacenId, decimal MontoInicial);
public record CerrarTurnoRequest(decimal MontoContado);
public record CrearVentaMostradorRequest(int? ClienteId, MetodoPago MetodoPago, List<ItemRequest> Items);
