using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class SupplierTypeConfiguration : IEntityTypeConfiguration<SupplierType>
    {
        public void Configure(EntityTypeBuilder<SupplierType> builder)
        {
            builder.ToTable("SupplierTypes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("SupplierTypeId");

            builder.Property(x => x.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.Description).HasColumnType("nvarchar(max)");

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

            builder.HasIndex(x => x.Code).IsUnique();

            // Chống xóa loại nhà cung cấp nếu có nhà cung cấp liên kết
            builder.HasMany(x => x.Suppliers)
                   .WithOne(s => s.SupplierType)
                   .HasForeignKey(s => s.SupplierTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Soft delete
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}