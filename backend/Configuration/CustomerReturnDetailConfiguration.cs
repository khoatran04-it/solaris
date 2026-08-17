using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerReturnDetailConfiguration : IEntityTypeConfiguration<CustomerReturnDetail>
    {
        public void Configure(EntityTypeBuilder<CustomerReturnDetail> builder)
        {
            builder.ToTable("CustomerReturnDetails");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReturnedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.AcceptedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.DamagedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.RejectReason).HasMaxLength(500);

            // Relationships
            builder.HasOne(x => x.CustomerReturn)
                   .WithMany(r => r.Details)
                   .HasForeignKey(x => x.CustomerReturnId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Batch)
                   .WithMany()
                   .HasForeignKey(x => x.BatchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UoM)
                   .WithMany()
                   .HasForeignKey(x => x.UoMId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
