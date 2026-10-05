using ClosedXML.Excel;
using ERP.Api.Controllers;
using ERP.Models;

namespace ERP.Services;

// Una PESTAÑA de Excel por almacén (cuando se piden varios a la vez) -- misma separación que el
// PDF y la pantalla, pero en formato de hoja de cálculo real para quien prefiera trabajarlo ahí.
public class KardexExcelService : IKardexExcelService
{
    public byte[] Generar(Producto producto, List<KardexMovimiento> movimientos)
    {
        using var workbook = new XLWorkbook();
        var grupos = movimientos.GroupBy(m => m.Almacen).OrderBy(g => g.Key).ToList();

        if (grupos.Count == 0)
        {
            EscribirHoja(workbook, "Kardex", producto, []);
        }
        else
        {
            foreach (var grupo in grupos)
            {
                EscribirHoja(workbook, NombreHojaValido(grupo.Key), producto, grupo.ToList());
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void EscribirHoja(XLWorkbook workbook, string nombreHoja, Producto producto, List<KardexMovimiento> movimientos)
    {
        var ws = workbook.Worksheets.Add(nombreHoja);

        ws.Cell(1, 1).Value = "Producto";
        ws.Cell(1, 2).Value = producto.Nombre;
        ws.Cell(2, 1).Value = "Código";
        ws.Cell(2, 2).Value = producto.Codigo ?? "";
        ws.Range(1, 1, 2, 1).Style.Font.Bold = true;

        const int filaEncabezado = 4;
        string[] encabezados = ["Fecha", "Almacén", "Tipo", "Cantidad", "Saldo", "Referencia", "Nota"];
        for (var col = 0; col < encabezados.Length; col++)
        {
            var celda = ws.Cell(filaEncabezado, col + 1);
            celda.Value = encabezados[col];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF6");
        }

        var fila = filaEncabezado + 1;
        foreach (var m in movimientos)
        {
            ws.Cell(fila, 1).Value = m.FechaUtc.ToLocalTime();
            ws.Cell(fila, 1).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
            ws.Cell(fila, 2).Value = m.Almacen;
            ws.Cell(fila, 3).Value = m.Tipo == TipoMovimiento.Entrada ? "Entrada" : "Salida";
            ws.Cell(fila, 4).Value = m.Tipo == TipoMovimiento.Entrada ? (double)m.Cantidad : -(double)m.Cantidad;
            ws.Cell(fila, 5).Value = (double)m.Saldo;
            ws.Cell(fila, 5).Style.Font.Bold = true;
            ws.Cell(fila, 6).Value = m.Referencia ?? "";
            ws.Cell(fila, 7).Value = m.Nota ?? "";
            fila++;
        }

        if (movimientos.Count == 0)
        {
            ws.Cell(fila, 1).Value = "Sin movimientos registrados.";
        }

        ws.Columns().AdjustToContents();
    }

    private static string NombreHojaValido(string nombreAlmacen)
    {
        var limpio = new string(nombreAlmacen.Where(c => !"[]*/\\?:".Contains(c)).ToArray());
        return limpio.Length > 31 ? limpio[..31] : (limpio.Length == 0 ? "Almacén" : limpio);
    }
}
