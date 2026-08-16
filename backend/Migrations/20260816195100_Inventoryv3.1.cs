using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Inventoryv31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "IAPermissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[] { 22, "INVENTORY_MANAGE", "Kho tổng", "Quản lý Tồn Kho" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 22);
        }
    }
}
