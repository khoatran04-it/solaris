using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerTierConfiguration : IEntityTypeConfiguration<CustomerTier>
    {
        public void Configure(EntityTypeBuilder<CustomerTier> builder)
        {
            builder.ToTable("CustomerTiers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("CustomerTierId");

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasColumnType("nvarchar(20)");

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100)
                   .HasColumnType("nvarchar(100)");

            builder.Property(x => x.DiscountPercent)
                   .HasPrecision(5, 2);

            builder.Property(x => x.MinSpending)
                   .HasPrecision(18, 2);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasMany(x => x.Customers)
                   .WithOne(c => c.CustomerTier)
                   .HasForeignKey(c => c.CustomerTierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}