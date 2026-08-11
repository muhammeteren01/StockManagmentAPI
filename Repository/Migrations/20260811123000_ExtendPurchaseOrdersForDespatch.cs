using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository.Data;

#nullable disable

namespace Repository.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260811123000_ExtendPurchaseOrdersForDespatch")]
    public partial class ExtendPurchaseOrdersForDespatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "warehouse_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "document_type",
                table: "purchase_orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PurchaseOrder");

            migrationBuilder.AddColumn<string>(
                name: "direction",
                table: "purchase_orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_company_period_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_company_address_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_address_json",
                table: "purchase_orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "act_name",
                table: "purchase_orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "act_vkn_tckn",
                table: "purchase_orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "issue_date",
                table: "purchase_orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "actual_despatch_date",
                table: "purchase_orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "warehouse_id",
                table: "purchase_order_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_id",
                table: "purchase_order_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "measure_unit_id",
                table: "purchase_order_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "stock_price_id",
                table: "purchase_order_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "purchase_order_items",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "purchase_order_items",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_percent",
                table: "purchase_order_items",
                type: "decimal(9,4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_document_type",
                table: "purchase_orders",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_external_sysmond_id",
                table: "purchase_orders",
                column: "external_sysmond_id",
                unique: true,
                filter: "[external_sysmond_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_external_sysmond_company_period_id",
                table: "purchase_orders",
                column: "external_sysmond_company_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_external_sysmond_id",
                table: "purchase_order_items",
                column: "external_sysmond_id",
                unique: true,
                filter: "[external_sysmond_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_warehouse_id",
                table: "purchase_order_items",
                column: "warehouse_id");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_order_items_warehouses_warehouse_id",
                table: "purchase_order_items",
                column: "warehouse_id",
                principalTable: "warehouses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_order_items_warehouses_warehouse_id",
                table: "purchase_order_items");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_items_warehouse_id",
                table: "purchase_order_items");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_items_external_sysmond_id",
                table: "purchase_order_items");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_external_sysmond_company_period_id",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_external_sysmond_id",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_document_type",
                table: "purchase_orders");

            migrationBuilder.DropColumn(name: "vat_percent", table: "purchase_order_items");
            migrationBuilder.DropColumn(name: "code", table: "purchase_order_items");
            migrationBuilder.DropColumn(name: "name", table: "purchase_order_items");
            migrationBuilder.DropColumn(name: "stock_price_id", table: "purchase_order_items");
            migrationBuilder.DropColumn(name: "measure_unit_id", table: "purchase_order_items");
            migrationBuilder.DropColumn(name: "external_sysmond_id", table: "purchase_order_items");
            migrationBuilder.DropColumn(name: "warehouse_id", table: "purchase_order_items");

            migrationBuilder.DropColumn(name: "actual_despatch_date", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "issue_date", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "act_vkn_tckn", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "act_name", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "delivery_address_json", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "external_sysmond_company_address_id", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "external_sysmond_company_period_id", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "external_sysmond_id", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "direction", table: "purchase_orders");
            migrationBuilder.DropColumn(name: "document_type", table: "purchase_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "warehouse_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
