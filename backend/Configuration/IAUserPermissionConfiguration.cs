using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class IAUserPermissionConfiguration : IEntityTypeConfiguration<IAUserPermission>
    {
        public void Configure(EntityTypeBuilder<IAUserPermission> builder)
        {
            builder.ToTable("IAUserPermissions");

            // 🔥 Khóa chính kết hợp: 1 User chỉ được set ngoại lệ cho 1 Permission 1 lần
            builder.HasKey(x => new { x.UserId, x.PermissionId });

            // Bắt buộc phải có Cờ hiệu (Tặng quyền hay Tước quyền)
            builder.Property(x => x.IsGranted).IsRequired();

            // Khóa ngoại nối với User
            builder.HasOne(x => x.User)
                   .WithMany(u => u.UserPermissions)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa nhân viên thì xóa sạch các ngoại lệ quyền của nhân viên đó

            // Khóa ngoại nối với Permission
            builder.HasOne(x => x.Permission)
                   .WithMany(p => p.UserPermissions)
                   .HasForeignKey(x => x.PermissionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}