using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API và ràng buộc quan hệ cho bảng Quy tắc quy đổi Đơn vị tính (UoMConversion).
    /// </summary>
    public class UoMConversionConfiguration : IEntityTypeConfiguration<UoMConversion>
    {
        public void Configure(EntityTypeBuilder<UoMConversion> builder)
        {
            builder.ToTable("UoMConversions");

            builder.HasKey(x => x.Id);

            #region Bỏ qua thuộc tính tính toán (Ignored Properties)
            // Bỏ qua IsStandard vì là thuộc tính logic tính toán trong bộ nhớ (Computed Property)
            builder.Ignore(x => x.IsStandard);
            #endregion

            #region Cấu hình Thuộc tính (Properties)
            // Hệ số quy đổi hỗ trợ độ chính xác cao đến 6 chữ số thập phân (Ví dụ: 1 Gram = 0.001000 Kg)
            builder.Property(x => x.ConversionFactor)
                .IsRequired()
                .HasColumnType("decimal(18,6)");

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(x => x.DeletedAt)
                .HasColumnType("datetime2");
            #endregion

            #region Chỉ mục & Bộ lọc Toàn cục (Indexes & Query Filters)
            // Đảm bảo không tạo trùng lặp quy tắc quy đổi giữa cùng một cặp FromUoM - ToUoM cho cùng 1 Product (hoặc cùng chuẩn hệ thống)
            builder.HasIndex(x => new { x.ProductId, x.FromUoMId, x.ToUoMId })
                .IsUnique();

            // Bộ lọc toàn cục: Tự động loại trừ các quy tắc quy đổi đã bị xóa mềm
            builder.HasQueryFilter(x => !x.IsDeleted);
            #endregion

            #region Khóa ngoại & Mối quan hệ (Foreign Keys)
            // 1. Quan hệ với Sản phẩm (Product): Nếu xóa Sản phẩm thì xóa luôn các quy đổi riêng của nó
            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Quan hệ với ĐVT Nguồn (FromUoM): Cấm xóa ĐVT nếu đang được dùng làm mốc quy đổi
            builder.HasOne(x => x.FromUoM)
                .WithMany()
                .HasForeignKey(x => x.FromUoMId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Quan hệ với ĐVT Đích (ToUoM): Cấm xóa ĐVT nếu đang được dùng làm đích quy đổi
            builder.HasOne(x => x.ToUoM)
                .WithMany()
                .HasForeignKey(x => x.ToUoMId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion
        }
    }
}