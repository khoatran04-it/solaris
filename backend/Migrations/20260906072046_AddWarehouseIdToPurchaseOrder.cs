using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseIdToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBatches_ProductVariants_ProductVariantId",
                table: "ProductBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductBatches_Suppliers_SupplierId1",
                table: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductBatches_ProductVariantId",
                table: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductBatches_SupplierId1",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "SupplierId1",
                table: "ProductBatches");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "ProductBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId1",
                table: "ProductBatches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductVariantId",
                table: "ProductBatches",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_SupplierId1",
                table: "ProductBatches",
                column: "SupplierId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBatches_ProductVariants_ProductVariantId",
                table: "ProductBatches",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBatches_Suppliers_SupplierId1",
                table: "ProductBatches",
                column: "SupplierId1",
                principalTable: "Suppliers",
                principalColumn: "SupplierId");
        }
    }
}
