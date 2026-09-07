using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddColdChainAndTransportationFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxColdChainRadiusKm",
                table: "Warehouses",
                type: "float",
                nullable: false,
                defaultValue: 15.0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresColdChain",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingProvider",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTripId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "Orders",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "Orders",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTripId",
                table: "InventoryTransfers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "InventoryTransfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "InventoryTransfers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InspectedById",
                table: "InventoryTransfers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InspectedDate",
                table: "InventoryTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "InventoryTransfers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualReceivedQuantity",
                table: "InventoryTransferDetails",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DamagedQuantity",
                table: "InventoryTransferDetails",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DeliveryVehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    LicensePlate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    VehicleType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IsColdChainEquipped = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DriverPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    HomeWarehouseId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryVehicles_Warehouses_HomeWarehouseId",
                        column: x => x.HomeWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryTrips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TripType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    LicensePlate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InventoryTransferId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryTrips_DeliveryVehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "DeliveryVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryTrips_InventoryTransfers_InventoryTransferId",
                        column: x => x.InventoryTransferId,
                        principalTable: "InventoryTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryTrips_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryTripOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DeliverySequence = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryTripOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryTripOrders_DeliveryTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "DeliveryTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryTripOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryTripId",
                table: "Orders",
                column: "DeliveryTripId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_DeliveryTripId",
                table: "InventoryTransfers",
                column: "DeliveryTripId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_InspectedById",
                table: "InventoryTransfers",
                column: "InspectedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTripOrders_OrderId",
                table: "DeliveryTripOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTripOrders_TripId_OrderId",
                table: "DeliveryTripOrders",
                columns: new[] { "TripId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTrips_InventoryTransferId",
                table: "DeliveryTrips",
                column: "InventoryTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTrips_TripCode",
                table: "DeliveryTrips",
                column: "TripCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTrips_VehicleId",
                table: "DeliveryTrips",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTrips_WarehouseId",
                table: "DeliveryTrips",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryVehicles_Code",
                table: "DeliveryVehicles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryVehicles_HomeWarehouseId",
                table: "DeliveryVehicles",
                column: "HomeWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryVehicles_LicensePlate",
                table: "DeliveryVehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_DeliveryTrips_DeliveryTripId",
                table: "InventoryTransfers",
                column: "DeliveryTripId",
                principalTable: "DeliveryTrips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_IAUsers_InspectedById",
                table: "InventoryTransfers",
                column: "InspectedById",
                principalTable: "IAUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryTrips_DeliveryTripId",
                table: "Orders",
                column: "DeliveryTripId",
                principalTable: "DeliveryTrips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_DeliveryTrips_DeliveryTripId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_IAUsers_InspectedById",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryTrips_DeliveryTripId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DeliveryTripOrders");

            migrationBuilder.DropTable(
                name: "DeliveryTrips");

            migrationBuilder.DropTable(
                name: "DeliveryVehicles");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryTripId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransfers_DeliveryTripId",
                table: "InventoryTransfers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransfers_InspectedById",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "MaxColdChainRadiusKm",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "RequiresColdChain",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTripId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DispatchedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTripId",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "InspectedById",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "InspectedDate",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "InventoryTransfers");

            migrationBuilder.DropColumn(
                name: "ActualReceivedQuantity",
                table: "InventoryTransferDetails");

            migrationBuilder.DropColumn(
                name: "DamagedQuantity",
                table: "InventoryTransferDetails");

            migrationBuilder.AlterColumn<string>(
                name: "ShippingProvider",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
