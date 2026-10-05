namespace ERP.Api.Dtos;

public record ItemNotaCreditoRequest(int ProductoId, decimal Cantidad);

public record CrearNotaCreditoRequest(
    int PedidoVentaId, string? Motivo, bool DevuelveInventario, List<ItemNotaCreditoRequest> Items);

public record AplicarCreditoRequest(int PedidoVentaId, decimal Monto);
