using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.ToTable("ProductVariants");
            builder.HasKey(x => x.Id);

            // SKU Code không được trùng lặp trên toàn hệ thống
            builder.Property(x => x.Code).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.Description).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            builder.Property(x => x.ImagePath).HasMaxLength(500);

            // 🔥 ĐÃ GỠ BỎ: builder.Property(x => x.DefaultPrice)...

            builder.Property(x => x.InventoryGuideline).HasDefaultValue(0);

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---
            // Nối với Product gốc
            builder.HasOne(x => x.Product)
                   .WithMany(p => p.Variants)
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Product gốc thì ẩn luôn các biến thể
        }
    }
}