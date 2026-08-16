using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class IAUserConfiguration : IEntityTypeConfiguration<IAUser>
    {
        public void Configure(EntityTypeBuilder<IAUser> builder)
        {
            builder.ToTable("IAUsers");
            builder.HasKey(x => x.Id);

            // --- CÁC TRƯỜNG ĐỊNH DANH (Chống trùng lặp) ---
            builder.Property(x => x.CitizenId).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");
            builder.HasIndex(x => x.CitizenId).IsUnique(); // 1 CCCD chỉ tạo 1 tài khoản

            builder.Property(x => x.Username).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.Username).IsUnique();

            builder.Property(x => x.Email).IsRequired().HasMaxLength(150).HasColumnType("varchar(150)");
            builder.HasIndex(x => x.Email).IsUnique();

            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");
            builder.HasIndex(x => x.PhoneNumber).IsUnique();

            // --- CÁC TRƯỜNG THÔNG TIN KHÁC ---
            builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(x => x.FullName).IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            builder.Property(x => x.AvatarUrl).HasMaxLength(500);

            builder.Property(x => x.LastLoginAt).HasColumnType("datetime2");

            // --- AUDIT & SOFT DELETE ---
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}