using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class DeliveryTripOrderConfiguration : IEntityTypeConfiguration<DeliveryTripOrder>
    {
        public void Configure(EntityTypeBuilder<DeliveryTripOrder> builder)
        {
            builder.ToTable("DeliveryTripOrders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).IsRequired().HasMaxLength(30).HasColumnType("varchar(30)");
            builder.Property(x => x.DeliveredAt).HasColumnType("datetime2");
            builder.Property(x => x.FailureReason).HasMaxLength(250);
            builder.Property(x => x.Note).HasMaxLength(500);

            builder.HasIndex(x => new { x.TripId, x.OrderId }).IsUnique();

            builder.HasOne(x => x.Trip)
                   .WithMany(t => t.TripOrders)
                   .HasForeignKey(x => x.TripId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Order)
                   .WithMany()
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
