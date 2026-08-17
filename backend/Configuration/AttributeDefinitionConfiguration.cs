using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
    {
        public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
        {
            builder.ToTable("AttributeDefinitions");
            builder.HasKey(x => x.Id);

            // Tên thuộc tính phải là duy nhất trên toàn hệ thống (không được có 2 chữ "Độ ngọt")
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.HasIndex(x => x.Name).IsUnique();

            // Loại dữ liệu (Text, Number, Date...)
            builder.Property(x => x.DataType).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}