using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            builder.HasIndex(x => x.Code).IsUnique(); // Mã kho không được trùng

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property(x => x.WarehouseType).HasMaxLength(50).HasColumnType("nvarchar(50)");

            // Thông số Sức chứa Vật lý
            builder.Property(x => x.TotalAreaSqm).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalCapacityCbm).HasColumnType("decimal(18,2)");
            builder.Property(x => x.MaxWeightCapacityKg).HasColumnType("decimal(18,2)");
            builder.Property(x => x.MaxPalletPositions);
            builder.Property(x => x.WarningThresholdPercent).HasDefaultValue(85);
            builder.Property(x => x.MaxColdChainRadiusKm).HasDefaultValue(15.0);

            // Audit & Soft Delete
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.IsActive);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            // --- QUAN HỆ KHÓA NGOẠI ---

            // 1 Kho có 1 Địa chỉ (Không cho phép xóa địa chỉ nếu đang có kho dùng)
            builder.HasOne(x => x.Address)
                   .WithMany()
                   .HasForeignKey(x => x.AddressId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 1 Kho có 1 Trưởng kho (Nếu xóa tài khoản Trưởng kho -> Cột này thành NULL)
            builder.HasOne(x => x.Manager)
                   .WithMany()
                   .HasForeignKey(x => x.ManagerId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}