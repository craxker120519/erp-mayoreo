using ERP.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services;

// Prefijo + año (2 dígitos) + consecutivo D4, ej. "PV26-0001".
// Cotización (COT), Pedido (PV) y Remisión (REM) son documentos SEPARADOS con folio propio --
// cada uno su propia fila en PedidosVenta, encadenados por DocumentoOrigenId (ver
// PedidosVentaController.ConvertirAPedido/Remisionar) -- no un solo folio cambiando de estatus.
public class FolioService(AppDbContext db) : IFolioService
{
    public Task<string> GenerarFolioCotizacionAsync() =>
        SiguienteFolioAsync("COT", db.PedidosVenta.Select(p => p.Folio));

    public Task<string> GenerarFolioPedidoVentaAsync() =>
        SiguienteFolioAsync("PV", db.PedidosVenta.Select(p => p.Folio));

    public Task<string> GenerarFolioRemisionAsync() =>
        SiguienteFolioAsync("REM", db.PedidosVenta.Select(p => p.Folio));

    public Task<string> GenerarFolioPedidoCompraAsync() =>
        SiguienteFolioAsync("OC", db.PedidosCompra.Select(p => p.Folio));

    public Task<string> GenerarFolioVentaMostradorAsync() =>
        SiguienteFolioAsync("VM", db.VentasMostrador.Select(v => v.Folio));

    public Task<string> GenerarFolioFacturaAsync() =>
        SiguienteFolioAsync("FAC", db.Facturas.Select(f => f.Folio));

    public Task<string> GenerarFolioNotaCreditoAsync() =>
        SiguienteFolioAsync("NC", db.NotasCredito.Select(n => n.Folio));

    public Task<string> GenerarFolioTraspasoAsync() =>
        SiguienteFolioAsync("TR", db.TraspasosAlmacen.Select(t => t.Folio));

    private static async Task<string> SiguienteFolioAsync(string prefijoBase, IQueryable<string> foliosExistentes)
    {
        var prefijo = $"{prefijoBase}{DateTime.UtcNow:yy}-";
        var folios = await foliosExistentes.Where(f => f.StartsWith(prefijo)).ToListAsync();

        var maximo = 0;
        foreach (var folio in folios)
        {
            var sufijo = folio[prefijo.Length..];
            if (int.TryParse(sufijo, out var numero) && numero > maximo)
            {
                maximo = numero;
            }
        }

        return $"{prefijo}{maximo + 1:D4}";
    }
}
