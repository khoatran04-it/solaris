using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToCustomerTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CustomerTiers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTiers_Code",
                table: "CustomerTiers",
                column: "Code",
                unique: true);

            migrationBuilder.Sql("UPDATE CustomerTiers SET IsActive = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerTiers_Code",
                table: "CustomerTiers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CustomerTiers");
        }
    }
}
