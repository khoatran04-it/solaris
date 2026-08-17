using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configuration
{
    public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
    {
        public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
        {
            builder.ToTable("InventoryAdjustments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.AdjustmentCode)
                .IsRequired()
                .HasMaxLength(50);
            builder.HasIndex(a => a.AdjustmentCode).IsUnique();

            builder.Property(a => a.TotalVarianceAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(a => a.AdjustmentDate).HasColumnType("datetime2");
            builder.Property(a => a.ApprovedDate).HasColumnType("datetime2");
            builder.Property(a => a.CreatedAt).HasColumnType("datetime2");
            builder.Property(a => a.UpdatedAt).HasColumnType("datetime2");
            builder.Property(a => a.DeletedAt).HasColumnType("datetime2");

            builder.Property(a => a.IsDeleted);

            // GLOBAL QUERY FILTER: Tự động loại trừ các bản ghi đã xóa mềm
            builder.HasQueryFilter(a => !a.IsDeleted);

            builder.HasOne(a => a.Warehouse)
                .WithMany()
                .HasForeignKey(a => a.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Audit)
                .WithMany()
                .HasForeignKey(a => a.AuditId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.ApprovedBy)
                .WithMany()
                .HasForeignKey(a => a.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.Details)
                .WithOne(d => d.Adjustment)
                .HasForeignKey(d => d.AdjustmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class InventoryAdjustmentDetailConfiguration : IEntityTypeConfiguration<InventoryAdjustmentDetail>
    {
        public void Configure(EntityTypeBuilder<InventoryAdjustmentDetail> builder)
        {
            builder.ToTable("InventoryAdjustmentDetails");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Quantity).HasColumnType("decimal(18,2)");
            builder.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(d => d.TotalAmount).HasColumnType("decimal(18,2)");

            builder.HasOne(d => d.Variant)
                .WithMany()
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Batch)
                .WithMany()
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.UoM)
                .WithMany()
                .HasForeignKey(d => d.UoMId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
