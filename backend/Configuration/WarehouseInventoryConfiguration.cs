using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class WarehouseInventoryConfiguration : IEntityTypeConfiguration<WarehouseInventory>
    {
        public void Configure(EntityTypeBuilder<WarehouseInventory> builder)
        {
            builder.ToTable("WarehouseInventories");
            builder.HasKey(x => x.Id);

            // 🔥 COMPOSITE UNIQUE INDEX: Đảm bảo 1 Kho + 1 Biến Thể + 1 Lô chỉ có DUY NHẤT 1 dòng số dư
            // Tăng tốc độ truy xuất cực nhanh khi check tồn kho
            builder.HasIndex(x => new { x.WarehouseId, x.VariantId, x.BatchId }).IsUnique();

            // Cấu hình kiểu Decimal (18 chữ số tổng cộng, 3 chữ số phần thập phân)
            builder.Property(x => x.QuantityAvailable).HasColumnType("decimal(18, 3)");
            builder.Property(x => x.QuantityReserved).HasColumnType("decimal(18, 3)");
            builder.Property(x => x.QuantityQC).HasColumnType("decimal(18, 3)");
            builder.Property(x => x.QuantityDamaged).HasColumnType("decimal(18, 3)");

            // Các khóa ngoại chặn việc xóa bừa bãi (Restrict)
            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Batch)
                   .WithMany()
                   .HasForeignKey(x => x.BatchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}