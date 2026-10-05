using ERP.Models;

namespace ERP.Services;

public interface IVentaMostradorPdfService
{
    byte[] Generar(VentaMostrador venta);
}
