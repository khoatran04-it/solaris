using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryTransferConfiguration : IEntityTypeConfiguration<InventoryTransfer>
    {
        public void Configure(EntityTypeBuilder<InventoryTransfer> builder)
        {
            builder.ToTable("InventoryTransfers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TransferCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.TransferCode).IsUnique();

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);

            builder.Property(x => x.DispatchedDate).HasColumnType("datetime2");
            builder.Property(x => x.ReceivedDate).HasColumnType("datetime2");
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Relationships
            builder.HasOne(x => x.FromWarehouse)
                   .WithMany()
                   .HasForeignKey(x => x.FromWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ToWarehouse)
                   .WithMany()
                   .HasForeignKey(x => x.ToWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Order)
                   .WithMany()
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DispatchedBy)
                   .WithMany()
                   .HasForeignKey(x => x.DispatchedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReceivedBy)
                   .WithMany()
                   .HasForeignKey(x => x.ReceivedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.InspectedBy)
                   .WithMany()
                   .HasForeignKey(x => x.InspectedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DeliveryTrip)
                   .WithMany()
                   .HasForeignKey(x => x.DeliveryTripId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.InspectedDate).HasColumnType("datetime2");
            builder.Property(x => x.DriverName).HasMaxLength(100);
            builder.Property(x => x.DriverPhone).HasMaxLength(20).HasColumnType("varchar(20)");
            builder.Property(x => x.LicensePlate).HasMaxLength(20).HasColumnType("varchar(20)");
        }
    }
}
