using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryTransferDetailConfiguration : IEntityTypeConfiguration<InventoryTransferDetail>
    {
        public void Configure(EntityTypeBuilder<InventoryTransferDetail> builder)
        {
            builder.ToTable("InventoryTransferDetails");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.ActualReceivedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.DamagedQuantity).HasColumnType("decimal(18,3)");

            // Relationships
            builder.HasOne(x => x.InventoryTransfer)
                   .WithMany(t => t.Details)
                   .HasForeignKey(x => x.InventoryTransferId)
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
