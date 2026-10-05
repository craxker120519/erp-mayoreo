using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Services;

// Piezas compartidas entre todos los generadores de PDF (Factura, PedidoVenta, PedidoCompra,
// NotaCredito, Traspaso, VentaMostrador, TurnoCaja) para no repetir el mismo boilerplate de
// QuestPDF seis veces -- cada servicio sigue armando su propio documento, esto solo cubre lo
// que de verdad es idéntico entre todos: el nombre/RFC de la empresa y el pie de página.
public static class PdfCommon
{
    public static (string Nombre, string Rfc) Empresa(IConfiguration configuration) => (
        configuration["Empresa:Nombre"] is { Length: > 0 } n ? n : "(nombre de empresa pendiente)",
        configuration["Empresa:Rfc"] is { Length: > 0 } r ? r : "(RFC pendiente)");

    public static void PieDePagina(PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
        });
    }
}
