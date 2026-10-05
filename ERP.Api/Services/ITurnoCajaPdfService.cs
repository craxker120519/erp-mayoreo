using ERP.Models;

namespace ERP.Services;

public interface ITurnoCajaPdfService
{
    byte[] Generar(TurnoCaja turno);
}
