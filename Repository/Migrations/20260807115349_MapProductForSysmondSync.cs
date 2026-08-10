using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class MapProductForSysmondSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "products",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                table: "products",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_id",
                table: "products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "measure_unit_id",
                table: "products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "purchase_currency_id",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sale_currency_id",
                table: "products",
                type: "int",
                nullable: true);

            // Mevcut satırlar için Sysmond StockTypes.Goods = 10
            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateIndex(
                name: "IX_products_external_sysmond_id",
                table: "products",
                column: "external_sysmond_id",
                unique: true,
                filter: "[external_sysmond_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_external_sysmond_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "external_sysmond_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "measure_unit_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "purchase_currency_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sale_currency_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "type",
                table: "products");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                table: "products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
