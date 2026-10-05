using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddTraspasosYNotasCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotasCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Folio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PedidoVentaId = table.Column<int>(type: "integer", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    DevuelveInventario = table.Column<bool>(type: "boolean", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TasaIva = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Iva = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Estatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasCredito_PedidosVenta_PedidoVentaId",
                        column: x => x.PedidoVentaId,
                        principalTable: "PedidosVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TraspasosAlmacen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Folio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AlmacenOrigenId = table.Column<int>(type: "integer", nullable: false),
                    AlmacenDestinoId = table.Column<int>(type: "integer", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraspasosAlmacen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraspasosAlmacen_Almacenes_AlmacenDestinoId",
                        column: x => x.AlmacenDestinoId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraspasosAlmacen_Almacenes_AlmacenOrigenId",
                        column: x => x.AlmacenOrigenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotaCreditoItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotaCreditoId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaCreditoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaCreditoItems_NotasCredito_NotaCreditoId",
                        column: x => x.NotaCreditoId,
                        principalTable: "NotasCredito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaCreditoItems_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TraspasoAlmacenItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraspasoAlmacenId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraspasoAlmacenItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraspasoAlmacenItems_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraspasoAlmacenItems_TraspasosAlmacen_TraspasoAlmacenId",
                        column: x => x.TraspasoAlmacenId,
                        principalTable: "TraspasosAlmacen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaCreditoItems_NotaCreditoId",
                table: "NotaCreditoItems",
                column: "NotaCreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCreditoItems_ProductoId",
                table: "NotaCreditoItems",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_Folio",
                table: "NotasCredito",
                column: "Folio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_PedidoVentaId",
                table: "NotasCredito",
                column: "PedidoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_TraspasoAlmacenItems_ProductoId",
                table: "TraspasoAlmacenItems",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_TraspasoAlmacenItems_TraspasoAlmacenId",
                table: "TraspasoAlmacenItems",
                column: "TraspasoAlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_TraspasosAlmacen_AlmacenDestinoId",
                table: "TraspasosAlmacen",
                column: "AlmacenDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_TraspasosAlmacen_AlmacenOrigenId",
                table: "TraspasosAlmacen",
                column: "AlmacenOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_TraspasosAlmacen_Folio",
                table: "TraspasosAlmacen",
                column: "Folio",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotaCreditoItems");

            migrationBuilder.DropTable(
                name: "TraspasoAlmacenItems");

            migrationBuilder.DropTable(
                name: "NotasCredito");

            migrationBuilder.DropTable(
                name: "TraspasosAlmacen");
        }
    }
}
