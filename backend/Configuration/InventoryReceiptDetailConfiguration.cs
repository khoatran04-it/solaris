using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryReceiptDetailConfiguration : IEntityTypeConfiguration<InventoryReceiptDetail>
    {
        public void Configure(EntityTypeBuilder<InventoryReceiptDetail> builder)
        {
            builder.ToTable("InventoryReceiptDetails");
            builder.HasKey(x => x.Id);

            // Số lượng: 3 thập phân cho nông sản đo theo kg
            builder.Property(x => x.ExpectedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.AcceptedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.RejectedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.RejectReason).HasMaxLength(500);

            // Đo lường thực tế & Thể tích
            builder.Property(x => x.ActualWeightKg).HasColumnType("decimal(18,3)");
            builder.Property(x => x.CalculatedCbm).HasColumnType("decimal(18,4)");

            // --- CONFIGURATION KHÓA NGOẠI ---
            // Xóa Phiếu Nhập thì xóa luôn chi tiết (Cascade)
            builder.HasOne(x => x.InventoryReceipt)
                   .WithMany(ir => ir.Details)
                   .HasForeignKey(x => x.InventoryReceiptId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Batch)
                   .WithMany()
                   .HasForeignKey(x => x.BatchId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa Lô nếu đã có phiếu nhập

            builder.HasOne(x => x.UoM)
                   .WithMany()
                   .HasForeignKey(x => x.UoMId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Liên kết ngược về dòng PO gốc (Nullable)
            // Chìa khóa vàng: 1 IR chứa hàng từ nhiều PO thông qua FK ở detail level
            builder.HasOne(x => x.PurchaseOrderDetail)
                   .WithMany(pod => pod.ReceiptDetails)
                   .HasForeignKey(x => x.PurchaseOrderDetailId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa PO nếu đã có phiếu nhập trỏ tới
        }
    }
}
