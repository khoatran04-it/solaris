using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class CustomerGroupLinkConfiguration : IEntityTypeConfiguration<CustomerGroupLink>
    {
        public void Configure(EntityTypeBuilder<CustomerGroupLink> builder)
        {
            builder.ToTable("CustomerGroupLinks");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasKey(x => new { x.CustomerId, x.CustomerGroupId });

            // Quan hệ với Customer
            builder.HasOne(x => x.Customer)
                   .WithMany(c => c.GroupLinks)
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Khách hàng thì tự động xóa kết nối này 

            // Quan hệ với CustomerGroup
            builder.HasOne(x => x.CustomerGroup)
                   .WithMany(g => g.GroupLinks)
                   .HasForeignKey(x => x.CustomerGroupId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Nhóm thì tự động xóa kết nối này
        }
    }
}