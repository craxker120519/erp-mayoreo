using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

public class TurnoCajaPdfService(IConfiguration configuration) : ITurnoCajaPdfService
{
    public byte[] Generar(TurnoCaja turno)
    {
        var (nombreEmpresa, rfcEmpresa) = PdfCommon.Empresa(configuration);
        var porMetodo = turno.Ventas
            .GroupBy(v => v.MetodoPago)
            .Select(g => (Metodo: g.Key, Cantidad: g.Count(), Total: g.Sum(v => v.Total)))
            .OrderBy(g => g.Metodo)
            .ToList();

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Green.Darken2).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text("CORTE DE CAJA").FontColor(Colors.White).Bold().FontSize(13);
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(nombreEmpresa).Bold().FontSize(13);
                            c.Item().Text($"RFC: {rfcEmpresa}");
                        });
                        row.ConstantItem(200).Column(c =>
                        {
                            c.Item().AlignRight().Text($"Cajero: {turno.CajeroEmpleado.Nombre}").Bold();
                            if (turno.Almacen is not null)
                            {
                                c.Item().AlignRight().Text($"Almacén: {turno.Almacen.Nombre}");
                            }
                            c.Item().AlignRight().Text($"Apertura: {turno.AbiertoUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                            if (turno.CerradoUtc is not null)
                            {
                                c.Item().AlignRight().Text($"Cierre: {turno.CerradoUtc.Value.ToLocalTime():dd/MM/yyyy HH:mm}");
                            }
                        });
                    });
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Método de pago").Bold();
                            header.Cell().AlignRight().Text("Ventas").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                            header.Cell().ColumnSpan(3).PaddingTop(3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var g in porMetodo)
                        {
                            table.Cell().PaddingVertical(3).Text(g.Metodo.ToString());
                            table.Cell().PaddingVertical(3).AlignRight().Text(g.Cantidad.ToString());
                            table.Cell().PaddingVertical(3).AlignRight().Text(g.Total.ToString("C"));
                        }
                    });

                    col.Item().PaddingTop(10).Text($"Total de ventas del turno: {turno.Ventas.Sum(v => v.Total):C} en {turno.Ventas.Count} venta(s)");

                    col.Item().PaddingTop(20).AlignRight().Width(260).Column(totales =>
                    {
                        totales.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Monto inicial en caja");
                            row.ConstantItem(110).AlignRight().Text(turno.MontoInicial.ToString("C"));
                        });
                        totales.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Ventas en efectivo");
                            row.ConstantItem(110).AlignRight().Text(
                                turno.Ventas.Where(v => v.MetodoPago == MetodoPago.Efectivo).Sum(v => v.Total).ToString("C"));
                        });
                        totales.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Darken1).Row(row =>
                        {
                            row.RelativeItem().Text("Efectivo esperado").Bold();
                            row.ConstantItem(110).AlignRight().Text((turno.MontoEsperado ?? 0).ToString("C")).Bold();
                        });
                        totales.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Efectivo contado");
                            row.ConstantItem(110).AlignRight().Text((turno.MontoContado ?? 0).ToString("C"));
                        });
                        totales.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Darken1).Row(row =>
                        {
                            var diferencia = turno.Diferencia ?? 0;
                            row.RelativeItem().Text("Diferencia").Bold();
                            row.ConstantItem(110).AlignRight()
                                .Text(diferencia.ToString("C")).Bold()
                                .FontColor(diferencia == 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });

                    if (turno.Ventas.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("Detalle de ventas").Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Folio").Bold();
                                header.Cell().Text("Hora").Bold();
                                header.Cell().Text("Método").Bold();
                                header.Cell().AlignRight().Text("Total").Bold();
                                header.Cell().ColumnSpan(4).PaddingTop(3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                            });

                            foreach (var v in turno.Ventas.OrderBy(v => v.FechaUtc))
                            {
                                table.Cell().PaddingVertical(2).Text(v.Folio);
                                table.Cell().PaddingVertical(2).Text(v.FechaUtc.ToLocalTime().ToString("HH:mm"));
                                table.Cell().PaddingVertical(2).Text(v.MetodoPago.ToString());
                                table.Cell().PaddingVertical(2).AlignRight().Text(v.Total.ToString("C"));
                            }
                        });
                    }
                });

                PdfCommon.PieDePagina(page);
            });
        });

        return documento.GeneratePdf();
    }
}
