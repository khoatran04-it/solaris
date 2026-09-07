using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.OrderCode).IsUnique();

            builder.Property(x => x.ReceiverName).HasMaxLength(150);
            builder.Property(x => x.ReceiverPhone).HasMaxLength(20);
            builder.Property(x => x.DeliveryAddress).HasMaxLength(300);

            builder.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ShippingFee).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);

            builder.Property(x => x.OrderDate).HasColumnType("datetime2");
            builder.Property(x => x.DispatchedAt).HasColumnType("datetime2");
            builder.Property(x => x.DeliveredAt).HasColumnType("datetime2");
            builder.Property(x => x.DriverName).HasMaxLength(100);
            builder.Property(x => x.DriverPhone).HasMaxLength(20).HasColumnType("varchar(20)");
            builder.Property(x => x.LicensePlate).HasMaxLength(20).HasColumnType("varchar(20)");
            builder.Property(x => x.ShippingProvider).HasMaxLength(50);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Relationships
            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CustomerAddress)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerAddressId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DeliveryTrip)
                   .WithMany()
                   .HasForeignKey(x => x.DeliveryTripId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
