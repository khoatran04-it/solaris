using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class SupplierProductConfiguration : IEntityTypeConfiguration<SupplierProduct>
    {
        public void Configure(EntityTypeBuilder<SupplierProduct> builder)
        {
            builder.ToTable("SupplierProducts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SupplierSKU).HasMaxLength(100).HasColumnType("varchar(100)");

            // Ép kiểu chuẩn Tiền tệ Kế toán
            builder.Property(x => x.LastImportPrice).HasColumnType("decimal(18,2)");

            // MOQ: Dùng decimal cho tương thích Base UoM (VD: 0.5 tấn)
            builder.Property(x => x.MinimumOrderQuantity).HasColumnType("decimal(18,3)").HasDefaultValue(1m);
            builder.Property(x => x.LeadTimeDays).HasDefaultValue(0);

            // 🔥 COMPOSITE INDEX: Đảm bảo 1 Nhà cung cấp chỉ có 1 mức giá/cấu hình cho 1 Sản phẩm tại một thời điểm
            builder.HasIndex(x => new { x.VariantId, x.SupplierId }).IsUnique();

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---
            builder.HasOne(x => x.Variant)
                   .WithMany(v => v.SupplierProducts)
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Supplier)
                   .WithMany(s => s.SupplierProducts)
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Đơn vị tính mua hàng (VD: NCC bán theo Thùng, Base UoM hệ thống là Kg)
            builder.HasOne(x => x.PurchaseUoM)
                   .WithMany()
                   .HasForeignKey(x => x.PurchaseUoMId)
                   .OnDelete(DeleteBehavior.Restrict); // Cấm xóa UoM nếu NCC đang dùng
        }
    }
}