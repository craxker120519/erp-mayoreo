using ERP.Api.Controllers;
using ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Services;

// Un bloque/tabla POR ALMACÉN cuando se piden varios a la vez -- cada uno con su propio saldo
// corriente (ya vienen así calculados desde ReportesController.CalcularKardexAsync), para que la
// separación por almacén se vea igual de clara en el PDF que en pantalla.
public class KardexPdfService(IConfiguration configuration) : IKardexPdfService
{
    public byte[] Generar(Producto producto, List<KardexMovimiento> movimientos)
    {
        var (nombreEmpresa, rfcEmpresa) = PdfCommon.Empresa(configuration);
        var grupos = movimientos.GroupBy(m => m.Almacen).OrderBy(g => g.Key).ToList();

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Indigo.Darken2).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text("KARDEX").FontColor(Colors.White).Bold().FontSize(13);
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(nombreEmpresa).Bold().FontSize(12);
                            c.Item().Text($"RFC: {rfcEmpresa}").FontSize(8);
                        });
                        row.ConstantItem(220).Column(c =>
                        {
                            c.Item().AlignRight().Text(producto.Nombre).Bold();
                            if (!string.IsNullOrWhiteSpace(producto.Codigo))
                            {
                                c.Item().AlignRight().Text($"Código: {producto.Codigo}").FontSize(8);
                            }
                            c.Item().AlignRight().Text($"Almacén(es): {string.Join(", ", grupos.Select(g => g.Key))}").FontSize(8);
                        });
                    });
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    foreach (var grupo in grupos)
                    {
                        col.Item().PaddingTop(10).Text(grupo.Key).Bold().FontSize(11);
                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2f);
                                columns.RelativeColumn(2.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Fecha").Bold();
                                header.Cell().Text("Tipo").Bold();
                                header.Cell().AlignRight().PaddingRight(8).Text("Cantidad").Bold();
                                header.Cell().AlignRight().PaddingRight(8).Text("Saldo").Bold();
                                header.Cell().Text("Documento").Bold();
                                header.Cell().Text("Motivo").Bold();
                                header.Cell().ColumnSpan(6).PaddingTop(2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                            });

                            foreach (var m in grupo)
                            {
                                var signo = m.Tipo == TipoMovimiento.Entrada ? "+" : "-";
                                table.Cell().PaddingVertical(2).Text(m.FechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                                table.Cell().PaddingVertical(2).Text(m.Tipo == TipoMovimiento.Entrada ? "Entrada" : "Salida");
                                table.Cell().PaddingVertical(2).AlignRight().PaddingRight(8).Text($"{signo}{m.Cantidad}");
                                table.Cell().PaddingVertical(2).AlignRight().PaddingRight(8).Text(m.Saldo.ToString()).Bold();
                                table.Cell().PaddingVertical(2).Text(m.Referencia ?? string.Empty);
                                table.Cell().PaddingVertical(2).Text(m.Nota ?? string.Empty).FontSize(8);
                            }
                        });

                        var saldoFinal = grupo.Last().Saldo;
                        col.Item().PaddingTop(3).AlignRight().Text($"Saldo final en {grupo.Key}: {saldoFinal} {producto.UnidadVenta}").Bold();
                    }

                    if (grupos.Count == 0)
                    {
                        col.Item().PaddingTop(20).Text("Este producto no tiene movimientos registrados en el rango seleccionado.").Italic();
                    }
                });

                PdfCommon.PieDePagina(page);
            });
        });

        return documento.GeneratePdf();
    }
}
