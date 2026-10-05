using ERP.Models;

namespace ERP.Services;

public interface ITraspasoPdfService
{
    byte[] Generar(TraspasoAlmacen traspaso);
}
