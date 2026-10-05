using ERP.Models;

namespace ERP.Services;

public interface INotaCreditoPdfService
{
    byte[] Generar(NotaCredito nota);
}
