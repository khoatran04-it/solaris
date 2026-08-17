using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class UoMCategoryConfiguration : IEntityTypeConfiguration<UoMCategory>
    {
        public void Configure(EntityTypeBuilder<UoMCategory> builder)
        {
            builder.ToTable("UoMCategories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");

            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne(x => x.BaseUoM)
                   .WithMany()
                   .HasForeignKey(x => x.BaseUoMId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}