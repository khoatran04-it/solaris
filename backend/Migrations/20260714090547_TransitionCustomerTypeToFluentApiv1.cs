using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class TransitionCustomerTypeToFluentApiv1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UoMConversions_Id",
                table: "UoMConversions");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CustomerTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomerTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomerTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomerTypes");

            migrationBuilder.CreateIndex(
                name: "IX_UoMConversions_Id",
                table: "UoMConversions",
                column: "Id",
                unique: true);
        }
    }
}
