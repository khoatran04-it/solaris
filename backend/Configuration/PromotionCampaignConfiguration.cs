using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations
{
    public class PromotionCampaignConfiguration : IEntityTypeConfiguration<PromotionCampaign>
    {
        public void Configure(EntityTypeBuilder<PromotionCampaign> builder)
        {
            builder.ToTable("PromotionCampaigns");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            // 🔥 RẤT QUAN TRỌNG: Phải định nghĩa kiểu decimal cho tiền tệ/phần trăm để tránh lỗi truncate
            builder.Property(x => x.DiscountValue)
                .HasColumnType("decimal(18,2)");

            // Đánh index cho ngày tháng để sau này Query lọc chiến dịch đang diễn ra cho nhanh
            builder.HasIndex(x => new { x.StartDate, x.EndDate });
            builder.HasIndex(x => x.IsActive);
        }
    }
}