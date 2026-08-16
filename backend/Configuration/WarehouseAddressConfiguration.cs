using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class WarehouseAddressConfiguration : IEntityTypeConfiguration<WarehouseAddress>
    {
        public void Configure(EntityTypeBuilder<WarehouseAddress> builder)
        {
            builder.ToTable("WarehouseAddresses");
            builder.HasKey(x => x.Id);

            // Bắt buộc và giới hạn độ dài
            builder.Property(x => x.Province).IsRequired().HasMaxLength(100);
            builder.Property(x => x.District).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Ward).IsRequired().HasMaxLength(100);
            builder.Property(x => x.StreetAddress).IsRequired().HasMaxLength(255);

            // Bỏ qua cột tính toán này khi map xuống Database
            builder.Ignore(x => x.FullAddress);

            // Tọa độ GPS cần độ chính xác cao
            builder.Property(x => x.Latitude).HasColumnType("float");
            builder.Property(x => x.Longitude).HasColumnType("float");
        }
    }
}