using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class PromotionVariantConfiguration : IEntityTypeConfiguration<PromotionVariant>
    {
        public void Configure(EntityTypeBuilder<PromotionVariant> builder)
        {
            builder.ToTable("PromotionVariants");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");

            // Bảo vệ dữ liệu: Đảm bảo 1 Biến thể không bị gán 2 lần vào cùng 1 Chiến dịch
            builder.HasIndex(x => new { x.PromotionCampaignId, x.VariantId })
                .IsUnique();

            // --- THIẾT LẬP KHÓA NGOẠI (FOREIGN KEYS) ---
            builder.HasOne(x => x.PromotionCampaign)
                .WithMany(x => x.PromotionVariants)
                .HasForeignKey(x => x.PromotionCampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Variant)
                .WithMany(x => x.PromotionVariants)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}