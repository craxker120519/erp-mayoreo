using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class DocumentosVentaSeparados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertidoAPedidoUtc",
                table: "PedidosVenta");

            migrationBuilder.DropColumn(
                name: "RemisionadoUtc",
                table: "PedidosVenta");

            migrationBuilder.AddColumn<int>(
                name: "DocumentoOrigenId",
                table: "PedidosVenta",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_DocumentoOrigenId",
                table: "PedidosVenta",
                column: "DocumentoOrigenId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenta_PedidosVenta_DocumentoOrigenId",
                table: "PedidosVenta",
                column: "DocumentoOrigenId",
                principalTable: "PedidosVenta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenta_PedidosVenta_DocumentoOrigenId",
                table: "PedidosVenta");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenta_DocumentoOrigenId",
                table: "PedidosVenta");

            migrationBuilder.DropColumn(
                name: "DocumentoOrigenId",
                table: "PedidosVenta");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertidoAPedidoUtc",
                table: "PedidosVenta",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemisionadoUtc",
                table: "PedidosVenta",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
