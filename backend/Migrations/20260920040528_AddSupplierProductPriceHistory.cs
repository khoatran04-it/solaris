using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierProductPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierProductPriceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierProductId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    PurchaseUoMId = table.Column<int>(type: "int", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceChange = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProductPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_IAUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "IAUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_SupplierProducts_SupplierProductId",
                        column: x => x.SupplierProductId,
                        principalTable: "SupplierProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_UoMs_PurchaseUoMId",
                        column: x => x.PurchaseUoMId,
                        principalTable: "UoMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_CreatedById",
                table: "SupplierProductPriceHistories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_EffectiveDate",
                table: "SupplierProductPriceHistories",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_PurchaseUoMId",
                table: "SupplierProductPriceHistories",
                column: "PurchaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_SupplierId",
                table: "SupplierProductPriceHistories",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_SupplierProductId",
                table: "SupplierProductPriceHistories",
                column: "SupplierProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_VariantId",
                table: "SupplierProductPriceHistories",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierProductPriceHistories");
        }
    }
}
