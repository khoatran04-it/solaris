using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class PurchaseOrderDetailConfiguration : IEntityTypeConfiguration<PurchaseOrderDetail>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderDetail> builder)
        {
            builder.ToTable("PurchaseOrderDetails");
            builder.HasKey(x => x.Id);

            // Số lượng: 3 thập phân cho nông sản đo theo kg (VD: 12.500 kg)
            builder.Property(x => x.OrderQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.ReceivedQuantity).HasColumnType("decimal(18,3)").HasDefaultValue(0m);
            builder.Property(x => x.RejectedQuantity).HasColumnType("decimal(18,3)").HasDefaultValue(0m);

            // Tiền: 2 thập phân chuẩn kế toán
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

            // --- CONFIGURATION KHÓA NGOẠI ---
            // Xóa PO thì xóa luôn chi tiết (Cascade) - Bảng con chết theo bảng cha
            builder.HasOne(x => x.PurchaseOrder)
                   .WithMany(po => po.Details)
                   .HasForeignKey(x => x.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa sản phẩm nếu đã có trong PO

            builder.HasOne(x => x.UoM)
                   .WithMany()
                   .HasForeignKey(x => x.UoMId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa đơn vị tính nếu đã dùng trong PO
        }
    }
}
