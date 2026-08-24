using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API cho bảng trung gian Quyền ngoại lệ theo người dùng (IAUserPermission).
    /// </summary>
    public class IAUserPermissionConfiguration : IEntityTypeConfiguration<IAUserPermission>
    {
        public void Configure(EntityTypeBuilder<IAUserPermission> builder)
        {
            builder.ToTable("IAUserPermissions");

            // Khóa chính phức hợp: Mỗi người dùng chỉ thiết lập tối đa 1 ngoại lệ cho từng quyền cụ thể
            builder.HasKey(x => new { x.UserId, x.PermissionId });

            // Trạng thái ngoại lệ: true (Cấp thêm / Allow), false (Chặn / Deny)
            builder.Property(x => x.IsGranted)
                .IsRequired();

            #region Cấu hình Khóa ngoại & Quan hệ (Foreign Keys)
            // Quan hệ với bảng Người dùng (IAUser)
            builder.HasOne(x => x.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ với bảng Quyền hạn (IAPermission)
            builder.HasOne(x => x.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}