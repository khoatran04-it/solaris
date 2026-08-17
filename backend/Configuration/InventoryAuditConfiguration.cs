using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configuration
{
    public class InventoryAuditConfiguration : IEntityTypeConfiguration<InventoryAudit>
    {
        public void Configure(EntityTypeBuilder<InventoryAudit> builder)
        {
            builder.ToTable("InventoryAudits");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.AuditCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(a => a.AuditCode).IsUnique();

            builder.Property(a => a.TotalSystemQty).HasColumnType("decimal(18,4)");
            builder.Property(a => a.TotalActualQty).HasColumnType("decimal(18,4)");
            builder.Property(a => a.TotalVarianceQty).HasColumnType("decimal(18,4)");
            builder.Property(a => a.TotalVarianceAmount).HasColumnType("decimal(18,4)");

            builder.HasOne(a => a.Warehouse)
                .WithMany()
                .HasForeignKey(a => a.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Auditor)
                .WithMany()
                .HasForeignKey(a => a.AuditorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.ApprovedBy)
                .WithMany()
                .HasForeignKey(a => a.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.Details)
                .WithOne(d => d.Audit)
                .HasForeignKey(d => d.AuditId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class InventoryAuditDetailConfiguration : IEntityTypeConfiguration<InventoryAuditDetail>
    {
        public void Configure(EntityTypeBuilder<InventoryAuditDetail> builder)
        {
            builder.ToTable("InventoryAuditDetails");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.SystemQuantity).HasColumnType("decimal(18,4)");
            builder.Property(d => d.ActualQuantity).HasColumnType("decimal(18,4)");
            builder.Property(d => d.VarianceQuantity).HasColumnType("decimal(18,4)");
            builder.Property(d => d.UnitPrice).HasColumnType("decimal(18,4)");
            builder.Property(d => d.VarianceAmount).HasColumnType("decimal(18,4)");

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
