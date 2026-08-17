using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Save21 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    AuditType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AuditorId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalSystemQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalActualQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalVarianceQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalVarianceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryAudits_IAUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "IAUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAudits_IAUsers_AuditorId",
                        column: x => x.AuditorId,
                        principalTable: "IAUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAudits_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdjustmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    AuditId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalVarianceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_IAUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "IAUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_IAUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "IAUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_InventoryAudits_AuditId",
                        column: x => x.AuditId,
                        principalTable: "InventoryAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAuditDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    UoMId = table.Column<int>(type: "int", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VarianceQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VarianceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReasonNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAuditDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryAuditDetails_InventoryAudits_AuditId",
                        column: x => x.AuditId,
                        principalTable: "InventoryAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryAuditDetails_ProductBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditDetails_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditDetails_UoMs_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UoMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAdjustmentDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdjustmentId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    UoMId = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReasonDetail = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAdjustmentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustmentDetails_InventoryAdjustments_AdjustmentId",
                        column: x => x.AdjustmentId,
                        principalTable: "InventoryAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustmentDetails_ProductBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustmentDetails_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustmentDetails_UoMs_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UoMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustmentDetails_AdjustmentId",
                table: "InventoryAdjustmentDetails",
                column: "AdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustmentDetails_BatchId",
                table: "InventoryAdjustmentDetails",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustmentDetails_UoMId",
                table: "InventoryAdjustmentDetails",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustmentDetails_VariantId",
                table: "InventoryAdjustmentDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_AdjustmentCode",
                table: "InventoryAdjustments",
                column: "AdjustmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_ApprovedById",
                table: "InventoryAdjustments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_AuditId",
                table: "InventoryAdjustments",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_CreatedById",
                table: "InventoryAdjustments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_WarehouseId",
                table: "InventoryAdjustments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditDetails_AuditId",
                table: "InventoryAuditDetails",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditDetails_BatchId",
                table: "InventoryAuditDetails",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditDetails_UoMId",
                table: "InventoryAuditDetails",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditDetails_VariantId",
                table: "InventoryAuditDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAudits_ApprovedById",
                table: "InventoryAudits",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAudits_AuditCode",
                table: "InventoryAudits",
                column: "AuditCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAudits_AuditorId",
                table: "InventoryAudits",
                column: "AuditorId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAudits_WarehouseId",
                table: "InventoryAudits",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryAdjustmentDetails");

            migrationBuilder.DropTable(
                name: "InventoryAuditDetails");

            migrationBuilder.DropTable(
                name: "InventoryAdjustments");

            migrationBuilder.DropTable(
                name: "InventoryAudits");
        }
    }
}
