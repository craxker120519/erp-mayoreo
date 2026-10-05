using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddAlmacenSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "TurnosCaja",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "PedidosVenta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "PedidosCompra",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_AlmacenId",
                table: "TurnosCaja",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_AlmacenId",
                table: "PedidosVenta",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_AlmacenId",
                table: "PedidosCompra",
                column: "AlmacenId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosCompra_Almacenes_AlmacenId",
                table: "PedidosCompra",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenta_Almacenes_AlmacenId",
                table: "PedidosVenta",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TurnosCaja_Almacenes_AlmacenId",
                table: "TurnosCaja",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosCompra_Almacenes_AlmacenId",
                table: "PedidosCompra");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenta_Almacenes_AlmacenId",
                table: "PedidosVenta");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnosCaja_Almacenes_AlmacenId",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_TurnosCaja_AlmacenId",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenta_AlmacenId",
                table: "PedidosVenta");

            migrationBuilder.DropIndex(
                name: "IX_PedidosCompra_AlmacenId",
                table: "PedidosCompra");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "TurnosCaja");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "PedidosVenta");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "PedidosCompra");
        }
    }
}
