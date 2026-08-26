using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API và quy tắc ràng buộc cho bảng Nhóm Đơn vị tính (UoMCategory).
    /// </summary>
    public class UoMCategoryConfiguration : IEntityTypeConfiguration<UoMCategory>
    {
        public void Configure(EntityTypeBuilder<UoMCategory> builder)
        {
            builder.ToTable("UoMCategories");

            builder.HasKey(x => x.Id);

            #region Cấu hình Thuộc tính (Properties)
            // Mã nhóm viết hoa không dấu, tối đa 50 ký tự
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            // Tên nhóm có dấu, tối đa 100 ký tự
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

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

            #region Chỉ mục & Bộ lọc (Indexes & Query Filters)
            builder.HasIndex(x => x.Code)
                .IsUnique();

            // Bộ lọc toàn cục: Tự động loại bỏ các bản ghi đã xóa mềm khỏi tất cả truy vấn
            builder.HasQueryFilter(x => !x.IsDeleted);
            #endregion

            #region Khóa ngoại & Mối quan hệ (Foreign Keys)
            // Quan hệ 1-N một chiều: Nhóm trỏ tới ĐVT cơ sở (Base UoM)
            // Cấm xóa đệ quy (Restrict) để tránh xung đột vòng lặp quan hệ với bảng UoM
            builder.HasOne(x => x.BaseUoM)
                .WithMany()
                .HasForeignKey(x => x.BaseUoMId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion
        }
    }
}