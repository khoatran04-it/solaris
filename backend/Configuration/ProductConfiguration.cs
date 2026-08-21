using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(x => x.Id);

            // Ràng buộc Code (Mã sản phẩm gốc): Bắt buộc, tối đa 50 ký tự, không được trùng
            builder.Property(x => x.Code).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.Code).IsUnique();

            // Ràng buộc Name & Description
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.Slug).HasMaxLength(250).HasColumnType("nvarchar(250)");
            builder.HasIndex(x => x.Slug);
            builder.Property(x => x.Description).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            builder.Property(x => x.ImagePath).HasMaxLength(1000);

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---
            // 1. Nối với Danh mục (ProductCategory)
            builder.HasOne(x => x.Category)
                   .WithMany(c => c.Products)
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 2. Nối với Đơn vị tính gốc (UoM)
            builder.HasOne(x => x.BaseUoM)
                   .WithMany() // UoM không cần chứa list Products để tránh vòng lặp
                   .HasForeignKey(x => x.BaseUoMId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}