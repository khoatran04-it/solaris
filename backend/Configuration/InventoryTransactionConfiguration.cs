using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TransactionCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.TransactionCode).IsUnique();

            // Kiểu giao dịch (Lưu dưới dạng số nguyên của Enum)
            builder.Property(x => x.Type).IsRequired();

            // Số lượng biến động
            builder.Property(x => x.Quantity).HasColumnType("decimal(18, 3)");

            builder.Property(x => x.ReferenceCode).HasMaxLength(100).HasColumnType("varchar(100)");
            // Đánh Index cột này để truy vết ngược từ Mã Đơn Hàng / Phiếu Nhập siêu nhanh
            builder.HasIndex(x => x.ReferenceCode);

            builder.Property(x => x.Note).HasMaxLength(500);

            // --- QUAN HỆ KHÓA NGOẠI ---
            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa Kho nếu đã có giao dịch

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa Sản phẩm nếu đã có giao dịch

            builder.HasOne(x => x.Batch)
                   .WithMany()
                   .HasForeignKey(x => x.BatchId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa Lô nếu đã có giao dịch

            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict); // Không cho xóa User nếu người đó từng duyệt phiếu
        }
    }
}