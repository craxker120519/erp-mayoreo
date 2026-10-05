using ERP.Models;

namespace ERP.Services;

public interface IPedidoCompraPdfService
{
    byte[] Generar(PedidoCompra pedido);
}
