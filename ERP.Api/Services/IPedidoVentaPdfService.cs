using ERP.Models;

namespace ERP.Services;

public interface IPedidoVentaPdfService
{
    byte[] Generar(PedidoVenta pedido);
}
