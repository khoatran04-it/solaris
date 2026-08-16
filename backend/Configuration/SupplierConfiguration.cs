using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("SupplierId");

            builder.Property(x => x.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.LogoPath).HasColumnType("nvarchar(max)");
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Email).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");

            builder.Property(x => x.TaxCode).HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Website).HasColumnType("nvarchar(max)");
            builder.Property(x => x.SocialLink).HasColumnType("nvarchar(max)");
            builder.Property(x => x.BankAccount).HasMaxLength(50).HasColumnType("nvarchar(50)");
            builder.Property(x => x.BankName).HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property(x => x.Note).HasColumnType("nvarchar(max)");

            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            ///Cấm xóa Loại NCC nếu vẫn có bản ghi NCC thuộc loại NCC đó
            builder.HasOne(x => x.SupplierType)
                   .WithMany(t => t.Suppliers)
                   .HasForeignKey(x => x.SupplierTypeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}