using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class IARolePermissionConfiguration : IEntityTypeConfiguration<IARolePermission>
    {
        public void Configure(EntityTypeBuilder<IARolePermission> builder)
        {
            builder.ToTable("IARolePermissions");

            // 🔥 Khóa chính kết hợp: 1 Role chỉ map với 1 Permission 1 lần
            builder.HasKey(x => new { x.RoleId, x.PermissionId });

            // Khóa ngoại nối với Role
            builder.HasOne(x => x.Role)
                   .WithMany(r => r.RolePermissions)
                   .HasForeignKey(x => x.RoleId)
                   .OnDelete(DeleteBehavior.Cascade); // Cực kỳ an toàn: Xóa Role thì tự động xóa sạch các dòng phân quyền của Role đó

            // Khóa ngoại nối với Permission
            builder.HasOne(x => x.Permission)
                   .WithMany(p => p.RolePermissions)
                   .HasForeignKey(x => x.PermissionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}