using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

// Formato tipo ticket de punto de venta (ancho fijo de rollo de 80mm, alto libre según el
// contenido) -- distinto del resto de los PDFs, que son documentos tamaño carta.
public class VentaMostradorPdfService(IConfiguration configuration) : IVentaMostradorPdfService
{
    public byte[] Generar(VentaMostrador venta)
    {
        var (nombreEmpresa, rfcEmpresa) = PdfCommon.Empresa(configuration);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.ContinuousSize(80, Unit.Millimetre);
                page.Margin(10);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text(nombreEmpresa).Bold().FontSize(11);
                    col.Item().AlignCenter().Text($"RFC: {rfcEmpresa}").FontSize(8);
                    col.Item().PaddingTop(5).AlignCenter().Text("TICKET DE VENTA").Bold();
                    col.Item().AlignCenter().Text($"Folio: {venta.Folio}");
                    col.Item().AlignCenter().Text($"{venta.FechaUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                    if (venta.Cliente is not null)
                    {
                        col.Item().AlignCenter().Text($"Cliente: {venta.Cliente.Nombre}");
                    }

                    col.Item().PaddingVertical(6).BorderBottom(1).BorderColor(Colors.Grey.Darken1);

                    foreach (var item in venta.Items)
                    {
                        col.Item().Text(item.Producto.Nombre).FontSize(9);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{item.Cantidad} {item.Producto.UnidadVenta} x {item.PrecioUnitario:C}");
                            row.ConstantItem(60).AlignRight().Text(item.Importe.ToString("C"));
                        });
                    }

                    col.Item().PaddingVertical(6).BorderBottom(1).BorderColor(Colors.Grey.Darken1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").Bold();
                        row.ConstantItem(60).AlignRight().Text(venta.Total.ToString("C")).Bold();
                    });
                    col.Item().PaddingTop(3).Text($"Método de pago: {venta.MetodoPago}");

                    col.Item().PaddingTop(10).AlignCenter().Text("¡Gracias por su compra!").FontSize(8).Italic();
                });
            });
        });

        return documento.GeneratePdf();
    }
}
