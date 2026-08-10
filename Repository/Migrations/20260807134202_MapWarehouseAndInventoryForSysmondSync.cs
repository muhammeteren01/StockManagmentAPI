using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class MapWarehouseAndInventoryForSysmondSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_id",
                table: "warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_sysmond_id",
                table: "inventory",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_external_sysmond_id",
                table: "warehouses",
                column: "external_sysmond_id",
                unique: true,
                filter: "[external_sysmond_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_sysmond_id",
                table: "inventory",
                column: "external_sysmond_id",
                unique: true,
                filter: "[external_sysmond_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_warehouses_external_sysmond_id",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_inventory_external_sysmond_id",
                table: "inventory");

            migrationBuilder.DropColumn(
                name: "external_sysmond_id",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "external_sysmond_id",
                table: "inventory");
        }
    }
}
