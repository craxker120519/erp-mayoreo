using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

public class PedidoVentaPdfService(IConfiguration configuration) : IPedidoVentaPdfService
{
    public byte[] Generar(PedidoVenta pedido)
    {
        var (nombreEmpresa, rfcEmpresa) = PdfCommon.Empresa(configuration);
        var titulo = pedido.Estatus switch
        {
            EstatusPedidoVenta.Cotizacion => "COTIZACIÓN",
            EstatusPedidoVenta.Pedido => "PEDIDO",
            EstatusPedidoVenta.Remisionado => "NOTA DE REMISIÓN",
            EstatusPedidoVenta.Cancelado => "PEDIDO CANCELADO",
            _ => "PEDIDO DE VENTA"
        };

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Blue.Darken2).Padding(8).Row(row =>
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
                            c.Item().AlignRight().Text($"Folio: {pedido.Folio}").Bold();
                            c.Item().AlignRight().Text($"Fecha: {pedido.FechaUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                            if (pedido.Almacen is not null)
                            {
                                c.Item().AlignRight().Text($"Almacén: {pedido.Almacen.Nombre}");
                            }
                        });
                    });
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Text("Cliente").Bold();
                    col.Item().Text(pedido.Cliente.Nombre);
                    col.Item().Text($"RFC: {pedido.Cliente.Rfc ?? "No registrado"}");
                    if (!string.IsNullOrWhiteSpace(pedido.Cliente.Direccion))
                    {
                        col.Item().Text(pedido.Cliente.Direccion!);
                    }
                    if (!string.IsNullOrWhiteSpace(pedido.Nota))
                    {
                        col.Item().PaddingTop(5).Text($"Nota: {pedido.Nota}").Italic();
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

                        foreach (var item in pedido.Items)
                        {
                            table.Cell().PaddingVertical(3).Text(item.Producto.Nombre);
                            table.Cell().PaddingVertical(3).AlignRight().Text($"{item.Cantidad} {item.Producto.UnidadVenta}");
                            table.Cell().PaddingVertical(3).AlignRight().Text(item.PrecioUnitario.ToString("C"));
                            table.Cell().PaddingVertical(3).AlignRight().Text(item.Importe.ToString("C"));
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Width(220).Row(row =>
                    {
                        row.RelativeItem().Text("Total").Bold();
                        row.ConstantItem(100).AlignRight().Text(pedido.Total.ToString("C")).Bold();
                    });
                });

                PdfCommon.PieDePagina(page);
            });
        });

        return documento.GeneratePdf();
    }
}
