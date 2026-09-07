using AutoMapper;
using backend.Data;
using backend.DTOs.VehicleDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    public class DeliveryTripService : IDeliveryTripService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public DeliveryTripService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<DeliveryTripReadDto>> GetAllTripsAsync(string? status = null, string? tripType = null)
        {
            var query = _context.DeliveryTrips
                .Include(t => t.Vehicle)
                .Include(t => t.Warehouse)
                .Include(t => t.InventoryTransfer)
                .Include(t => t.TripOrders)
                    .ThenInclude(to => to.Order)
                .Where(t => !t.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status.ToLower() == status.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(tripType))
            {
                query = query.Where(t => t.TripType.ToLower() == tripType.ToLower());
            }

            var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return _mapper.Map<List<DeliveryTripReadDto>>(list);
        }

        public async Task<DeliveryTripReadDto?> GetTripByIdAsync(int id)
        {
            var trip = await _context.DeliveryTrips
                .Include(t => t.Vehicle)
                .Include(t => t.Warehouse)
                .Include(t => t.InventoryTransfer)
                .Include(t => t.TripOrders)
                    .ThenInclude(to => to.Order)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            return trip == null ? null : _mapper.Map<DeliveryTripReadDto>(trip);
        }

        public async Task<DeliveryTripReadDto> CreateTripAsync(DeliveryTripCreateDto dto)
        {
            var vehicle = await _context.DeliveryVehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId && !v.IsDeleted);
            if (vehicle == null)
            {
                throw new InvalidOperationException("Không tìm thấy phương tiện giao hàng.");
            }

            if (vehicle.Status == "Maintenance")
            {
                throw new InvalidOperationException("Phương tiện đang trong trạng thái bảo dưỡng, không thể điều phối chuyến.");
            }

            string todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            int countToday = await _context.DeliveryTrips.CountAsync(t => t.TripCode.StartsWith($"TRIP-{todayStr}"));
            string tripCode = $"TRIP-{todayStr}-{(countToday + 1):D3}";

            var trip = new DeliveryTrip
            {
                TripCode = tripCode,
                TripType = dto.TripType,
                Status = "Preparing",
                VehicleId = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                DriverName = vehicle.DriverName ?? "Tài xế Solaris",
                DriverPhone = vehicle.DriverPhone ?? "0901234567",
                WarehouseId = dto.WarehouseId,
                Note = dto.Note,
                InventoryTransferId = dto.InventoryTransferId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DeliveryTrips.Add(trip);
            await _context.SaveChangesAsync();

            // Gán các đơn hàng B2C vào chuyến nếu có
            if (dto.TripType == "B2C_Delivery" && dto.OrderIds != null && dto.OrderIds.Any())
            {
                int seq = 1;
                var orders = await _context.Orders
                    .Where(o => dto.OrderIds.Contains(o.Id) && !o.IsDeleted)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    trip.TripOrders.Add(new DeliveryTripOrder
                    {
                        TripId = trip.Id,
                        OrderId = order.Id,
                        DeliverySequence = seq++,
                        Status = "Pending"
                    });

                    // Cập nhật thông tin tài xế và chuyển trạng thái đơn sang Shipping
                    order.ShippingProvider = "Internal";
                    order.TrackingCode = $"SLR-EXP-{order.OrderCode}";
                    order.DeliveryTripId = trip.Id;
                    order.DriverName = trip.DriverName;
                    order.DriverPhone = trip.DriverPhone;
                    order.LicensePlate = trip.LicensePlate;
                    order.DispatchedAt = DateTime.UtcNow;
                    order.Status = OrderStatus.Shipping;
                    order.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Gán chuyển kho B2B nếu có
            if (dto.TripType == "B2B_Transfer" && dto.InventoryTransferId.HasValue)
            {
                var transfer = await _context.InventoryTransfers.FindAsync(dto.InventoryTransferId.Value);
                if (transfer != null)
                {
                    transfer.DeliveryTripId = trip.Id;
                    transfer.DriverName = trip.DriverName;
                    transfer.DriverPhone = trip.DriverPhone;
                    transfer.LicensePlate = trip.LicensePlate;
                    transfer.DispatchedDate = DateTime.UtcNow;
                    transfer.Status = InventoryTransferStatus.InTransit;
                    transfer.UpdatedAt = DateTime.UtcNow;
                }
            }

            vehicle.Status = "OnTrip";
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTripByIdAsync(trip.Id) ?? _mapper.Map<DeliveryTripReadDto>(trip);
        }

        public async Task<DeliveryTripReadDto> DispatchSingleOrderInternalAsync(int orderId, DispatchInternalOrderDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

            if (order == null)
            {
                throw new InvalidOperationException("Không tìm thấy đơn hàng.");
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Đơn hàng đã hoàn tất hoặc đã bị hủy, không thể điều phối vận chuyển.");
            }

            var vehicle = await _context.DeliveryVehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId && !v.IsDeleted);
            if (vehicle == null)
            {
                throw new InvalidOperationException("Không tìm thấy phương tiện vận tải chỉ định.");
            }

            // Tạo nhanh 1 chuyến giao đơn lẻ cho xe này
            string todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            int countToday = await _context.DeliveryTrips.CountAsync(t => t.TripCode.StartsWith($"TRIP-{todayStr}"));
            string tripCode = $"TRIP-{todayStr}-{(countToday + 1):D3}";

            var trip = new DeliveryTrip
            {
                TripCode = tripCode,
                TripType = "B2C_Delivery",
                Status = "InTransit",
                VehicleId = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                DriverName = vehicle.DriverName ?? "Tài xế Solaris",
                DriverPhone = vehicle.DriverPhone ?? "0901234567",
                WarehouseId = order.WarehouseId ?? vehicle.HomeWarehouseId ?? 1,
                StartedAt = DateTime.UtcNow,
                Note = dto.Note ?? $"Chuyến giao nhanh đơn {order.OrderCode}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            trip.TripOrders.Add(new DeliveryTripOrder
            {
                OrderId = order.Id,
                DeliverySequence = 1,
                Status = "Pending"
            });

            _context.DeliveryTrips.Add(trip);

            // Cập nhật Order sang Shipping
            order.ShippingProvider = "Internal";
            order.TrackingCode = $"SLR-EXP-{order.OrderCode}";
            order.DeliveryTripId = trip.Id;
            order.DriverName = trip.DriverName;
            order.DriverPhone = trip.DriverPhone;
            order.LicensePlate = trip.LicensePlate;
            order.DispatchedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Shipping;
            order.UpdatedAt = DateTime.UtcNow;

            vehicle.Status = "OnTrip";
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTripByIdAsync(trip.Id) ?? _mapper.Map<DeliveryTripReadDto>(trip);
        }

        public async Task<DeliveryTripReadDto> StartTripAsync(int tripId)
        {
            var trip = await _context.DeliveryTrips.FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);
            if (trip == null) throw new InvalidOperationException("Không tìm thấy chuyến xe.");

            trip.Status = "InTransit";
            trip.StartedAt = DateTime.UtcNow;
            trip.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTripByIdAsync(tripId) ?? _mapper.Map<DeliveryTripReadDto>(trip);
        }

        public async Task<DeliveryTripReadDto> CompleteTripAsync(int tripId)
        {
            var trip = await _context.DeliveryTrips
                .Include(t => t.Vehicle)
                .Include(t => t.TripOrders)
                    .ThenInclude(to => to.Order)
                .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

            if (trip == null) throw new InvalidOperationException("Không tìm thấy chuyến xe.");

            trip.Status = "Completed";
            trip.CompletedAt = DateTime.UtcNow;
            trip.UpdatedAt = DateTime.UtcNow;

            // Giải phóng xe về trạng thái sẵn sàng
            if (trip.Vehicle != null)
            {
                trip.Vehicle.Status = "Available";
                trip.Vehicle.UpdatedAt = DateTime.UtcNow;
            }

            // Hoàn tất tất cả đơn hàng trong chuyến nếu chưa hoàn tất
            foreach (var to in trip.TripOrders)
            {
                if (to.Status != "Delivered")
                {
                    to.Status = "Delivered";
                    to.DeliveredAt = DateTime.UtcNow;
                }

                if (to.Order != null && to.Order.Status != OrderStatus.Completed)
                {
                    to.Order.Status = OrderStatus.Completed;
                    to.Order.DeliveredAt = DateTime.UtcNow;
                    to.Order.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return await GetTripByIdAsync(tripId) ?? _mapper.Map<DeliveryTripReadDto>(trip);
        }

        public async Task<DeliveryTripReadDto> MarkTripOrderDeliveredAsync(int tripId, int orderId, string? note)
        {
            var tripOrder = await _context.DeliveryTripOrders
                .Include(to => to.Order)
                .Include(to => to.Trip)
                    .ThenInclude(t => t!.Vehicle)
                .FirstOrDefaultAsync(to => to.TripId == tripId && to.OrderId == orderId);

            if (tripOrder == null) throw new InvalidOperationException("Không tìm thấy đơn hàng trong chuyến xe này.");

            tripOrder.Status = "Delivered";
            tripOrder.DeliveredAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(note)) tripOrder.Note = note;

            if (tripOrder.Order != null)
            {
                tripOrder.Order.Status = OrderStatus.Completed;
                tripOrder.Order.DeliveredAt = DateTime.UtcNow;
                tripOrder.Order.UpdatedAt = DateTime.UtcNow;
            }

            // Kiểm tra xem tất cả các đơn trong chuyến đã giao xong chưa
            var allOrders = await _context.DeliveryTripOrders.Where(to => to.TripId == tripId).ToListAsync();
            if (allOrders.All(o => o.Status == "Delivered"))
            {
                if (tripOrder.Trip != null)
                {
                    tripOrder.Trip.Status = "Completed";
                    tripOrder.Trip.CompletedAt = DateTime.UtcNow;
                    if (tripOrder.Trip.Vehicle != null)
                    {
                        tripOrder.Trip.Vehicle.Status = "Available";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return await GetTripByIdAsync(tripId) ?? new DeliveryTripReadDto();
        }

        public async Task<TransportationDashboardStatsDto> GetTransportationDashboardStatsAsync()
        {
            var vehicles = await _context.DeliveryVehicles.Where(v => !v.IsDeleted).ToListAsync();
            int availBikes = vehicles.Count(v => v.VehicleType == "Motorbike" && v.Status == "Available" && v.IsActive);
            int totalBikes = vehicles.Count(v => v.VehicleType == "Motorbike" && v.IsActive);
            int activeTrucks = vehicles.Count(v => v.VehicleType == "RefrigeratedTruck" && v.Status == "OnTrip" && v.IsActive);
            int totalTrucks = vehicles.Count(v => v.VehicleType == "RefrigeratedTruck" && v.IsActive);

            var activeTrips = await _context.DeliveryTrips
                .Include(t => t.Vehicle)
                .Include(t => t.Warehouse)
                .Include(t => t.TripOrders)
                .Where(t => !t.IsDeleted && (t.Status == "Preparing" || t.Status == "InTransit"))
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Đơn hàng đang đóng gói hoặc đã duyệt chờ gán xe
            var pendingOrders = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
                .Where(o => !o.IsDeleted && (o.Status == OrderStatus.Processing || o.Status == OrderStatus.Confirmed) && o.DeliveryTripId == null)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var pendingColdChainCount = pendingOrders.Count(o => o.Details.Any(d => d.Variant?.Product?.Category?.RequiresColdChain == true));

            var waitingList = pendingOrders.Take(15).Select(o => new OrderWaitingDispatchDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                ReceiverName = o.ReceiverName ?? "Khách hàng",
                ReceiverPhone = o.ReceiverPhone ?? string.Empty,
                DeliveryAddress = o.DeliveryAddress ?? string.Empty,
                TotalAmount = o.TotalAmount,
                RequiresColdChain = o.Details.Any(d => d.Variant?.Product?.Category?.RequiresColdChain == true),
                CreatedAt = o.CreatedAt
            }).ToList();

            return new TransportationDashboardStatsDto
            {
                AvailableBikes = availBikes,
                TotalBikes = totalBikes,
                ActiveTrucks = activeTrucks,
                TotalTrucks = totalTrucks,
                PendingColdChainOrders = pendingColdChainCount,
                ActiveTripsCount = activeTrips.Count,
                RecentActiveTrips = _mapper.Map<List<DeliveryTripReadDto>>(activeTrips),
                OrdersWaitingDispatch = waitingList
            };
        }
    }
}
