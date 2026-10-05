using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

public class TraspasoPdfService(IConfiguration configuration) : ITraspasoPdfService
{
    public byte[] Generar(TraspasoAlmacen traspaso)
    {
        var (nombreEmpresa, rfcEmpresa) = PdfCommon.Empresa(configuration);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Teal.Darken2).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text("COMPROBANTE DE TRASPASO ENTRE ALMACENES")
                            .FontColor(Colors.White).Bold().FontSize(13);
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(nombreEmpresa).Bold().FontSize(13);
                            c.Item().Text($"RFC: {rfcEmpresa}");
                        });
                        row.ConstantItem(180).Column(c =>
                        {
                            c.Item().AlignRight().Text($"Folio: {traspaso.Folio}").Bold();
                            c.Item().AlignRight().Text($"Fecha: {traspaso.FechaUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                        });
                    });
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Almacén origen").Bold();
                            c.Item().Text(traspaso.AlmacenOrigen.Nombre);
                        });
                        row.ConstantItem(30).AlignCenter().Text("→").FontSize(16);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Almacén destino").Bold();
                            c.Item().Text(traspaso.AlmacenDestino.Nombre);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(traspaso.Nota))
                    {
                        col.Item().PaddingTop(10).Text($"Nota: {traspaso.Nota}").Italic();
                    }

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Producto").Bold();
                            header.Cell().AlignRight().Text("Cantidad").Bold();
                            header.Cell().ColumnSpan(2).PaddingTop(3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var item in traspaso.Items)
                        {
                            table.Cell().PaddingVertical(3).Text(item.Producto.Nombre);
                            table.Cell().PaddingVertical(3).AlignRight().Text($"{item.Cantidad} {item.Producto.UnidadVenta}");
                        }
                    });
                });

                PdfCommon.PieDePagina(page);
            });
        });

        return documento.GeneratePdf();
    }
}
