using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehousePhysicalCapacityAndDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "MaxPalletPositions",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxWeightCapacityKg",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAreaSqm",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCapacityCbm",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarningThresholdPercent",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 85);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShoppingCarts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShoppingCartItems",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossWeightKg",
                table: "ProductVariants",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCbm",
                table: "ProductVariants",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWeightKg",
                table: "InventoryReceiptDetails",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalculatedCbm",
                table: "InventoryReceiptDetails",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCbm",
                table: "InventoryIssueDetails",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeightKg",
                table: "InventoryIssueDetails",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CustomerReturns",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPalletPositions",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxWeightCapacityKg",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TotalAreaSqm",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TotalCapacityCbm",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WarningThresholdPercent",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "GrossWeightKg",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "UnitCbm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ActualWeightKg",
                table: "InventoryReceiptDetails");

            migrationBuilder.DropColumn(
                name: "CalculatedCbm",
                table: "InventoryReceiptDetails");

            migrationBuilder.DropColumn(
                name: "TotalCbm",
                table: "InventoryIssueDetails");

            migrationBuilder.DropColumn(
                name: "TotalWeightKg",
                table: "InventoryIssueDetails");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShoppingCarts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShoppingCartItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CustomerReturns",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
