using ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<PedidoVenta> PedidosVenta => Set<PedidoVenta>();
    public DbSet<PedidoVentaItem> PedidoVentaItems => Set<PedidoVentaItem>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<PedidoCompraItem> PedidoCompraItems => Set<PedidoCompraItem>();
    public DbSet<PagoProveedor> PagosProveedor => Set<PagoProveedor>();
    public DbSet<TurnoCaja> TurnosCaja => Set<TurnoCaja>();
    public DbSet<VentaMostrador> VentasMostrador => Set<VentaMostrador>();
    public DbSet<VentaMostradorItem> VentaMostradorItems => Set<VentaMostradorItem>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<NotaCredito> NotasCredito => Set<NotaCredito>();
    public DbSet<NotaCreditoItem> NotaCreditoItems => Set<NotaCreditoItem>();
    public DbSet<NotaCreditoAplicacion> NotaCreditoAplicaciones => Set<NotaCreditoAplicacion>();
    public DbSet<TraspasoAlmacen> TraspasosAlmacen => Set<TraspasoAlmacen>();
    public DbSet<TraspasoAlmacenItem> TraspasoAlmacenItems => Set<TraspasoAlmacenItem>();
    public DbSet<BitacoraEvento> BitacoraEventos => Set<BitacoraEvento>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CandadoAcceso> CandadosAcceso => Set<CandadoAcceso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        modelBuilder.Entity<Empleado>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
            e.Property(x => x.Rfc).HasMaxLength(13);
        });

        modelBuilder.Entity<Proveedor>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
            e.Property(x => x.Rfc).HasMaxLength(13);
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
            e.Property(x => x.Codigo).HasMaxLength(50);
            e.HasIndex(x => x.Codigo);
        });

        modelBuilder.Entity<Almacen>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Existencia>(e =>
        {
            e.HasIndex(x => new { x.ProductoId, x.AlmacenId }).IsUnique();
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoVenta>(e =>
        {
            e.Property(x => x.Folio).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Folio).IsUnique();
            e.HasOne(x => x.Cliente).WithMany(c => c.Pedidos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DocumentoOrigen).WithMany(x => x.DocumentosGenerados).HasForeignKey(x => x.DocumentoOrigenId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.PedidoVenta).HasForeignKey(i => i.PedidoVentaId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Pagos).WithOne(p => p.PedidoVenta).HasForeignKey(p => p.PedidoVentaId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Facturas).WithOne(f => f.PedidoVenta).HasForeignKey(f => f.PedidoVentaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.NotasCredito).WithOne(n => n.PedidoVenta).HasForeignKey(n => n.PedidoVentaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.CreditoRecibido).WithOne(a => a.PedidoVentaDestino).HasForeignKey(a => a.PedidoVentaDestinoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Factura>(e =>
        {
            e.Property(x => x.Folio).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Folio).IsUnique();
        });

        modelBuilder.Entity<NotaCredito>(e =>
        {
            e.Property(x => x.Folio).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Folio).IsUnique();
            e.HasMany(x => x.Items).WithOne(i => i.NotaCredito).HasForeignKey(i => i.NotaCreditoId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Aplicaciones).WithOne(a => a.NotaCredito).HasForeignKey(a => a.NotaCreditoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotaCreditoItem>(e =>
        {
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TraspasoAlmacen>(e =>
        {
            e.Property(x => x.Folio).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Folio).IsUnique();
            e.HasOne(x => x.AlmacenOrigen).WithMany().HasForeignKey(x => x.AlmacenOrigenId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AlmacenDestino).WithMany().HasForeignKey(x => x.AlmacenDestinoId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.TraspasoAlmacen).HasForeignKey(i => i.TraspasoAlmacenId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TraspasoAlmacenItem>(e =>
        {
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoVentaItem>(e =>
        {
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoCompra>(e =>
        {
            e.Property(x => x.Folio).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Folio).IsUnique();
            e.HasOne(x => x.Proveedor).WithMany(p => p.PedidosCompra).HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.PedidoCompra).HasForeignKey(i => i.PedidoCompraId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Pagos).WithOne(p => p.PedidoCompra).HasForeignKey(p => p.PedidoCompraId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PedidoCompraItem>(e =>
        {
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TurnoCaja>(e =>
        {
            e.HasOne(x => x.CajeroEmpleado).WithMany().HasForeignKey(x => x.CajeroEmpleadoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VentaMostrador>(e =>
        {
            e.Property(x => x.Folio).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Folio).IsUnique();
            e.HasOne(x => x.TurnoCaja).WithMany(t => t.Ventas).HasForeignKey(x => x.TurnoCajaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.VentaMostrador).HasForeignKey(i => i.VentaMostradorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VentaMostradorItem>(e =>
        {
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BitacoraEvento>(e =>
        {
            e.Property(x => x.Usuario).IsRequired().HasMaxLength(100);
            e.Property(x => x.Accion).IsRequired().HasMaxLength(100);
            e.Property(x => x.Entidad).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.FechaUtc);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(x => x.Username).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Roles).HasColumnType("text[]");
        });

        modelBuilder.Entity<CandadoAcceso>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
        });
    }
}
