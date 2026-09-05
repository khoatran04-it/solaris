using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryIssueDetailConfiguration : IEntityTypeConfiguration<InventoryIssueDetail>
    {
        public void Configure(EntityTypeBuilder<InventoryIssueDetail> builder)
        {
            builder.ToTable("InventoryIssueDetails");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalWeightKg).HasColumnType("decimal(18,3)");
            builder.Property(x => x.TotalCbm).HasColumnType("decimal(18,4)");

            // Relationships
            builder.HasOne(x => x.InventoryIssue)
                   .WithMany(i => i.Details)
                   .HasForeignKey(x => x.InventoryIssueId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.OrderDetail)
                   .WithMany()
                   .HasForeignKey(x => x.OrderDetailId)
                   .OnDelete(DeleteBehavior.Restrict);

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
