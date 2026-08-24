using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API cho bảng trung gian Phân quyền theo vai trò (IARolePermission).
    /// </summary>
    public class IARolePermissionConfiguration : IEntityTypeConfiguration<IARolePermission>
    {
        public void Configure(EntityTypeBuilder<IARolePermission> builder)
        {
            builder.ToTable("IARolePermissions");

            // Khóa chính phức hợp: Đảm bảo mỗi cặp Role - Permission là duy nhất
            builder.HasKey(x => new { x.RoleId, x.PermissionId });

            #region Cấu hình Khóa ngoại & Quan hệ (Foreign Keys)
            // Quan hệ với bảng Vai trò (IARole)
            builder.HasOne(x => x.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ với bảng Quyền hạn (IAPermission)
            builder.HasOne(x => x.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}