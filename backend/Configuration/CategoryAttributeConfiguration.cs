using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
    {
        public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
        {
            builder.ToTable("CategoryAttributes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsRequired).HasDefaultValue(false);

            // Composite Unique Index: Đảm bảo 1 Danh mục không bị gán trùng 1 thuộc tính 2 lần
            builder.HasIndex(x => new { x.CategoryId, x.AttributeDefinitionId }).IsUnique();

            // --- CONFIGURATION KHÓA NGOẠI ---
            // Nối với Danh mục (Category)
            builder.HasOne(x => x.Category)
                   .WithMany(c => c.AttributeTemplates)
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Danh mục thì xóa luôn khung thuộc tính của nó

            // Nối với Từ điển (AttributeDefinition)
            builder.HasOne(x => x.AttributeDefinition)
                   .WithMany()
                   .HasForeignKey(x => x.AttributeDefinitionId)
                   .OnDelete(DeleteBehavior.Restrict); // Không cho phép xóa Từ điển nếu đang có Danh mục sử dụng
        }
    }
}