using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToCustomerTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CustomerTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTypes_Code",
                table: "CustomerTypes",
                column: "Code",
                unique: true);

            migrationBuilder.Sql("UPDATE CustomerTypes SET IsActive = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerTypes_Code",
                table: "CustomerTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CustomerTypes");
        }
    }
}
