using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_Payment_Shipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GhnDistrictId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnWardCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionNo",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingProvider",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GhnDistrictId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GhnWardCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionNo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingProvider",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "Orders");
        }
    }
}
