using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerReturnConfiguration : IEntityTypeConfiguration<CustomerReturn>
    {
        public void Configure(EntityTypeBuilder<CustomerReturn> builder)
        {
            builder.ToTable("CustomerReturns");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReturnCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.ReturnCode).IsUnique();

            builder.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.InspectionNotes).HasMaxLength(500);

            builder.Property(x => x.ReturnDate).HasColumnType("datetime2");
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Relationships
            builder.HasOne(x => x.Order)
                   .WithMany(o => o.CustomerReturns)
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReceivedBy)
                   .WithMany()
                   .HasForeignKey(x => x.ReceivedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DeliveryTrip)
                   .WithMany(t => t.CustomerReturns)
                   .HasForeignKey(x => x.DeliveryTripId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
