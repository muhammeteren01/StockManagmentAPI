using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository.Data;

#nullable disable

namespace Repository.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260810133000_MapStockTransactionForSysmondDespatchSync")]
    public partial class MapStockTransactionForSysmondDespatchSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_id",
                table: "stock_transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_despatch_id",
                table: "stock_transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_external_sysmond_id",
                table: "stock_transactions",
                column: "external_sysmond_id",
                unique: true,
                filter: "[external_sysmond_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_external_sysmond_despatch_id",
                table: "stock_transactions",
                column: "external_sysmond_despatch_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_external_sysmond_despatch_id",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_external_sysmond_id",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "external_sysmond_despatch_id",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "external_sysmond_id",
                table: "stock_transactions");
        }
    }
}
