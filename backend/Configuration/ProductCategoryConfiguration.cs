using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
    {
        public void Configure(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.ToTable("ProductCategories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.Description).HasMaxLength(500).HasColumnType("nvarchar(500)");
            builder.Property(x => x.ImagePath).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CẤU HÌNH KHÓA NGOẠI ---
            // Quy tắc: Xóa Nhóm Lớn (Group) thì KHÔNG được tự động xóa Danh mục con (Category)
            builder.HasOne(x => x.CategoryGroup)
                   .WithMany(g => g.Categories)
                   .HasForeignKey(x => x.CategoryGroupId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}