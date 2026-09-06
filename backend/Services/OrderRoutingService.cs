using backend.Data;
using backend.DTOs.OrderDTOs;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class OrderRoutingService : IOrderRoutingService
    {
        private readonly SolarisDbContext _context;
        private readonly IDistanceService _distanceService;

        public OrderRoutingService(SolarisDbContext context, IDistanceService distanceService)
        {
            _context = context;
            _distanceService = distanceService;
        }

        public async Task<RoutingResultDto> DetermineOptimalWarehouseAsync(int? customerAddressId, List<OrderDetailCreateDto> items)
        {
            // Chỉ định tuyến đơn hàng đến Kho Bán Lẻ đang hoạt động
            var warehouses = await _context.Warehouses
                .Include(w => w.Address)
                .Where(w => w.IsActive && !w.IsDeleted && (w.WarehouseType == WarehouseTypeConstants.Retail || string.IsNullOrEmpty(w.WarehouseType)))
                .ToListAsync();

            if (!warehouses.Any())
            {
                throw new InvalidOperationException("Hệ thống chưa có kho hàng nào đang hoạt động.");
            }

            double custLat = 0;
            double custLon = 0;

            if (customerAddressId.HasValue)
            {
                var address = await _context.CustomerAddresses.FindAsync(customerAddressId.Value);
                if (address != null)
                {
                    custLat = address.Latitude;
                    custLon = address.Longitude;
                }
            }

            // 1. Tính khoảng cách và sắp xếp kho từ gần nhất -> xa nhất
            var warehousesWithDistance = warehouses.Select(w => new
            {
                Warehouse = w,
                Distance = (custLat != 0 && custLon != 0 && w.Address != null)
                    ? _distanceService.CalculateDistanceKm(custLat, custLon, w.Address.Latitude, w.Address.Longitude)
                    : 0.0
            })
            .OrderBy(x => x.Distance)
            .ToList();

            // 2. Kiểm tra tồn kho khả dụng tại từng kho
            foreach (var item in warehousesWithDistance)
            {
                var wh = item.Warehouse;
                bool isStockSufficient = true;
                var missingList = new List<MissingItemDto>();

                foreach (var orderItem in items)
                {
                    var available = await _context.WarehouseInventories
                        .Where(i => i.WarehouseId == wh.Id && i.VariantId == orderItem.VariantId)
                        .SumAsync(i => (decimal?)i.QuantityAvailable) ?? 0;

                    if (available < orderItem.Quantity)
                    {
                        isStockSufficient = false;
                        var variant = await _context.ProductVariants.FindAsync(orderItem.VariantId);
                        missingList.Add(new MissingItemDto
                        {
                            VariantId = orderItem.VariantId,
                            VariantName = variant?.Name ?? string.Empty,
                            RequestedQuantity = orderItem.Quantity,
                            AvailableQuantity = available
                        });
                    }
                }

                // Nếu kho này đủ 100% hàng -> Chọn ngay kho này
                if (isStockSufficient)
                {
                    return new RoutingResultDto
                    {
                        OptimalWarehouseId = wh.Id,
                        WarehouseName = wh.Name,
                        DistanceKm = item.Distance,
                        IsFullyStocked = true,
                        MissingItems = new()
                    };
                }
            }

            // 3. Nếu không có kho nào đủ 100% -> Chọn kho gần nhất làm kho đích, tìm kho phụ cung ứng
            var nearest = warehousesWithDistance.First();
            var targetMissingList = new List<MissingItemDto>();

            foreach (var orderItem in items)
            {
                var available = await _context.WarehouseInventories
                    .Where(i => i.WarehouseId == nearest.Warehouse.Id && i.VariantId == orderItem.VariantId)
                    .SumAsync(i => (decimal?)i.QuantityAvailable) ?? 0;

                if (available < orderItem.Quantity)
                {
                    var variant = await _context.ProductVariants.FindAsync(orderItem.VariantId);
                    targetMissingList.Add(new MissingItemDto
                    {
                        VariantId = orderItem.VariantId,
                        VariantName = variant?.Name ?? string.Empty,
                        RequestedQuantity = orderItem.Quantity,
                        AvailableQuantity = available
                    });
                }
            }

            // Tìm kho nguồn có hàng còn thiếu
            int? suggestedSourceId = null;
            string? suggestedSourceName = null;

            if (targetMissingList.Any())
            {
                var firstMissing = targetMissingList.First();
                var sourceWh = await _context.WarehouseInventories
                    .Include(i => i.Warehouse)
                    .Where(i => i.WarehouseId != nearest.Warehouse.Id && 
                                i.VariantId == firstMissing.VariantId && 
                                i.QuantityAvailable >= firstMissing.MissingQuantity &&
                                (i.Warehouse == null || i.Warehouse.WarehouseType != WarehouseTypeConstants.Damaged))
                    .Select(i => i.Warehouse)
                    .FirstOrDefaultAsync();

                if (sourceWh != null)
                {
                    suggestedSourceId = sourceWh.Id;
                    suggestedSourceName = sourceWh.Name;
                }
            }

            return new RoutingResultDto
            {
                OptimalWarehouseId = nearest.Warehouse.Id,
                WarehouseName = nearest.Warehouse.Name,
                DistanceKm = nearest.Distance,
                IsFullyStocked = false,
                MissingItems = targetMissingList,
                SuggestedSourceWarehouseId = suggestedSourceId,
                SuggestedSourceWarehouseName = suggestedSourceName
            };
        }
    }
}
