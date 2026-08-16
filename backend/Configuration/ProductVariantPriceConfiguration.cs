using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class ProductVariantPriceConfiguration : IEntityTypeConfiguration<ProductVariantPrice>
    {
        public void Configure(EntityTypeBuilder<ProductVariantPrice> builder)
        {
            // Tên bảng trong SQL Server
            builder.ToTable("ProductVariantPrices");
            builder.HasKey(x => x.Id);

            // Cấu hình kiểu tiền tệ chuẩn (18 chữ số, 2 số thập phân)
            builder.Property(x => x.Price).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(x => x.IsDefault).HasDefaultValue(false);

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CONFIGURATION KHÓA NGOẠI ---

            // 1. Nối với ProductVariant (Biến thể)
            builder.HasOne(x => x.Variant)
                   .WithMany(v => v.Prices) // Khớp với ICollection<ProductVariantPrice> Prices ở Model Variant
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Cascade); // CỰC KỲ QUAN TRỌNG: Xóa/Ẩn Biến thể thì ẩn luôn bảng giá của nó

            // 2. Nối với UoM (Đơn vị tính)
            builder.HasOne(x => x.UoM)
                   .WithMany() // Không cần khai báo ICollection bên UoM để tránh vòng lặp tham chiếu rườm rà
                   .HasForeignKey(x => x.UoMId)
                   .OnDelete(DeleteBehavior.Restrict); // CỰC KỲ QUAN TRỌNG: Không cho phép xóa Đơn vị tính (VD: "Kg") nếu đang có bảng giá sử dụng nó. Chống mồ côi dữ liệu!
        }
    }
}