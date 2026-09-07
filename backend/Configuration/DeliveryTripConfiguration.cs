using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class DeliveryTripConfiguration : IEntityTypeConfiguration<DeliveryTrip>
    {
        public void Configure(EntityTypeBuilder<DeliveryTrip> builder)
        {
            builder.ToTable("DeliveryTrips");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TripCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.TripCode).IsUnique();

            builder.Property(x => x.TripType).IsRequired().HasMaxLength(30).HasColumnType("varchar(30)");
            builder.Property(x => x.Status).IsRequired().HasMaxLength(30).HasColumnType("varchar(30)");

            builder.Property(x => x.LicensePlate).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");
            builder.Property(x => x.DriverName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.DriverPhone).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");

            builder.Property(x => x.StartedAt).HasColumnType("datetime2");
            builder.Property(x => x.CompletedAt).HasColumnType("datetime2");
            builder.Property(x => x.Note).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Relationships
            builder.HasOne(x => x.Vehicle)
                   .WithMany(v => v.Trips)
                   .HasForeignKey(x => x.VehicleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.InventoryTransfer)
                   .WithMany()
                   .HasForeignKey(x => x.InventoryTransferId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
