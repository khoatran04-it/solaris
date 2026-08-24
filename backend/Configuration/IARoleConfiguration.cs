using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    /// <summary>
    /// Cấu hình Fluent API cho thực thể Vai trò (IARole).
    /// </summary>
    public class IARoleConfiguration : IEntityTypeConfiguration<IARole>
    {
        public void Configure(EntityTypeBuilder<IARole> builder)
        {
            builder.ToTable("IARoles");

            builder.HasKey(x => x.Id);

            #region Định danh & Thông tin vai trò
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");
            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("nvarchar(150)");

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");
            #endregion

            #region Audit & Global Query Filter (Soft Delete)
            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            builder.HasQueryFilter(x => !x.IsDeleted);
            #endregion
        }
    }
}