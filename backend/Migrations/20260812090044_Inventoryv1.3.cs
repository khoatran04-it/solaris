using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Inventoryv13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 19,
                column: "Code",
                value: "WAREHOUSE_VIEW");

            migrationBuilder.UpdateData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 20,
                column: "Code",
                value: "WAREHOUSE_MANAGE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 19,
                column: "Code",
                value: "INVENTORY_VIEW");

            migrationBuilder.UpdateData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 20,
                column: "Code",
                value: "INVENTORY_MANAGE");
        }
    }
}
