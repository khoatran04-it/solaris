using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class IAUserRoleConfiguration : IEntityTypeConfiguration<IAUserRole>
    {
        public void Configure(EntityTypeBuilder<IAUserRole> builder)
        {
            builder.ToTable("IAUserRoles");

            // 🔥 Khóa chính kết hợp (Composite Key): 1 User chỉ nhận 1 Role 1 lần
            builder.HasKey(x => new { x.UserId, x.RoleId });

            builder.Property(x => x.AssignedAt).HasColumnType("datetime2");

            // --- CONFIGURATION KHÓA NGOẠI ---
            // 1. Nối với User
            builder.HasOne(x => x.User)
                   .WithMany(u => u.UserRoles)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa/Ẩn User thì xóa quyền của User đó

            // 2. Nối với Role
            builder.HasOne(x => x.Role)
                   .WithMany(r => r.UserRoles)
                   .HasForeignKey(x => x.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}