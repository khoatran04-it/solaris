using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class SupplierAddressConfiguration : IEntityTypeConfiguration<SupplierAddress>
    {
        public void Configure(EntityTypeBuilder<SupplierAddress> builder)
        {
            builder.ToTable("SupplierAddresses");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("SupplierAddressId");

            builder.Ignore(x => x.FullAddress);

            builder.Property(x => x.ContactName).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.ContactPhone).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");

            builder.Property(x => x.Province).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.District).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.Ward).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.StreetAddress).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");

            builder.Property(x => x.IsDefault).HasDefaultValue(false);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            /// Cho phép xóa Cascase: xóa nhà cung cấp thì xóa luôn danh sách địa chỉ của nó
            builder.HasOne(x => x.Supplier)
                   .WithMany(c => c.Addresses)
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}