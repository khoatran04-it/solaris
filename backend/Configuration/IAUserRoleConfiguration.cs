using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API cho bảng trung gian Gán vai trò người dùng (IAUserRole).
    /// </summary>
    public class IAUserRoleConfiguration : IEntityTypeConfiguration<IAUserRole>
    {
        public void Configure(EntityTypeBuilder<IAUserRole> builder)
        {
            builder.ToTable("IAUserRoles");

            // Khóa chính phức hợp (Composite Key): Đảm bảo mỗi cặp User - Role là duy nhất
            builder.HasKey(x => new { x.UserId, x.RoleId });

            builder.Property(x => x.AssignedAt)
                .HasColumnType("datetime2");

            #region Cấu hình Khóa ngoại & Quan hệ (Foreign Keys)
            // Quan hệ với bảng Người dùng (IAUser)
            builder.HasOne(x => x.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ với bảng Vai trò (IARole)
            builder.HasOne(x => x.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}