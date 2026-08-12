using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddActsAndActAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    external_sysmond_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    surname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    act_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    vkn_tckn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    tax_office_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    act_full_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    country_id = table.Column<int>(type: "int", nullable: true),
                    city_id = table.Column<int>(type: "int", nullable: true),
                    city_other = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    scenario = table.Column<int>(type: "int", nullable: false),
                    is_disabled = table.Column<bool>(type: "bit", nullable: false),
                    is_locked = table.Column<bool>(type: "bit", nullable: false),
                    is_abroad_customer = table.Column<bool>(type: "bit", nullable: false),
                    parent_act_external_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    synced_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acts", x => x.id);
                    table.ForeignKey(
                        name: "FK_acts_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "act_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    act_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    external_sysmond_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    country_id = table.Column<int>(type: "int", nullable: false),
                    city_id = table.Column<int>(type: "int", nullable: true),
                    city_other = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    district_id = table.Column<int>(type: "int", nullable: true),
                    district_other = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    street = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    building_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    building_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    room = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    floor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    postal_zone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    country_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    city_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    district_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_disabled = table.Column<bool>(type: "bit", nullable: false),
                    contact_first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    contact_last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    contact_email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    contact_main_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    contact_main_cell_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    synced_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_act_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_act_addresses_acts_act_id",
                        column: x => x.act_id,
                        principalTable: "acts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_act_addresses_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_act_addresses_act_id",
                table: "act_addresses",
                column: "act_id");

            migrationBuilder.CreateIndex(
                name: "IX_act_addresses_company_id",
                table: "act_addresses",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_act_addresses_external_sysmond_id",
                table: "act_addresses",
                column: "external_sysmond_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acts_company_id_type",
                table: "acts",
                columns: new[] { "company_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_acts_external_sysmond_id",
                table: "acts",
                column: "external_sysmond_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "act_addresses");

            migrationBuilder.DropTable(
                name: "acts");
        }
    }
}
