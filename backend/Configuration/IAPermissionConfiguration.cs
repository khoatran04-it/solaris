using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class IAPermissionConfiguration : IEntityTypeConfiguration<IAPermission>
    {
        public void Configure(EntityTypeBuilder<IAPermission> builder)
        {
            builder.ToTable("IAPermissions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Module).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
            builder.Property(x => x.Code).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");

            // 🔥 SEEDING TOÀN BỘ QUYỀN DỰA TRÊN MODULE CỦA HỆ THỐNG
            builder.HasData(
                // 1. NHÓM HỆ THỐNG (SYSTEM)
                new IAPermission { Id = 1, Module = "Hệ thống", Code = "ROLE_VIEW", Name = "Xem danh sách Vai trò" },
                new IAPermission { Id = 2, Module = "Hệ thống", Code = "ROLE_MANAGE", Name = "Thêm/Sửa/Xóa Vai trò & Phân quyền" },
                new IAPermission { Id = 3, Module = "Hệ thống", Code = "USER_VIEW", Name = "Xem danh sách Nhân viên" },
                new IAPermission { Id = 4, Module = "Hệ thống", Code = "USER_MANAGE", Name = "Thêm/Sửa/Xóa Nhân viên" },

                // 2. NHÓM ĐỐI TÁC: NHÀ CUNG CẤP (SUPPLIER)
                new IAPermission { Id = 5, Module = "Nhà cung cấp", Code = "SUPPLIER_VIEW", Name = "Xem danh sách Nhà cung cấp" },
                new IAPermission { Id = 6, Module = "Nhà cung cấp", Code = "SUPPLIER_MANAGE", Name = "Thêm/Sửa/Xóa Nhà cung cấp" },
                new IAPermission { Id = 7, Module = "Nhà cung cấp", Code = "SUPPLIER_CONFIG", Name = "Cấu hình Phân loại Nhà cung cấp" },

                // 3. NHÓM ĐỐI TÁC: KHÁCH HÀNG (CUSTOMER)
                new IAPermission { Id = 8, Module = "Khách hàng", Code = "CUSTOMER_VIEW", Name = "Xem danh sách Khách hàng" },
                new IAPermission { Id = 9, Module = "Khách hàng", Code = "CUSTOMER_MANAGE", Name = "Thêm/Sửa/Xóa Khách hàng" },
                new IAPermission { Id = 10, Module = "Khách hàng", Code = "CUSTOMER_CONFIG", Name = "Cấu hình Khách hàng (Loại, Cấp bậc, Nhóm)" },

                // 4. NHÓM SẢN PHẨM (PRODUCT & CATEGORY)
                new IAPermission { Id = 11, Module = "Sản phẩm", Code = "PRODUCT_VIEW", Name = "Xem danh sách Sản phẩm & Biến thể" },
                new IAPermission { Id = 12, Module = "Sản phẩm", Code = "PRODUCT_MANAGE", Name = "Thêm/Sửa/Xóa Sản phẩm & Biến thể" },
                new IAPermission { Id = 13, Module = "Sản phẩm", Code = "CATEGORY_MANAGE", Name = "Quản lý Danh mục & Nhóm danh mục" },

                // 5. NHÓM THUỘC TÍNH (ATTRIBUTE)
                new IAPermission { Id = 14, Module = "Thuộc tính", Code = "ATTRIBUTE_MANAGE", Name = "Quản lý Từ điển & Gán Thuộc tính" },

                // 6. NHÓM ĐƠN VỊ TÍNH (UOM)
                new IAPermission { Id = 15, Module = "Đơn vị tính", Code = "UOM_VIEW", Name = "Xem Đơn vị tính & Tỷ lệ quy đổi" },
                new IAPermission { Id = 16, Module = "Đơn vị tính", Code = "UOM_MANAGE", Name = "Quản lý Đơn vị tính, Phân loại & Quy đổi" },

                // 7. NHÓM KHUYẾN MÃI (PROMOTION)
                new IAPermission { Id = 17, Module = "Khuyến mãi", Code = "PROMOTION_VIEW", Name = "Xem Chiến dịch Khuyến mãi" },
                new IAPermission { Id = 18, Module = "Khuyến mãi", Code = "PROMOTION_MANAGE", Name = "Quản lý Chiến dịch Khuyến mãi" },

               // 8. KHO HÀNG (WAREHOUSE)
               new IAPermission { Id = 19, Module = "Kho hàng", Code = "WAREHOUSE_VIEW", Name = "Xem Kho hàng" },
               new IAPermission { Id = 20, Module = "Kho hàng", Code = "WAREHOUSE_MANAGE", Name = "Quản lý Kho hàng" },

               // 9. KHO TỔNG (INVENTORY)
               new IAPermission { Id = 21, Module = "Kho tổng", Code = "INVENTORY_VIEW", Name = "Xem Kho tổng" },
               new IAPermission { Id = 22, Module = "Kho tổng", Code = "INVENTORY_MANAGE", Name = "Quản lý Tồn Kho" }
            );
        }
    }
}