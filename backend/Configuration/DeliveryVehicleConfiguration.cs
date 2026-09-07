using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class DeliveryVehicleConfiguration : IEntityTypeConfiguration<DeliveryVehicle>
    {
        public void Configure(EntityTypeBuilder<DeliveryVehicle> builder)
        {
            builder.ToTable("DeliveryVehicles");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.LicensePlate).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");
            builder.HasIndex(x => x.LicensePlate).IsUnique();

            builder.Property(x => x.VehicleType).IsRequired().HasMaxLength(30).HasColumnType("varchar(30)");
            builder.Property(x => x.MaxWeightKg).HasColumnType("decimal(12,2)");
            builder.Property(x => x.DriverName).HasMaxLength(100);
            builder.Property(x => x.DriverPhone).HasMaxLength(20).HasColumnType("varchar(20)");
            builder.Property(x => x.Status).HasMaxLength(30).HasColumnType("varchar(30)");

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne(x => x.HomeWarehouse)
                   .WithMany()
                   .HasForeignKey(x => x.HomeWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
