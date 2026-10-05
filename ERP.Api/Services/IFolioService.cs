namespace ERP.Services;

public interface IFolioService
{
    Task<string> GenerarFolioCotizacionAsync();
    Task<string> GenerarFolioPedidoVentaAsync();
    Task<string> GenerarFolioRemisionAsync();
    Task<string> GenerarFolioPedidoCompraAsync();
    Task<string> GenerarFolioVentaMostradorAsync();
    Task<string> GenerarFolioFacturaAsync();
    Task<string> GenerarFolioNotaCreditoAsync();
    Task<string> GenerarFolioTraspasoAsync();
}
