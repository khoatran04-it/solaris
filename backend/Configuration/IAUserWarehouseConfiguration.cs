using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API cho bảng trung gian Phân quyền dữ liệu theo kho (IAUserWarehouse).
    /// </summary>
    public class IAUserWarehouseConfiguration : IEntityTypeConfiguration<IAUserWarehouse>
    {
        public void Configure(EntityTypeBuilder<IAUserWarehouse> builder)
        {
            builder.ToTable("IAUserWarehouses");

            // Khóa chính phức hợp: Mỗi người dùng chỉ được gán quyền cho một kho cụ thể một lần
            builder.HasKey(x => new { x.UserId, x.WarehouseId });

            builder.Property(x => x.AssignedAt)
                .HasColumnType("datetime2");

            #region Cấu hình Khóa ngoại & Quan hệ (Foreign Keys)
            // Quan hệ với bảng Người dùng (IAUser)
            builder.HasOne(x => x.User)
                .WithMany(u => u.UserWarehouses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ với bảng Kho hàng (Warehouse)
            builder.HasOne(x => x.Warehouse)
                .WithMany(w => w.UserWarehouses)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}