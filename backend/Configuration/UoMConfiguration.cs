using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API và ràng buộc toàn vẹn dữ liệu cho bảng Đơn vị tính (UoM).
    /// </summary>
    public class UoMConfiguration : IEntityTypeConfiguration<UoM>
    {
        public void Configure(EntityTypeBuilder<UoM> builder)
        {
            builder.ToTable("UoMs");

            builder.HasKey(x => x.Id);

            #region Cấu hình Thuộc tính (Properties)
            // Mã đơn vị tính viết hoa không dấu (Ví dụ: KG, G, PCS, BOX)
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            // Tên đơn vị tính hiển thị (Ví dụ: Kilogram, Thùng, Hộp)
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            // Từ khóa tìm kiếm hoặc tên gọi đồng nghĩa (Ví dụ: "ký, cân, kilogam")
            builder.Property(x => x.Synonyms)
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

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
            builder.HasIndex(x => x.Code)
                .IsUnique();

            // Bộ lọc toàn cục: Tự động loại trừ các đơn vị tính đã bị xóa mềm
            builder.HasQueryFilter(x => !x.IsDeleted);
            #endregion

            #region Khóa ngoại & Mối quan hệ (Foreign Keys)
            // Quan hệ Nhiều - 1: Một ĐVT thuộc về 1 Nhóm ĐVT (UoMCategory)
            // Cấm xóa đệ quy (Restrict) để đảm bảo toàn vẹn dữ liệu nếu nhóm đang chứa ĐVT
            builder.HasOne(x => x.Category)
                .WithMany(c => c.UoMs)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion
        }
    }
}