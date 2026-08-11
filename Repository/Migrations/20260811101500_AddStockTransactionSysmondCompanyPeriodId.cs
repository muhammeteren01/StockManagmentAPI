using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository.Data;

#nullable disable

namespace Repository.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260811101500_AddStockTransactionSysmondCompanyPeriodId")]
    public partial class AddStockTransactionSysmondCompanyPeriodId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_company_period_id",
                table: "stock_transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_external_sysmond_company_period_id",
                table: "stock_transactions",
                column: "external_sysmond_company_period_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_external_sysmond_company_period_id",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "external_sysmond_company_period_id",
                table: "stock_transactions");
        }
    }
}
