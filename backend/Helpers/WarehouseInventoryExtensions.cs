using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend.Helpers
{
    /// <summary>
    /// Helper extension methods lọc tồn kho theo quy tắc Chuỗi cung ứng (SCM).
    /// </summary>
    public static class WarehouseInventoryExtensions
    {
        /// <summary>
        /// Bộ lọc tồn kho chỉ cho phép các Kho Bán Lẻ (Retail) xuất bán trực tiếp cho khách hàng.
        /// Chặn tuyệt đối tồn kho thuộc Kho Tổng, Trạm Trung Chuyển, Kho Hàng Lỗi.
        /// Tự động tương thích với các unit tests cô lập (khi bảng Warehouses chưa được seed).
        /// </summary>
        public static async Task<IQueryable<WarehouseInventory>> FilterRetailOnlyAsync(
            this IQueryable<WarehouseInventory> query,
            SolarisDbContext context)
        {
            var retailWhIds = await context.Warehouses
                .Where(w => w.WarehouseType == WarehouseTypeConstants.Retail)
                .Select(w => w.Id)
                .ToListAsync();

            if (retailWhIds.Count > 0)
            {
                return query.Where(wi => retailWhIds.Contains(wi.WarehouseId));
            }

            var nonRetailWhIds = await context.Warehouses
                .Where(w => w.WarehouseType == WarehouseTypeConstants.MasterHub
                         || w.WarehouseType == WarehouseTypeConstants.Transit
                         || w.WarehouseType == WarehouseTypeConstants.Damaged)
                .Select(w => w.Id)
                .ToListAsync();

            if (nonRetailWhIds.Count > 0)
            {
                return query.Where(wi => !nonRetailWhIds.Contains(wi.WarehouseId));
            }

            return query;
        }
    }
}
