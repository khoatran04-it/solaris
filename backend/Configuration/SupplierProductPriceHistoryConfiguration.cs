using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class SupplierProductPriceHistoryConfiguration : IEntityTypeConfiguration<SupplierProductPriceHistory>
    {
        public void Configure(EntityTypeBuilder<SupplierProductPriceHistory> builder)
        {
            builder.ToTable("SupplierProductPriceHistories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OldPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.NewPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.PriceChange).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ChangeType).HasMaxLength(50).HasColumnType("varchar(50)");
            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.EffectiveDate).HasColumnType("datetime2");

            builder.HasIndex(x => x.VariantId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.EffectiveDate);

            builder.HasOne(x => x.SupplierProduct)
                   .WithMany()
                   .HasForeignKey(x => x.SupplierProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Supplier)
                   .WithMany()
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Variant)
                   .WithMany()
                   .HasForeignKey(x => x.VariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PurchaseUoM)
                   .WithMany()
                   .HasForeignKey(x => x.PurchaseUoMId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
