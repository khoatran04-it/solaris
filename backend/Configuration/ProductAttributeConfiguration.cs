using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductAttribute> builder)
        {
            builder.ToTable("ProductAttributes");
            builder.HasKey(x => x.Id);

            // Giá trị thuộc tính (Ví dụ: "VietGAP", "Đỏ", "15 Brix")
            builder.Property(x => x.AttributeValue).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");

            builder.Property(x => x.DisplayOrder).HasDefaultValue(0);

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---
            // 1. Nối với Biến thể (ProductVariant)
            builder.HasOne(x => x.Variant)
                   .WithMany(v => v.Attributes)
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 2. Nối với Từ điển thuộc tính (AttributeDefinition)
            // Lưu ý: Đảm bảo sếp sẽ tạo model AttributeDefinition sau nhé
            builder.HasOne(x => x.AttributeDefinition)
                   .WithMany()
                   .HasForeignKey(x => x.AttributeDefinitionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}