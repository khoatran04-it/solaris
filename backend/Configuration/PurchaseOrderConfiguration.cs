using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.ToTable("PurchaseOrders");
            builder.HasKey(x => x.Id);

            // Mã đơn hàng: Unique, dùng varchar cho tốc độ Index
            builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.OrderCode).IsUnique();

            builder.Property(x => x.OrderDate).HasColumnType("datetime2");
            builder.Property(x => x.ExpectedDeliveryDate).HasColumnType("datetime2");

            // Trạng thái (Lưu dưới dạng số nguyên của Enum)
            builder.Property(x => x.Status).IsRequired();

            // Tổng tiền: Chuẩn kế toán 18 chữ số, 2 thập phân
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);

            // --- AUDIT & SOFT DELETE ---
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---
            builder.HasOne(x => x.Supplier)
                   .WithMany(s => s.PurchaseOrders)
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa NCC nếu đã có đơn hàng

            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa User nếu người đó từng lập PO

            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa Kho nếu đang có PO trỏ tới
        }
    }
}
