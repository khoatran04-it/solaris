using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configuration
{
    public class ProductCategoryGroupConfiguration : IEntityTypeConfiguration<ProductCategoryGroup>
    {
        public void Configure(EntityTypeBuilder<ProductCategoryGroup> builder)
        {
            builder.ToTable("ProductCategoryGroups");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("ProductCategoryGroupId");

            builder.Property(x => x.Code).IsRequired().HasMaxLength(20).HasColumnType("varchar(20)");
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.Slug).HasMaxLength(150).HasColumnType("nvarchar(150)");
            builder.HasIndex(x => x.Slug);

            builder.Property(x => x.Description).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            builder.Property(x => x.ImagePath).HasMaxLength(1000);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
