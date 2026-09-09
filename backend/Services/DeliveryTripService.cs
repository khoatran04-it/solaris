using AutoMapper;
using backend.Data;
using backend.DTOs.VehicleDTOs;
using backend.Helpers;
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
        private readonly IUoMConversionService _uomConversionService;

        public DeliveryTripService(SolarisDbContext context, IMapper mapper, IUoMConversionService? uomConversionService = null)
        {
            _context = context;
            _mapper = mapper;
            _uomConversionService = uomConversionService ?? new UoMConversionService(context, mapper);
        }

        public async Task<List<DeliveryTripReadDto>> GetAllTripsAsync(string? status = null, string? tripType = null)
        {
            var query = _context.DeliveryTrips
                .Include(t => t.Vehicle)
                .Include(t => t.Warehouse)
                .Include(t => t.InventoryTransfer)
                    .ThenInclude(it => it!.FromWarehouse)
                .Include(t => t.InventoryTransfer)
                    .ThenInclude(it => it!.ToWarehouse)
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
                    .ThenInclude(it => it!.FromWarehouse)
                .Include(t => t.InventoryTransfer)
                    .ThenInclude(it => it!.ToWarehouse)
                .Include(t => t.TripOrders)
                    .ThenInclude(to => to.Order)
                .Include(t => t.CustomerReturns)
                    .ThenInclude(cr => cr.Order)
                .Include(t => t.CustomerReturns)
                    .ThenInclude(cr => cr.Customer)
                .Include(t => t.CustomerReturns)
                    .ThenInclude(cr => cr.Details)
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
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == dto.InventoryTransferId.Value);

                if (transfer != null)
                {
                    // Nếu phiếu đang ở trạng thái Draft, thực hiện trừ tồn kho kho nguồn và ghi nhận sổ cái (Dispatch)
                    if (transfer.Status == InventoryTransferStatus.Draft)
                    {
                        foreach (var detail in transfer.Details)
                        {
                            decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.Quantity);

                            var sourceInv = await _context.WarehouseInventories
                                .FirstOrDefaultAsync(i => i.WarehouseId == transfer.FromWarehouseId &&
                                                          i.VariantId == detail.VariantId &&
                                                          i.BatchId == detail.BatchId);

                            if (sourceInv == null || sourceInv.QuantityAvailable < baseQty)
                                throw new InvalidOperationException($"Kho nguồn không đủ số lượng khả dụng cho mặt hàng mã #{detail.VariantId}, lô #{detail.BatchId} (Hiện có: {sourceInv?.QuantityAvailable ?? 0}, Cần xuất: {baseQty} theo ĐVT cơ sở).");

                            sourceInv.QuantityAvailable -= baseQty;
                            sourceInv.UpdatedAt = DateTime.UtcNow;

                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                                WarehouseId = transfer.FromWarehouseId,
                                VariantId = detail.VariantId,
                                BatchId = detail.BatchId,
                                Type = TransactionType.TransferOut,
                                Quantity = baseQty,
                                ReferenceCode = transfer.TransferCode,
                                Note = $"Xuất chuyển kho sang kho #{transfer.ToWarehouseId} (Chuyến xe {trip.TripCode}, Xe {vehicle.LicensePlate})",
                                CreatedById = 1,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    transfer.DeliveryTripId = trip.Id;
                    transfer.DriverName = trip.DriverName;
                    transfer.DriverPhone = trip.DriverPhone;
                    transfer.LicensePlate = trip.LicensePlate;
                    transfer.DispatchedDate = DateTime.UtcNow;
                    transfer.Status = InventoryTransferStatus.InTransit;
                    transfer.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Gán thu hồi đơn trả RMA nếu có (TripType == B2C_Return)
            if (dto.TripType == "B2C_Return" && dto.CustomerReturnIds != null && dto.CustomerReturnIds.Any())
            {
                var returns = await _context.CustomerReturns
                    .Where(r => dto.CustomerReturnIds.Contains(r.Id) && !r.IsDeleted)
                    .ToListAsync();

                foreach (var ret in returns)
                {
                    ret.DeliveryTripId = trip.Id;
                    ret.Status = CustomerReturnStatus.PickingUp; // Đang thu hồi
                    ret.UpdatedAt = DateTime.UtcNow;
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

            // Xử lý các đơn hàng trong chuyến B2C_Delivery
            foreach (var to in trip.TripOrders)
            {
                if (to.Status == "Delivered")
                {
                    if (to.Order != null && to.Order.Status != OrderStatus.Completed)
                    {
                        to.Order.Status = OrderStatus.Completed;
                        to.Order.DeliveredAt ??= DateTime.UtcNow;
                        to.Order.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (to.Status == "Failed")
                {
                    // BẢO TOÀN TRẠNG THÁI ĐÃ HỦY CHO ĐƠN BỊ TỪ CHỐI
                    if (to.Order != null)
                    {
                        to.Order.Status = OrderStatus.Cancelled;
                        to.Order.CancellationReason = to.FailureReason ?? to.Note ?? "Khách từ chối nhận hàng tại thời điểm giao";
                        to.Order.UpdatedAt = DateTime.UtcNow;

                        // TỰ ĐỘNG SINH PHIẾU RMA NẾU CHƯA CÓ
                        bool hasRma = await _context.CustomerReturns.AnyAsync(r => r.OrderId == to.OrderId && !r.IsDeleted);
                        if (!hasRma)
                        {
                            var autoRma = new CustomerReturn
                            {
                                ReturnCode = $"RET-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                                OrderId = to.OrderId,
                                CustomerId = to.Order.CustomerId,
                                WarehouseId = to.Order.WarehouseId ?? trip.WarehouseId,
                                ReceivedById = 1,
                                Status = CustomerReturnStatus.Pending,
                                ReturnType = CustomerReturnType.DoorstepRefusal,
                                ReturnDate = DateTime.UtcNow,
                                Reason = to.FailureReason ?? to.Note ?? "Hàng hoàn về từ chuyến giao thất bại / khách từ chối nhận",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsDeleted = false
                            };

                            var orderWithDetails = await _context.Orders
                                .Include(o => o.Details)
                                .FirstOrDefaultAsync(o => o.Id == to.OrderId);

                            if (orderWithDetails != null)
                            {
                                foreach (var detail in orderWithDetails.Details)
                                {
                                    int batchId = 0;
                                    var issueDetail = await _context.InventoryIssueDetails
                                        .Include(iid => iid.InventoryIssue)
                                        .FirstOrDefaultAsync(iid => iid.InventoryIssue != null && iid.InventoryIssue.OrderId == to.OrderId && iid.VariantId == detail.VariantId && !iid.InventoryIssue.IsDeleted);

                                    if (issueDetail != null && issueDetail.BatchId > 0)
                                    {
                                        batchId = issueDetail.BatchId;
                                    }
                                    else
                                    {
                                        var anyBatch = await _context.ProductBatches
                                            .Where(b => b.VariantId == detail.VariantId && !b.IsDeleted)
                                            .OrderByDescending(b => b.Id)
                                            .FirstOrDefaultAsync();
                                        batchId = anyBatch?.Id ?? 0;
                                    }

                                    if (batchId == 0)
                                    {
                                        var fallbackBatch = await _context.ProductBatches.FirstOrDefaultAsync(b => !b.IsDeleted);
                                        batchId = fallbackBatch?.Id ?? 1;
                                    }

                                    autoRma.Details.Add(new CustomerReturnDetail
                                    {
                                        VariantId = detail.VariantId,
                                        BatchId = batchId,
                                        UoMId = detail.UoMId,
                                        ReturnedQuantity = detail.Quantity,
                                        UnitPrice = detail.UnitPrice,
                                        AcceptedQuantity = 0,
                                        DamagedQuantity = 0,
                                        RefundAmount = 0
                                    });
                                }
                            }

                            _context.CustomerReturns.Add(autoRma);
                        }
                    }
                }
            }

            // Xử lý các phiếu trả hàng thu hồi trong chuyến (B2C_Return)
            var returnIdsInTrip = await _context.CustomerReturns
                .Where(r => r.DeliveryTripId == trip.Id && !r.IsDeleted)
                .ToListAsync();

            foreach (var ret in returnIdsInTrip)
            {
                // Hàng đã về tới kho an toàn -> Chuyển sang Inspecting (Chờ kiểm định QC)
                ret.Status = CustomerReturnStatus.Inspecting;
                ret.UpdatedAt = DateTime.UtcNow;
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

            // Kiểm tra xem tất cả các đơn trong chuyến đã giao xong (Delivered hoặc Failed) chưa
            var allOrders = await _context.DeliveryTripOrders.Where(to => to.TripId == tripId).ToListAsync();
            if (allOrders.All(o => o.Status == "Delivered" || o.Status == "Failed"))
            {
                if (tripOrder.Trip != null && tripOrder.Trip.Status != "Completed")
                {
                    // Chuyển sang Returning (Chờ xe quay về kho và chốt COD/thùng lạnh)
                    // Xe vẫn ở trạng thái OnTrip cho đến khi Quản lý kho bấm Xác nhận xe đã về kho!
                    tripOrder.Trip.Status = "Returning";
                    tripOrder.Trip.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return await GetTripByIdAsync(tripId) ?? new DeliveryTripReadDto();
        }

        public async Task<DeliveryTripReadDto> MarkTripOrderFailedAsync(int tripId, int orderId, string reason)
        {
            var tripOrder = await _context.DeliveryTripOrders
                .Include(to => to.Order)
                .Include(to => to.Trip)
                    .ThenInclude(t => t!.Vehicle)
                .FirstOrDefaultAsync(to => to.TripId == tripId && to.OrderId == orderId);

            if (tripOrder == null) throw new InvalidOperationException("Không tìm thấy đơn hàng trong chuyến xe này.");

            tripOrder.Status = "Failed";
            tripOrder.FailureReason = reason;

            if (tripOrder.Order != null)
            {
                tripOrder.Order.Status = OrderStatus.Cancelled;
                tripOrder.Order.CancellationReason = $"Khách từ chối nhận: {reason}";
                tripOrder.Order.UpdatedAt = DateTime.UtcNow;
            }

            // Kiểm tra xem tất cả các đơn trong chuyến đã giao xong (Delivered hoặc Failed) chưa
            var allOrders = await _context.DeliveryTripOrders.Where(to => to.TripId == tripId).ToListAsync();
            if (allOrders.All(o => o.Status == "Delivered" || o.Status == "Failed"))
            {
                if (tripOrder.Trip != null && tripOrder.Trip.Status != "Completed")
                {
                    tripOrder.Trip.Status = "Returning";
                    tripOrder.Trip.UpdatedAt = DateTime.UtcNow;
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
                .Include(t => t.InventoryTransfer)
                    .ThenInclude(it => it!.FromWarehouse)
                .Include(t => t.InventoryTransfer)
                    .ThenInclude(it => it!.ToWarehouse)
                .Include(t => t.TripOrders)
                    .ThenInclude(to => to.Order)
                .Include(t => t.CustomerReturns)
                    .ThenInclude(cr => cr.Order)
                .Include(t => t.CustomerReturns)
                    .ThenInclude(cr => cr.Customer)
                .Include(t => t.CustomerReturns)
                    .ThenInclude(cr => cr.Details)
                .Where(t => !t.IsDeleted && (t.Status == "Preparing" || t.Status == "InTransit" || t.Status == "Returning"))
                .OrderByDescending(t => t.CreatedAt)
                .Take(15)
                .ToListAsync();

            // 1. Đơn hàng B2C chờ gán xe:
            // RÀNG BUỘC KHO: CHỈ lấy các đơn hàng ĐÃ HOÀN TẤT PHIẾU XUẤT KHO!
            var pendingOrders = await _context.Orders
                .Include(o => o.Warehouse)
                .Include(o => o.CustomerAddress)
                .Include(o => o.InventoryIssues)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
                .Where(o => !o.IsDeleted &&
                            o.DeliveryTripId == null &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Completed &&
                            !(o.ShippingProvider == "GHN" && !string.IsNullOrEmpty(o.TrackingCode)) &&
                            (o.InventoryIssues.Any(i => !i.IsDeleted && i.Status == InventoryIssueStatus.Completed) ||
                             (o.Details.Any() && o.Details.All(d => d.IssuedQuantity >= d.Quantity))))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var pendingColdChainCount = pendingOrders.Count(o => o.Details.Any(d => d.Variant?.Product?.Category?.RequiresColdChain == true));

            var waitingOrdersList = pendingOrders.Take(50).Select(o =>
            {
                string province = o.CustomerAddress?.Province ?? string.Empty;
                string district = o.CustomerAddress?.District ?? string.Empty;
                string ward = o.CustomerAddress?.Ward ?? string.Empty;

                if (string.IsNullOrEmpty(province) && !string.IsNullOrEmpty(o.DeliveryAddress))
                {
                    var parts = o.DeliveryAddress.Split(',', StringSplitOptions.TrimEntries);
                    if (parts.Length >= 2)
                    {
                        province = parts[^1];
                        district = parts[^2];
                    }
                }

                return new OrderWaitingDispatchDto
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    ReceiverName = o.ReceiverName ?? "Khách hàng",
                    ReceiverPhone = o.ReceiverPhone ?? string.Empty,
                    DeliveryAddress = o.DeliveryAddress ?? string.Empty,
                    TotalAmount = o.TotalAmount,
                    RequiresColdChain = o.Details.Any(d => d.Variant?.Product?.Category?.RequiresColdChain == true),
                    WarehouseId = o.WarehouseId,
                    WarehouseName = o.Warehouse?.Name ?? (o.WarehouseId.HasValue ? $"Kho #{o.WarehouseId}" : "Chưa gán"),
                    Province = province,
                    District = district,
                    Ward = ward,
                    HasCompletedIssue = true,
                    CreatedAt = o.CreatedAt
                };
            }).ToList();

            // 2. Các phiếu chuyển kho liên chi nhánh đang ở trạng thái Nháp chờ gán xe tải (B2B)
            var pendingTransfers = await _context.InventoryTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.CreatedBy)
                .Include(t => t.Details)
                .Where(t => !t.IsDeleted && t.Status == InventoryTransferStatus.Draft && t.DeliveryTripId == null)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();

            var waitingTransfersList = pendingTransfers.Select(t => new TransferWaitingDispatchDto
            {
                TransferId = t.Id,
                TransferCode = t.TransferCode,
                FromWarehouseId = t.FromWarehouseId,
                FromWarehouseName = t.FromWarehouse?.Name ?? $"Kho #{t.FromWarehouseId}",
                ToWarehouseId = t.ToWarehouseId,
                ToWarehouseName = t.ToWarehouse?.Name ?? $"Kho #{t.ToWarehouseId}",
                TotalItems = t.Details.Count,
                CreatedByName = t.CreatedBy?.FullName ?? "Quản trị viên",
                CreatedAt = t.CreatedAt,
                Note = t.Note
            }).ToList();

            // 3. Các phiếu trả hàng (RMA) đã duyệt chờ gán xe thu hồi (Tab 3)
            var pendingReturns = await _context.CustomerReturns
                .Include(r => r.Order)
                .Include(r => r.Customer)
                .Include(r => r.Warehouse)
                .Include(r => r.Details)
                .Where(r => !r.IsDeleted &&
                            r.Status == CustomerReturnStatus.Approved &&
                            r.DeliveryTripId == null &&
                            r.ReturnType == CustomerReturnType.PostDeliveryReturn)
                .OrderByDescending(r => r.CreatedAt)
                .Take(30)
                .ToListAsync();

            var waitingReturnsList = pendingReturns.Select(r => new ReturnWaitingDispatchDto
            {
                ReturnId = r.Id,
                ReturnCode = r.ReturnCode,
                OrderId = r.OrderId,
                OrderCode = r.Order?.OrderCode ?? string.Empty,
                CustomerName = r.Customer?.Name ?? "Khách hàng",
                CustomerPhone = r.Customer?.PhoneNumber ?? string.Empty,
                PickupAddress = r.Order?.DeliveryAddress ?? "Địa chỉ khách hàng",
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? $"Kho #{r.WarehouseId}",
                Reason = r.Reason ?? "Yêu cầu đổi trả",
                TotalRefundEstimated = r.RefundAmount > 0 ? r.RefundAmount : r.Details.Sum(d => d.ReturnedQuantity * d.UnitPrice),
                TotalItems = r.Details.Count,
                ReturnDate = r.ReturnDate
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
                OrdersWaitingDispatch = waitingOrdersList,
                TransfersWaitingDispatch = waitingTransfersList,
                ReturnsWaitingDispatch = waitingReturnsList
            };
        }
    }
}
