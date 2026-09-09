using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryTripIdToCustomerReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryTripId",
                table: "CustomerReturns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_DeliveryTripId",
                table: "CustomerReturns",
                column: "DeliveryTripId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerReturns_DeliveryTrips_DeliveryTripId",
                table: "CustomerReturns",
                column: "DeliveryTripId",
                principalTable: "DeliveryTrips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerReturns_DeliveryTrips_DeliveryTripId",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_DeliveryTripId",
                table: "CustomerReturns");

            migrationBuilder.DropColumn(
                name: "DeliveryTripId",
                table: "CustomerReturns");
        }
    }
}
