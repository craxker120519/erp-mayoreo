using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaCreditoAplicaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotaCreditoAplicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotaCreditoId = table.Column<int>(type: "integer", nullable: false),
                    PedidoVentaDestinoId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaCreditoAplicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaCreditoAplicaciones_NotasCredito_NotaCreditoId",
                        column: x => x.NotaCreditoId,
                        principalTable: "NotasCredito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaCreditoAplicaciones_PedidosVenta_PedidoVentaDestinoId",
                        column: x => x.PedidoVentaDestinoId,
                        principalTable: "PedidosVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaCreditoAplicaciones_NotaCreditoId",
                table: "NotaCreditoAplicaciones",
                column: "NotaCreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCreditoAplicaciones_PedidoVentaDestinoId",
                table: "NotaCreditoAplicaciones",
                column: "PedidoVentaDestinoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotaCreditoAplicaciones");
        }
    }
}
