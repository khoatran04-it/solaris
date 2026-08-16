using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryReceiptConfiguration : IEntityTypeConfiguration<InventoryReceipt>
    {
        public void Configure(EntityTypeBuilder<InventoryReceipt> builder)
        {
            builder.ToTable("InventoryReceipts");
            builder.HasKey(x => x.Id);

            // Mã phiếu nhập: Unique, varchar cho tốc độ Index
            builder.Property(x => x.ReceiptCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.ReceiptCode).IsUnique();

            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.ReceiptDate).HasColumnType("datetime2");

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);

            // --- AUDIT & SOFT DELETE ---
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---
            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa kho nếu đã có phiếu nhập

            // SupplierId nullable: Hàng tặng/Hàng mẫu không cần NCC
            builder.HasOne(x => x.Supplier)
                   .WithMany()
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReceivedBy)
                   .WithMany()
                   .HasForeignKey(x => x.ReceivedById)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
