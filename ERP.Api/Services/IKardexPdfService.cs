using ERP.Api.Controllers;
using ERP.Models;

namespace ERP.Services;

public interface IKardexPdfService
{
    byte[] Generar(Producto producto, List<KardexMovimiento> movimientos);
}
