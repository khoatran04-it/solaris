using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            builder.ToTable("CustomerAddresses");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("CustomerAddressId");

            // --- BỎ QUA CỘT ẢO (Quan trọng) ---
            // Báo cho EF Core biết FullAddress chỉ là cột tính toán trên RAM, không tạo trong SQL
            builder.Ignore(x => x.FullAddress);

            builder.Property(x => x.ReceiverName).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Province).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.District).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.Ward).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.StreetAddress).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");

            builder.Property(x => x.IsDefault).HasDefaultValue(false);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne(x => x.Customer)
                   .WithMany(c => c.Addresses)
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade); //Cho phép xóa dây chuyền (Cascade) nếu Khách hàng bị xóa
        }
    }
}