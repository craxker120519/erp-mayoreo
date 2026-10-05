using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

// Genera el PDF interno de la Fase pre-CFDI (ver Factura.EstatusFactura). Cuando se conecte un
// PAC real, este servicio deja de ser el generador final -- el PAC devuelve su propio PDF/XML
// timbrado y este solo serviría como respaldo/borrador si hiciera falta.
public class FacturaPdfService(IConfiguration configuration) : IFacturaPdfService
{
    public byte[] Generar(Factura factura)
    {
        var nombreEmpresa = configuration["Empresa:Nombre"] is { Length: > 0 } n ? n : "(nombre de empresa pendiente)";
        var rfcEmpresa = configuration["Empresa:Rfc"] is { Length: > 0 } r ? r : "(RFC pendiente)";

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Red.Darken1).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text("COMPROBANTE INTERNO — NO VÁLIDO PARA EFECTOS FISCALES")
                            .FontColor(Colors.White).Bold().FontSize(11);
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
                            c.Item().AlignRight().Text($"Folio: {factura.Folio}").Bold();
                            c.Item().AlignRight().Text($"Fecha: {factura.FechaEmisionUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                            c.Item().AlignRight().Text($"Pedido: {factura.PedidoVenta.Folio}");
                        });
                    });
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Text("Cliente").Bold();
                    col.Item().Text(factura.PedidoVenta.Cliente.Nombre);
                    col.Item().Text($"RFC: {factura.PedidoVenta.Cliente.Rfc ?? "No registrado"}");
                    if (!string.IsNullOrWhiteSpace(factura.PedidoVenta.Cliente.Direccion))
                    {
                        col.Item().Text(factura.PedidoVenta.Cliente.Direccion!);
                    }

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

                        foreach (var item in factura.PedidoVenta.Items)
                        {
                            table.Cell().PaddingVertical(3).Text($"{item.Producto.Nombre}");
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
                            row.ConstantItem(100).AlignRight().Text(factura.Subtotal.ToString("C"));
                        });
                        totales.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"IVA ({factura.TasaIva:P0})");
                            row.ConstantItem(100).AlignRight().Text(factura.Iva.ToString("C"));
                        });
                        totales.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Darken1).Row(row =>
                        {
                            row.RelativeItem().Text("Total").Bold();
                            row.ConstantItem(100).AlignRight().Text(factura.Total.ToString("C")).Bold();
                        });
                    });

                    col.Item().PaddingTop(30).Background(Colors.Grey.Lighten3).Padding(10).Text(
                        "Este documento es de uso interno mientras se completa el trámite de facturación electrónica (CFDI) ante el SAT. " +
                        "No sustituye una factura fiscal ni tiene validez para efectos de deducción de impuestos.")
                        .FontSize(8).Italic();
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }
}
