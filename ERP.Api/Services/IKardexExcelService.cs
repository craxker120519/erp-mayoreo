using ERP.Api.Controllers;
using ERP.Models;

namespace ERP.Services;

public interface IKardexExcelService
{
    byte[] Generar(Producto producto, List<KardexMovimiento> movimientos);
}
