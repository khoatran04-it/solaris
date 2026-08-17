using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(EntityTypeBuilder<OrderDetail> builder)
        {
            builder.ToTable("OrderDetails");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.BaseQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.IssuedQuantity).HasColumnType("decimal(18,3)");

            // Relationships
            builder.HasOne(x => x.Order)
                   .WithMany(o => o.Details)
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UoM)
                   .WithMany()
                   .HasForeignKey(x => x.UoMId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
