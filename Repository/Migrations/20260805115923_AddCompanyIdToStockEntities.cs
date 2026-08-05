using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToStockEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "stock_transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "stock_transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "inventory",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE i
                SET company_id = p.company_id
                FROM inventory i
                INNER JOIN products p ON p.id = i.product_id;

                UPDATE st
                SET company_id = p.company_id
                FROM stock_transactions st
                INNER JOIN products p ON p.id = st.product_id;

                UPDATE t
                SET company_id = w.company_id
                FROM stock_transfers t
                INNER JOIN warehouses w ON w.id = t.from_warehouse_id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "stock_transfers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "stock_transactions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "inventory",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_company_id",
                table: "stock_transfers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_company_id",
                table: "stock_transactions",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_company_id",
                table: "inventory",
                column: "company_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_companies_company_id",
                table: "inventory",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_transactions_companies_company_id",
                table: "stock_transactions",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_transfers_companies_company_id",
                table: "stock_transfers",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_companies_company_id",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_transactions_companies_company_id",
                table: "stock_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_transfers_companies_company_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_company_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_company_id",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_inventory_company_id",
                table: "inventory");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "inventory");
        }
    }
}
