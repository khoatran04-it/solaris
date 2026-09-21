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
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var vehicle = await _context.DeliveryVehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId && !v.IsDeleted);
                if (vehicle == null)
                {
                    throw new InvalidOperationException("Không tìm thấy phương tiện giao hàng.");
                }

                if (!vehicle.IsActive)
                {
                    throw new InvalidOperationException("Phương tiện đang ngưng hoạt động, không thể điều phối chuyến.");
                }

                if (vehicle.Status == "Maintenance")
                {
                    throw new InvalidOperationException("Phương tiện đang trong trạng thái bảo dưỡng, không thể điều phối chuyến.");
                }

                if (vehicle.Status == "OnTrip")
                {
                    throw new InvalidOperationException("Phương tiện đang thực hiện chuyến xe khác, không thể điều phối chuyến mới.");
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
                if (dto.TripType == "B2C_Delivery")
                {
                    if (dto.OrderIds == null || !dto.OrderIds.Any())
                    {
                        throw new InvalidOperationException("Chuyến giao hàng B2C phải có ít nhất 1 đơn hàng.");
                    }

                    var orders = await _context.Orders
                        .Include(o => o.Details)
                            .ThenInclude(d => d.Variant)
                                .ThenInclude(v => v!.Product)
                                    .ThenInclude(p => p!.Category)
                        .Where(o => dto.OrderIds.Contains(o.Id) && !o.IsDeleted)
                        .ToListAsync();

                    if (orders.Count != dto.OrderIds.Count)
                    {
                        throw new InvalidOperationException("Một số đơn hàng chỉ định không tồn tại hoặc đã bị xóa.");
                    }

                    int seq = 1;
                    foreach (var order in orders)
                    {
                        if (order.DeliveryTripId.HasValue)
                        {
                            throw new InvalidOperationException($"Đơn hàng '{order.OrderCode}' đã được gán vào chuyến xe khác.");
                        }

                        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Completed)
                        {
                            throw new InvalidOperationException($"Đơn hàng '{order.OrderCode}' đang ở trạng thái '{order.Status}', không thể điều phối vận chuyển.");
                        }

                        bool requiresColdChain = order.Details.Any(d => d.Variant?.Product?.Category?.RequiresColdChain == true);
                        if (requiresColdChain && !vehicle.IsColdChainEquipped)
                        {
                            throw new InvalidOperationException($"Đơn hàng '{order.OrderCode}' yêu cầu bảo quản chuỗi lạnh nhưng phương tiện {vehicle.LicensePlate} không có trang bị thùng lạnh.");
                        }

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
                        order.Status = OrderStatus.Shipping;
                        order.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Gán chuyển kho B2B nếu có
                if (dto.TripType == "B2B_Transfer")
                {
                    if (!dto.InventoryTransferId.HasValue)
                    {
                        throw new InvalidOperationException("Chuyến chuyển kho B2B bắt buộc phải chỉ định phiếu chuyển kho liên kết.");
                    }

                    if (vehicle.VehicleType != "RefrigeratedTruck" || !vehicle.IsColdChainEquipped)
                    {
                        throw new InvalidOperationException("Vận chuyển điều chuyển kho B2B bắt buộc phải sử dụng Xe tải lạnh chuyên dụng (RefrigeratedTruck) có trang bị hệ thống làm lạnh.");
                    }

                    var transfer = await _context.InventoryTransfers
                        .Include(t => t.Details)
                        .FirstOrDefaultAsync(t => t.Id == dto.InventoryTransferId.Value && !t.IsDeleted);

                    if (transfer == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy phiếu chuyển kho chỉ định.");
                    }

                    if (transfer.DeliveryTripId.HasValue)
                    {
                        throw new InvalidOperationException($"Phiếu chuyển kho '{transfer.TransferCode}' đã được gán vào chuyến xe khác.");
                    }

                    if (transfer.Status != InventoryTransferStatus.Approved)
                    {
                        throw new InvalidOperationException($"Chỉ có thể điều phối chuyến xe cho phiếu chuyển kho đã được phê duyệt (Approved). Phiếu '{transfer.TransferCode}' hiện ở trạng thái '{transfer.Status}'.");
                    }

                    // Trừ tồn kho kho nguồn và ghi nhận sổ cái xuất kho (Dispatch)
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

                    transfer.DeliveryTripId = trip.Id;
                    transfer.DriverName = trip.DriverName;
                    transfer.DriverPhone = trip.DriverPhone;
                    transfer.LicensePlate = trip.LicensePlate;
                    transfer.DispatchedDate = DateTime.UtcNow;
                    transfer.Status = InventoryTransferStatus.InTransit;
                    transfer.UpdatedAt = DateTime.UtcNow;

                    // Đồng bộ trạng thái chuyến xe B2B sang InTransit ngay lập tức
                    trip.Status = "InTransit";
                    trip.StartedAt = DateTime.UtcNow;
                }

                // Gán thu hồi đơn trả RMA nếu có (TripType == B2C_Return)
                if (dto.TripType == "B2C_Return")
                {
                    if (dto.CustomerReturnIds == null || !dto.CustomerReturnIds.Any())
                    {
                        throw new InvalidOperationException("Chuyến thu hồi B2C phải có ít nhất 1 phiếu trả hàng.");
                    }

                    var returns = await _context.CustomerReturns
                        .Where(r => dto.CustomerReturnIds.Contains(r.Id) && !r.IsDeleted)
                        .ToListAsync();

                    if (returns.Count != dto.CustomerReturnIds.Count)
                    {
                        throw new InvalidOperationException("Một số phiếu trả hàng chỉ định không tồn tại hoặc đã bị xóa.");
                    }

                    foreach (var ret in returns)
                    {
                        if (ret.DeliveryTripId.HasValue)
                        {
                            throw new InvalidOperationException($"Phiếu trả hàng '{ret.ReturnCode}' đã được gán vào chuyến xe khác.");
                        }

                        if (ret.Status != CustomerReturnStatus.Approved)
                        {
                            throw new InvalidOperationException($"Chỉ có thể điều phối thu hồi cho phiếu trả hàng đã duyệt (Approved). Phiếu '{ret.ReturnCode}' hiện ở trạng thái '{ret.Status}'.");
                        }

                        ret.DeliveryTripId = trip.Id;
                        ret.Status = CustomerReturnStatus.PickingUp; // Đang thu hồi
                        ret.UpdatedAt = DateTime.UtcNow;
                    }
                }

                vehicle.Status = "OnTrip";
                vehicle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(trip.Id) ?? _mapper.Map<DeliveryTripReadDto>(trip);
            });
        }

        public async Task<DeliveryTripReadDto> DispatchSingleOrderInternalAsync(int orderId, DispatchInternalOrderDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var order = await _context.Orders
                    .Include(o => o.Details)
                        .ThenInclude(d => d.Variant)
                            .ThenInclude(v => v!.Product)
                                .ThenInclude(p => p!.Category)
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null)
                {
                    throw new InvalidOperationException("Không tìm thấy đơn hàng.");
                }

                if (order.DeliveryTripId.HasValue)
                {
                    throw new InvalidOperationException($"Đơn hàng '{order.OrderCode}' đã được gán vào chuyến xe khác.");
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

                if (!vehicle.IsActive)
                {
                    throw new InvalidOperationException("Phương tiện đang ngưng hoạt động, không thể điều phối chuyến.");
                }

                if (vehicle.Status == "Maintenance")
                {
                    throw new InvalidOperationException("Phương tiện đang trong trạng thái bảo dưỡng, không thể điều phối chuyến.");
                }

                if (vehicle.Status == "OnTrip")
                {
                    throw new InvalidOperationException("Phương tiện đang thực hiện chuyến xe khác, không thể điều phối chuyến mới.");
                }

                bool requiresColdChain = order.Details.Any(d => d.Variant?.Product?.Category?.RequiresColdChain == true);
                if (requiresColdChain && !vehicle.IsColdChainEquipped)
                {
                    throw new InvalidOperationException($"Đơn hàng '{order.OrderCode}' yêu cầu bảo quản chuỗi lạnh nhưng phương tiện {vehicle.LicensePlate} không có trang bị thùng lạnh.");
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
                order.DispatchedAt = trip.StartedAt;
                order.Status = OrderStatus.Shipping;
                order.UpdatedAt = DateTime.UtcNow;

                vehicle.Status = "OnTrip";
                vehicle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(trip.Id) ?? _mapper.Map<DeliveryTripReadDto>(trip);
            });
        }

        public async Task<DeliveryTripReadDto> StartTripAsync(int tripId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var trip = await _context.DeliveryTrips
                    .Include(t => t.TripOrders)
                        .ThenInclude(to => to.Order)
                    .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

                if (trip == null) throw new KeyNotFoundException("Không tìm thấy chuyến xe.");

                if (trip.Status != "Preparing")
                    throw new InvalidOperationException($"Chỉ có thể xuất bến chuyến xe đang ở trạng thái chuẩn bị (Preparing). Chuyến hiện tại đang ở trạng thái '{trip.Status}'.");

                trip.Status = "InTransit";
                trip.StartedAt = DateTime.UtcNow;
                trip.UpdatedAt = DateTime.UtcNow;

                foreach (var to in trip.TripOrders)
                {
                    if (to.Order != null)
                    {
                        to.Order.DispatchedAt = trip.StartedAt;
                        to.Order.Status = OrderStatus.Shipping;
                        to.Order.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(tripId) ?? _mapper.Map<DeliveryTripReadDto>(trip);
            });
        }

        public async Task<DeliveryTripReadDto> CompleteTripAsync(int tripId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var trip = await _context.DeliveryTrips
                    .Include(t => t.Vehicle)
                    .Include(t => t.TripOrders)
                        .ThenInclude(to => to.Order)
                    .Include(t => t.InventoryTransfer)
                    .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

                if (trip == null) throw new KeyNotFoundException("Không tìm thấy chuyến xe.");

                if (trip.Status == "Completed")
                    throw new InvalidOperationException("Chuyến xe đã được hoàn tất trước đó.");

                if (trip.Status == "Cancelled")
                    throw new InvalidOperationException("Không thể hoàn tất chuyến xe đã bị hủy.");

                // Xử lý theo từng loại chuyến xe
                if (trip.TripType == "B2B_Transfer")
                {
                    if (trip.InventoryTransferId.HasValue)
                    {
                        var transfer = await _context.InventoryTransfers
                            .FirstOrDefaultAsync(t => t.Id == trip.InventoryTransferId.Value && !t.IsDeleted);

                        if (transfer != null && transfer.Status != InventoryTransferStatus.Completed)
                        {
                            throw new InvalidOperationException($"Chuyến xe vận chuyển liên kho chỉ được hoàn tất khi Kho đích đã nghiệm thu nhận hàng (Phiếu {transfer.TransferCode} hiện ở trạng thái '{transfer.Status}').");
                        }
                    }
                }
                else if (trip.TripType == "B2C_Delivery")
                {
                    // RÀNG BUỘC KHO: Kiểm tra xem còn đơn hàng nào chưa hoàn tất (Pending) không
                    var pendingTripOrders = trip.TripOrders.Where(to => to.Status == "Pending").ToList();
                    if (pendingTripOrders.Any())
                    {
                        throw new InvalidOperationException($"Không thể hoàn tất chuyến xe khi vẫn còn {pendingTripOrders.Count} đơn hàng ở trạng thái Chờ giao (Pending). Vui lòng cập nhật kết quả giao hàng.");
                    }

                    foreach (var to in trip.TripOrders)
                    {
                        if (to.Status == "Delivered")
                        {
                            if (to.Order != null)
                            {
                                if (to.Order.Status != OrderStatus.Completed)
                                {
                                    to.Order.Status = OrderStatus.Completed;
                                    to.Order.DeliveredAt ??= DateTime.UtcNow;
                                }

                                // ĐỐI SOÁT TIỀN MẶT COD: Khi đơn giao thành công, khách đã trả tiền mặt cho shipper -> Cập nhật PaymentStatus = Paid
                                if (to.Order.PaymentMethod == PaymentMethod.COD && to.Order.PaymentStatus != PaymentStatus.Paid)
                                {
                                    to.Order.PaymentStatus = PaymentStatus.Paid;
                                }
                                to.Order.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        else if (to.Status == "Failed")
                        {
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
                                        Status = CustomerReturnStatus.Inspecting, // Hàng đã theo xe quay về kho, chuyển sang Inspecting chờ QC
                                        ReturnType = CustomerReturnType.DoorstepRefusal,
                                        ReturnDate = DateTime.UtcNow,
                                        Reason = to.FailureReason ?? to.Note ?? "Hàng hoàn về từ chuyến giao thất bại / khách từ chối nhận",
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow,
                                        IsDeleted = false
                                    };

                                    // TRUY VẾT CHÍNH XÁC TỪNG LÔ HÀNG THỰC TẾ ĐÃ XUẤT KHO (Tránh gộp sai lô)
                                    var issueDetails = await _context.InventoryIssueDetails
                                        .Include(iid => iid.InventoryIssue)
                                        .Where(iid => iid.InventoryIssue != null && iid.InventoryIssue.OrderId == to.OrderId && !iid.InventoryIssue.IsDeleted)
                                        .ToListAsync();

                                    if (issueDetails.Any())
                                    {
                                        foreach (var issueDetail in issueDetails)
                                        {
                                            autoRma.Details.Add(new CustomerReturnDetail
                                            {
                                                VariantId = issueDetail.VariantId,
                                                BatchId = issueDetail.BatchId,
                                                UoMId = issueDetail.UoMId,
                                                ReturnedQuantity = issueDetail.Quantity,
                                                UnitPrice = issueDetail.UnitPrice,
                                                AcceptedQuantity = 0,
                                                DamagedQuantity = 0,
                                                RefundAmount = 0
                                            });
                                        }
                                    }
                                    else
                                    {
                                        var orderWithDetails = await _context.Orders
                                            .Include(o => o.Details)
                                            .FirstOrDefaultAsync(o => o.Id == to.OrderId);

                                        if (orderWithDetails != null)
                                        {
                                            foreach (var detail in orderWithDetails.Details)
                                            {
                                                var anyBatch = await _context.ProductBatches
                                                    .Where(b => b.VariantId == detail.VariantId && !b.IsDeleted)
                                                    .OrderBy(b => b.ExpiryDate) // Quy tắc FEFO: Ưu tiên lô cận hạn sử dụng nhất
                                                    .FirstOrDefaultAsync();

                                                int batchId = anyBatch?.Id ?? 1;

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
                                    }

                                    _context.CustomerReturns.Add(autoRma);
                                }
                            }
                        }
                    }
                }
                else if (trip.TripType == "B2C_Return")
                {
                    // Chỉ chuyển sang Inspecting các phiếu còn liên kết với chuyến xe này (đã thu hồi thành công)
                    var returnIdsInTrip = await _context.CustomerReturns
                        .Where(r => r.DeliveryTripId == trip.Id && !r.IsDeleted)
                        .ToListAsync();

                    foreach (var ret in returnIdsInTrip)
                    {
                        // Hàng đã về tới kho an toàn -> Chuyển sang Inspecting (Chờ kiểm định QC)
                        ret.Status = CustomerReturnStatus.Inspecting;
                        ret.UpdatedAt = DateTime.UtcNow;
                    }
                }

                trip.Status = "Completed";
                trip.CompletedAt = DateTime.UtcNow;
                trip.UpdatedAt = DateTime.UtcNow;

                // Giải phóng xe về trạng thái sẵn sàng
                if (trip.Vehicle != null)
                {
                    trip.Vehicle.Status = "Available";
                    trip.Vehicle.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(tripId) ?? _mapper.Map<DeliveryTripReadDto>(trip);
            });
        }

        public async Task<DeliveryTripReadDto> CancelTripAsync(int tripId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy chuyến xe không được để trống.", nameof(reason));

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var trip = await _context.DeliveryTrips
                    .Include(t => t.Vehicle)
                    .Include(t => t.TripOrders)
                        .ThenInclude(to => to.Order)
                    .Include(t => t.InventoryTransfer)
                    .Include(t => t.CustomerReturns)
                    .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

                if (trip == null) throw new KeyNotFoundException("Không tìm thấy chuyến xe.");

                if (trip.Status == "Completed")
                    throw new InvalidOperationException("Không thể hủy chuyến xe đã hoàn tất.");

                if (trip.Status == "Cancelled")
                    throw new InvalidOperationException("Chuyến xe đã bị hủy trước đó.");

                trip.Status = "Cancelled";
                trip.Note = string.IsNullOrWhiteSpace(trip.Note)
                    ? $"Đã hủy chuyến: {reason.Trim()}"
                    : $"{trip.Note} | Đã hủy chuyến: {reason.Trim()}";
                trip.UpdatedAt = DateTime.UtcNow;

                // Giải phóng phương tiện
                if (trip.Vehicle != null)
                {
                    trip.Vehicle.Status = "Available";
                    trip.Vehicle.UpdatedAt = DateTime.UtcNow;
                }

                // 1. Hoàn trả các đơn hàng B2C
                foreach (var to in trip.TripOrders)
                {
                    to.Status = "Cancelled";
                    to.FailureReason = $"Hủy chuyến xe: {reason.Trim()}";

                    if (to.Order != null)
                    {
                        to.Order.DeliveryTripId = null;
                        to.Order.DriverName = null;
                        to.Order.DriverPhone = null;
                        to.Order.LicensePlate = null;
                        to.Order.DispatchedAt = null;

                        // Nếu chuyến bị hủy trong giai đoạn chuẩn bị, hoàn trả trạng thái về Confirmed
                        if (to.Order.Status == OrderStatus.Shipping)
                        {
                            to.Order.Status = OrderStatus.Confirmed;
                        }
                        to.Order.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // 2. Hoàn trả các phiếu trả hàng B2C_Return
                foreach (var ret in trip.CustomerReturns)
                {
                    ret.DeliveryTripId = null;
                    ret.Status = CustomerReturnStatus.Approved; // Hoàn về Approved để lên lịch xe chuyến khác
                    ret.InspectionNotes = string.IsNullOrWhiteSpace(ret.InspectionNotes)
                        ? $"Chuyến xe #{trip.TripCode} bị hủy: {reason.Trim()}"
                        : $"{ret.InspectionNotes} | Chuyến xe #{trip.TripCode} bị hủy: {reason.Trim()}";
                    ret.UpdatedAt = DateTime.UtcNow;
                }

                // 3. Xử lý Chuyển kho B2B
                if (trip.InventoryTransfer != null)
                {
                    trip.InventoryTransfer.DeliveryTripId = null;
                    trip.InventoryTransfer.DriverName = null;
                    trip.InventoryTransfer.DriverPhone = null;
                    trip.InventoryTransfer.LicensePlate = null;
                    trip.InventoryTransfer.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(tripId) ?? _mapper.Map<DeliveryTripReadDto>(trip);
            });
        }

        public async Task<DeliveryTripReadDto> MarkTripOrderDeliveredAsync(int tripId, int orderId, string? note)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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
                    if (tripOrder.Order.DispatchedAt == null && tripOrder.Trip?.StartedAt != null)
                    {
                        tripOrder.Order.DispatchedAt = tripOrder.Trip.StartedAt;
                    }

                    // ĐỐI SOÁT TIỀN MẶT COD: Khi tài xế giao thành công, khách trả tiền mặt -> Cập nhật PaymentStatus = Paid
                    if (tripOrder.Order.PaymentMethod == PaymentMethod.COD)
                    {
                        tripOrder.Order.PaymentStatus = PaymentStatus.Paid;
                    }
                    tripOrder.Order.UpdatedAt = DateTime.UtcNow;
                }

                // Kiểm tra xem tất cả các đơn trong chuyến đã giao xong (Delivered hoặc Failed) chưa
                var allOrders = await _context.DeliveryTripOrders.Where(to => to.TripId == tripId).ToListAsync();
                if (allOrders.All(o => o.Status == "Delivered" || o.Status == "Failed"))
                {
                    if (tripOrder.Trip != null && tripOrder.Trip.Status != "Completed")
                    {
                        // Chuyển sang Returning (Chờ xe quay về kho và chốt COD/thùng lạnh)
                        tripOrder.Trip.Status = "Returning";
                        tripOrder.Trip.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(tripId) ?? new DeliveryTripReadDto();
            });
        }

        public async Task<DeliveryTripReadDto> MarkTripOrderFailedAsync(int tripId, int orderId, string reason)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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
            });
        }

        public async Task<DeliveryTripReadDto> MarkTripReturnPickedUpAsync(int tripId, int returnId, string? note)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var ret = await _context.CustomerReturns
                    .Include(r => r.DeliveryTrip)
                    .FirstOrDefaultAsync(r => r.Id == returnId && r.DeliveryTripId == tripId && !r.IsDeleted);

                if (ret == null)
                    throw new InvalidOperationException("Không tìm thấy phiếu trả hàng trong chuyến xe này.");

                if (ret.Status != CustomerReturnStatus.PickingUp)
                    throw new InvalidOperationException($"Phiếu trả hàng đang ở trạng thái '{ret.Status}', không thể đánh dấu đã thu hồi.");

                if (!string.IsNullOrWhiteSpace(note))
                {
                    ret.InspectionNotes = string.IsNullOrWhiteSpace(ret.InspectionNotes)
                        ? $"Đã thu hồi: {note.Trim()}"
                        : $"{ret.InspectionNotes} | Đã thu hồi: {note.Trim()}";
                }
                ret.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(tripId) ?? new DeliveryTripReadDto();
            });
        }

        public async Task<DeliveryTripReadDto> MarkTripReturnFailedAsync(int tripId, int returnId, string reason)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var ret = await _context.CustomerReturns
                    .Include(r => r.DeliveryTrip)
                    .FirstOrDefaultAsync(r => r.Id == returnId && r.DeliveryTripId == tripId && !r.IsDeleted);

                if (ret == null)
                    throw new InvalidOperationException("Không tìm thấy phiếu trả hàng trong chuyến xe này.");

                // Thu hồi thất bại: gỡ chuyến xe, hoàn trả trạng thái Approved để lên lịch lại
                ret.DeliveryTripId = null;
                ret.Status = CustomerReturnStatus.Approved;
                ret.InspectionNotes = string.IsNullOrWhiteSpace(ret.InspectionNotes)
                    ? $"Thu hồi không thành công: {reason.Trim()}"
                    : $"{ret.InspectionNotes} | Thu hồi không thành công: {reason.Trim()}";
                ret.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return await GetTripByIdAsync(tripId) ?? new DeliveryTripReadDto();
            });
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

            // 2. Các phiếu chuyển kho liên chi nhánh đã duyệt chờ gán xe tải (B2B)
            var pendingTransfers = await _context.InventoryTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.CreatedBy)
                .Include(t => t.Details)
                .Where(t => !t.IsDeleted && t.Status == InventoryTransferStatus.Approved && t.DeliveryTripId == null)
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
