using ERP.Models;

namespace ERP.Services;

public interface IFacturaPdfService
{
    byte[] Generar(Factura factura);
}
