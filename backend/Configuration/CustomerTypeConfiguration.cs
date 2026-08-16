using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerTypeConfiguration : IEntityTypeConfiguration<CustomerType>
    {
        public void Configure(EntityTypeBuilder<CustomerType> builder)
        {
            builder.ToTable("CustomerTypes");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                   .HasColumnName("CustomerTypeId");

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasColumnType("nvarchar(20)");

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200)
                   .HasColumnType("nvarchar(200)");

            builder.Property(x => x.Description)
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.CreatedAt)
                   .HasColumnType("datetime2");

            builder.Property(x => x.UpdatedAt)
                   .HasColumnType("datetime2");

            builder.HasMany(x => x.Customers)
                   .WithOne(c => c.CustomerType)
                   .HasForeignKey(c => c.CustomerTypeId)
                   .OnDelete(DeleteBehavior.Restrict); // Chống xóa loại KH nếu đang có KH thuộc loại này

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}