using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
    {
        public void Configure(EntityTypeBuilder<ProductBatch> builder)
        {
            builder.ToTable("ProductBatches");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BatchCode).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");

            // Mã lô phải là duy nhất trên toàn hệ thống
            builder.HasIndex(x => x.BatchCode).IsUnique();

            // Quan hệ với ProductVariant (1 Biến thể có nhiều Lô)
            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Biến thể nếu đang có Lô hàng

            // Quan hệ với Supplier (1 NCC cung cấp nhiều Lô)
            builder.HasOne(x => x.Supplier)
                   .WithMany()
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict); // Xóa NCC thì ID này về Null, lô hàng vẫn giữ nguyên
        }
    }
}