using backend.Data;
using backend.DTOs.OrderDTOs;
using backend.Helpers;
using backend.Models;
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

        public async Task<RoutingResultDto> DetermineOptimalWarehouseAsync(
            int? customerAddressId,
            List<OrderDetailCreateDto> items,
            double directLat = 0,
            double directLng = 0,
            string? province = null,
            string? district = null)
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

            double custLat = directLat;
            double custLon = directLng;
            string custProvince = province?.Trim() ?? string.Empty;
            string custDistrict = district?.Trim() ?? string.Empty;

            if (customerAddressId.HasValue)
            {
                var address = await _context.CustomerAddresses.FindAsync(customerAddressId.Value);
                if (address != null)
                {
                    if (address.Latitude != 0 && address.Longitude != 0)
                    {
                        custLat = address.Latitude;
                        custLon = address.Longitude;
                    }
                    if (string.IsNullOrEmpty(custProvince) && !string.IsNullOrEmpty(address.Province))
                        custProvince = address.Province;
                    if (string.IsNullOrEmpty(custDistrict) && !string.IsNullOrEmpty(address.District))
                        custDistrict = address.District;
                }
            }

            // Bỏ qua tọa độ nếu bị gán nhầm tọa độ mặc định Chợ Bến Thành Quận 1 (10.7769, 106.7009) khi quận của khách khác Quận 1
            if (custLat != 0 && custLon != 0 &&
                Math.Abs(custLat - 10.7769) < 0.001 && Math.Abs(custLon - 106.7009) < 0.001 &&
                !string.IsNullOrEmpty(custDistrict) && !GeoHelper.IsSameLocation(custDistrict, "Quận 1"))
            {
                custLat = 0;
                custLon = 0;
            }

            // Nếu không có GPS thực tế: Tự động tra cứu Ma trận 105 Tọa độ Trọng tâm Hành chính (GIS Centroid)
            if (custLat == 0 && custLon == 0)
            {
                var centroid = GeoHelper.FindCoordinates(custDistrict, custProvince);
                if (centroid.HasValue)
                {
                    custLat = centroid.Value.Lat;
                    custLon = centroid.Value.Lng;
                }
            }

            // 1. Tính khoảng cách an toàn (áp dụng Hệ số uốn khúc đường bộ thực tế K = 1.25)
            double CalculateSafeDistance(Warehouse wh)
            {
                if (wh.Address == null) return 5.0;

                bool hasCustCoords = custLat != 0 && custLon != 0;
                bool hasWhCoords = wh.Address.Latitude != 0 && wh.Address.Longitude != 0;

                // Ưu tiên 1: Tính khoảng cách đường bộ thực tế từ Tọa độ GPS thật hoặc Tọa độ Trọng tâm Quận
                if (hasCustCoords && hasWhCoords)
                {
                    double d = _distanceService.CalculateDistanceKm(custLat, custLon, wh.Address.Latitude, wh.Address.Longitude);
                    if (!double.IsInfinity(d) && !double.IsNaN(d) && d < 99999)
                    {
                        // Nhân hệ số uốn khúc đường sá đô thị 1.25 (Road Tortuosity Factor)
                        return Math.Round(d * 1.25, 2);
                    }
                }

                // Fallback cấp 1: Cùng quận/huyện giữa khách hàng và kho hàng (nếu thiếu tọa độ cả 2 bên)
                if (!string.IsNullOrEmpty(wh.Address.District) && !string.IsNullOrEmpty(custDistrict))
                {
                    if (GeoHelper.IsSameLocation(wh.Address.District, custDistrict))
                    {
                        return 2.5; // Cùng quận: Kho nằm ngay tại quận của khách hàng!
                    }
                }

                // Fallback cấp 2: Phân cấp địa lý theo Tỉnh/Thành phố
                if (!string.IsNullOrEmpty(wh.Address.Province) && !string.IsNullOrEmpty(custProvince))
                {
                    bool sameProvince = GeoHelper.IsSameLocation(wh.Address.Province, custProvince);
                    if (sameProvince) return 8.0; // Cùng tỉnh/TP: ước tính ~8.0km
                    return 100.0; // Khác tỉnh thành
                }

                return 5.0; // Mặc định trong nội thành
            }

            // Tính khoảng cách và sắp xếp kho từ gần nhất -> xa nhất
            var warehousesWithDistance = warehouses.Select(w => new
            {
                Warehouse = w,
                Distance = CalculateSafeDistance(w)
            })
            .OrderBy(x => x.Distance)
            .ToList();

            // 2. Kiểm tra tồn kho khả dụng tại từng kho (chỉ tính các lô còn hạn sử dụng)
            var now = DateTime.UtcNow;
            foreach (var item in warehousesWithDistance)
            {
                var wh = item.Warehouse;
                bool isStockSufficient = true;
                var missingList = new List<MissingItemDto>();

                foreach (var orderItem in items)
                {
                    var available = await _context.WarehouseInventories
                        .Where(i => i.WarehouseId == wh.Id && i.VariantId == orderItem.VariantId && (i.Batch == null || i.Batch.ExpiryDate > now))
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

            // 3. Nếu không có kho nào đủ 100% -> Chọn kho gần nhất làm kho tối ưu và báo danh sách hàng thiếu
            var nearest = warehousesWithDistance.First();
            var targetMissingList = new List<MissingItemDto>();

            foreach (var orderItem in items)
            {
                var available = await _context.WarehouseInventories
                    .Where(i => i.WarehouseId == nearest.Warehouse.Id && i.VariantId == orderItem.VariantId && (i.Batch == null || i.Batch.ExpiryDate > now))
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

            return new RoutingResultDto
            {
                OptimalWarehouseId = nearest.Warehouse.Id,
                WarehouseName = nearest.Warehouse.Name,
                DistanceKm = nearest.Distance,
                IsFullyStocked = false,
                MissingItems = targetMissingList
            };
        }
    }
}
