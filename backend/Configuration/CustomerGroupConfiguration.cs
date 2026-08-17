using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
    {
        public void Configure(EntityTypeBuilder<CustomerGroup> builder)
        {
            builder.ToTable("CustomerGroups");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("CustomerGroupId");

            builder.Property(x => x.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
            builder.Property(x => x.IsActive);

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

            // Unique index trên Code
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}