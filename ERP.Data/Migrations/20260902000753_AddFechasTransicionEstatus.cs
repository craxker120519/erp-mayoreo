using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddFechasTransicionEstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<DateTime>(
                name: "RecibidoUtc",
                table: "PedidosCompra",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertidoAPedidoUtc",
                table: "PedidosVenta");

            migrationBuilder.DropColumn(
                name: "RemisionadoUtc",
                table: "PedidosVenta");

            migrationBuilder.DropColumn(
                name: "RecibidoUtc",
                table: "PedidosCompra");
        }
    }
}
