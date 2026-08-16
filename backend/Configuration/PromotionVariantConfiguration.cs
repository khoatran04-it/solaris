using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations
{
    public class PromotionVariantConfiguration : IEntityTypeConfiguration<PromotionVariant>
    {
        public void Configure(EntityTypeBuilder<PromotionVariant> builder)
        {
            builder.ToTable("PromotionVariants");

            builder.HasKey(x => x.Id);

            // 🔥 BẢO VỆ DỮ LIỆU: Đảm bảo 1 Biến thể không bị add 2 lần vào cùng 1 Chiến dịch
            builder.HasIndex(x => new { x.PromotionCampaignId, x.VariantId })
                .IsUnique();

            // --- THIẾT LẬP KHÓA NGOẠI (FOREIGN KEYS) ---

            // Nối với Bảng Chiến Dịch
            builder.HasOne(x => x.PromotionCampaign)
                .WithMany(x => x.PromotionVariants)
                .HasForeignKey(x => x.PromotionCampaignId)
                // Nếu xóa cứng 1 Chiến dịch, tự động xóa luôn các dòng liên kết ở bảng này
                .OnDelete(DeleteBehavior.Cascade);

            // Nối với Bảng Biến Thể
            builder.HasOne(x => x.Variant)
                .WithMany(x => x.PromotionVariants)
                .HasForeignKey(x => x.VariantId)
                // Nếu xóa biến thể, tự động xóa dòng liên kết khuyến mãi
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}