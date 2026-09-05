using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API cho bảng Quyền hạn (IAPermission).
    /// </summary>
    public class IAPermissionConfiguration : IEntityTypeConfiguration<IAPermission>
    {
        public void Configure(EntityTypeBuilder<IAPermission> builder)
        {
            builder.ToTable("IAPermissions");

            builder.HasKey(x => x.Id);

            #region Cấu hình thuộc tính & Chỉ mục
            // Sử dụng nvarchar cho Module vì lưu chuỗi Tiếng Việt có dấu
            builder.Property(x => x.Module)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");
            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");
            #endregion
        }
    }
}