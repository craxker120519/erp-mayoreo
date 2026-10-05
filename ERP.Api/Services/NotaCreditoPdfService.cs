using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

public class NotaCreditoPdfService(IConfiguration configuration) : INotaCreditoPdfService
{
    public byte[] Generar(NotaCredito nota)
    {
        var (nombreEmpresa, rfcEmpresa) = PdfCommon.Empresa(configuration);
        var titulo = nota.Estatus == EstatusNotaCredito.Cancelada ? "NOTA DE CRÉDITO — CANCELADA" : "NOTA DE CRÉDITO";

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Purple.Darken2).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(titulo).FontColor(Colors.White).Bold().FontSize(13);
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
                            c.Item().AlignRight().Text($"Folio: {nota.Folio}").Bold();
                            c.Item().AlignRight().Text($"Fecha: {nota.FechaUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                            c.Item().AlignRight().Text($"Contra pedido: {nota.PedidoVenta.Folio}");
                        });
                    });
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Text("Cliente").Bold();
                    col.Item().Text(nota.PedidoVenta.Cliente.Nombre);
                    col.Item().Text($"RFC: {nota.PedidoVenta.Cliente.Rfc ?? "No registrado"}");

                    if (!string.IsNullOrWhiteSpace(nota.Motivo))
                    {
                        col.Item().PaddingTop(5).Text($"Motivo: {nota.Motivo}").Italic();
                    }
                    col.Item().PaddingTop(3).Text(
                        nota.DevuelveInventario
                            ? "El producto regresó físicamente al inventario."
                            : "Ajuste de precio / bonificación — no afecta inventario.")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Producto").Bold();
                            header.Cell().AlignRight().Text("Cantidad").Bold();
                            header.Cell().AlignRight().Text("Precio unitario").Bold();
                            header.Cell().AlignRight().Text("Importe").Bold();
                            header.Cell().ColumnSpan(4).PaddingTop(3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var item in nota.Items)
                        {
                            table.Cell().PaddingVertical(3).Text(item.Producto.Nombre);
                            table.Cell().PaddingVertical(3).AlignRight().Text($"{item.Cantidad} {item.Producto.UnidadVenta}");
                            table.Cell().PaddingVertical(3).AlignRight().Text(item.PrecioUnitario.ToString("C"));
                            table.Cell().PaddingVertical(3).AlignRight().Text(item.Importe.ToString("C"));
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Width(220).Column(totales =>
                    {
                        totales.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal");
                            row.ConstantItem(100).AlignRight().Text(nota.Subtotal.ToString("C"));
                        });
                        totales.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"IVA ({nota.TasaIva:P0})");
                            row.ConstantItem(100).AlignRight().Text(nota.Iva.ToString("C"));
                        });
                        totales.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Darken1).Row(row =>
                        {
                            row.RelativeItem().Text("Total").Bold();
                            row.ConstantItem(100).AlignRight().Text(nota.Total.ToString("C")).Bold();
                        });
                        if (nota.Estatus == EstatusNotaCredito.Emitida)
                        {
                            totales.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Crédito disponible").FontColor(Colors.Grey.Darken2);
                                row.ConstantItem(100).AlignRight().Text(nota.Disponible.ToString("C")).FontColor(Colors.Grey.Darken2);
                            });
                        }
                    });
                });

                PdfCommon.PieDePagina(page);
            });
        });

        return documento.GeneratePdf();
    }
}
