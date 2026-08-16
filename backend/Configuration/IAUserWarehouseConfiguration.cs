using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class IAUserWarehouseConfiguration : IEntityTypeConfiguration<IAUserWarehouse>
    {
        public void Configure(EntityTypeBuilder<IAUserWarehouse> builder)
        {
            builder.ToTable("IAUserWarehouses");

            // Tạo khóa chính kép (Composite Key)
            builder.HasKey(x => new { x.UserId, x.WarehouseId });

            // Quan hệ với bảng IAUser
            builder.HasOne(x => x.User)
                   .WithMany(x => x.UserWarehouses)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa nhân viên thì tự động xóa dòng phân quyền này

            // Quan hệ với bảng Warehouse
            builder.HasOne(x => x.Warehouse)
                   .WithMany(x => x.UserWarehouses)
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa kho thì tự động xóa dòng phân quyền này
        }
    }
}