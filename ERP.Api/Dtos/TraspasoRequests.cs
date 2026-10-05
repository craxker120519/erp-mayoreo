namespace ERP.Api.Dtos;

public record ItemTraspasoRequest(int ProductoId, decimal Cantidad);

public record CrearTraspasoRequest(
    int AlmacenOrigenId, int AlmacenDestinoId, string? Nota, List<ItemTraspasoRequest> Items);
