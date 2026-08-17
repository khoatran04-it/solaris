using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class UoMConversionConfiguration : IEntityTypeConfiguration<UoMConversion>
    {
        public void Configure(EntityTypeBuilder<UoMConversion> builder)
        {
            builder.ToTable("UoMConversions");
            builder.HasKey(x => x.Id);

            builder.Ignore(x => x.IsStandard); 
            builder.Property(x => x.ConversionFactor).IsRequired().HasColumnType("decimal(18,6)");

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa sản phẩm thì xóa quy đổi của nó

            builder.HasOne(x => x.FromUoM)
                   .WithMany()
                   .HasForeignKey(x => x.FromUoMId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ToUoM)
                   .WithMany()
                   .HasForeignKey(x => x.ToUoMId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}