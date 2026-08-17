using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class InventoryIssueConfiguration : IEntityTypeConfiguration<InventoryIssue>
    {
        public void Configure(EntityTypeBuilder<InventoryIssue> builder)
        {
            builder.ToTable("InventoryIssues");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IssueCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.IssueCode).IsUnique();

            builder.Property(x => x.ReceiverName).HasMaxLength(150);
            builder.Property(x => x.ReceiverPhone).HasMaxLength(20);
            builder.Property(x => x.DeliveryAddress).HasMaxLength(300);

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);

            builder.Property(x => x.IssueDate).HasColumnType("datetime2");
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Relationships
            builder.HasOne(x => x.Order)
                   .WithMany(o => o.InventoryIssues)
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.IssuedBy)
                   .WithMany()
                   .HasForeignKey(x => x.IssuedById)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
