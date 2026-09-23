using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransferApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "InventoryTransfers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "InventoryTransfers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "InventoryTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ApprovedById",
                table: "InventoryTransfers",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_IAUsers_ApprovedById",
                table: "InventoryTransfers",
                column: "ApprovedById",
                principalTable: "IAUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_IAUsers_ApprovedById",
                table: "InventoryTransfers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransfers_ApprovedById",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "InventoryTransfers");
        }
    }
}
