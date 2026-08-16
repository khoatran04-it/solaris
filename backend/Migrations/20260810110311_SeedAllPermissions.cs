using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SeedAllPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "IAPermissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[,]
                {
                    { 1, "ROLE_VIEW", "Hệ thống", "Xem danh sách Vai trò" },
                    { 2, "ROLE_MANAGE", "Hệ thống", "Thêm/Sửa/Xóa Vai trò & Phân quyền" },
                    { 3, "USER_VIEW", "Hệ thống", "Xem danh sách Nhân viên" },
                    { 4, "USER_MANAGE", "Hệ thống", "Thêm/Sửa/Xóa Nhân viên" },
                    { 5, "SUPPLIER_VIEW", "Nhà cung cấp", "Xem danh sách Nhà cung cấp" },
                    { 6, "SUPPLIER_MANAGE", "Nhà cung cấp", "Thêm/Sửa/Xóa Nhà cung cấp" },
                    { 7, "SUPPLIER_CONFIG", "Nhà cung cấp", "Cấu hình Phân loại Nhà cung cấp" },
                    { 8, "CUSTOMER_VIEW", "Khách hàng", "Xem danh sách Khách hàng" },
                    { 9, "CUSTOMER_MANAGE", "Khách hàng", "Thêm/Sửa/Xóa Khách hàng" },
                    { 10, "CUSTOMER_CONFIG", "Khách hàng", "Cấu hình Khách hàng (Loại, Cấp bậc, Nhóm)" },
                    { 11, "PRODUCT_VIEW", "Sản phẩm", "Xem danh sách Sản phẩm & Biến thể" },
                    { 12, "PRODUCT_MANAGE", "Sản phẩm", "Thêm/Sửa/Xóa Sản phẩm & Biến thể" },
                    { 13, "CATEGORY_MANAGE", "Sản phẩm", "Quản lý Danh mục & Nhóm danh mục" },
                    { 14, "ATTRIBUTE_MANAGE", "Thuộc tính", "Quản lý Từ điển & Gán Thuộc tính" },
                    { 15, "UOM_VIEW", "Đơn vị tính", "Xem Đơn vị tính & Tỷ lệ quy đổi" },
                    { 16, "UOM_MANAGE", "Đơn vị tính", "Quản lý Đơn vị tính, Phân loại & Quy đổi" },
                    { 17, "PROMOTION_VIEW", "Khuyến mãi", "Xem Chiến dịch Khuyến mãi" },
                    { 18, "PROMOTION_MANAGE", "Khuyến mãi", "Quản lý Chiến dịch Khuyến mãi" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "IAPermissions",
                keyColumn: "Id",
                keyValue: 18);
        }
    }
}
