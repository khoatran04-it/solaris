using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("CustomerId");

            // --- CẤU HÌNH CÁC CỘT CƠ BẢN ---
            builder.Property(x => x.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");

            builder.Property(x => x.Email).HasMaxLength(150).HasColumnType("nvarchar(150)");
            builder.Property(x => x.TaxCode).HasMaxLength(20).HasColumnType("nvarchar(20)");
            builder.Property(x => x.AvatarPath).HasColumnType("nvarchar(max)");
            builder.Property(x => x.Note).HasColumnType("nvarchar(max)");

            builder.Property(x => x.Birthday).HasColumnType("datetime2");
            builder.Property(x => x.Gender).HasColumnType("bit");
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

            builder.Property(x => x.IsActive);

            // --- INDEX UNIQUE (BẮT BUỘC TRONG ERP) ---
            builder.HasIndex(x => x.Code).IsUnique();

            // --- CẤU HÌNH XÓA MỀM ---
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- CẤU HÌNH KHÓA NGOẠI (RELATIONSHIPS) ---
            builder.HasOne(x => x.CustomerType)
                   .WithMany(t => t.Customers)
                   .HasForeignKey(x => x.CustomerTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CustomerTier)
                   .WithMany(t => t.Customers)
                   .HasForeignKey(x => x.CustomerTierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}