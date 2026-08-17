using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class PromotionCampaignConfiguration : IEntityTypeConfiguration<PromotionCampaign>
    {
        public void Configure(EntityTypeBuilder<PromotionCampaign> builder)
        {
            builder.ToTable("PromotionCampaigns");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("nvarchar(255)");

            builder.Property(x => x.Description)
                .HasMaxLength(1000)
                .HasColumnType("nvarchar(1000)");

            builder.Property(x => x.DiscountValue)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.StartDate)
                .HasColumnType("datetime2");

            builder.Property(x => x.EndDate)
                .HasColumnType("datetime2");

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Indexes
            builder.HasIndex(x => new { x.StartDate, x.EndDate });
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Name);
        }
    }
}