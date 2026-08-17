using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.OrderDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class OrderService : IOrderService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOrderRoutingService _routingService;

        public OrderService(SolarisDbContext context, IMapper mapper, IOrderRoutingService routingService)
        {
            _context = context;
            _mapper = mapper;
            _routingService = routingService;
        }

        public async Task<PagedResult<OrderReadDto>> GetPagedAsync(
            string? search,
            int? customerId,
            int? warehouseId,
            int? status,
            int? paymentStatus,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Warehouse)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(o => o.WarehouseId == null || allowedWarehouseIds.Contains(o.WarehouseId.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(s) ||
                                         (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(s)) ||
                                         (o.ReceiverPhone != null && o.ReceiverPhone.Contains(s)));
            }

            if (customerId.HasValue) query = query.Where(o => o.CustomerId == customerId.Value);
            if (warehouseId.HasValue) query = query.Where(o => o.WarehouseId == warehouseId.Value);
            if (status.HasValue && Enum.IsDefined(typeof(OrderStatus), status.Value)) query = query.Where(o => o.Status == (OrderStatus)status.Value);
            if (paymentStatus.HasValue && Enum.IsDefined(typeof(PaymentStatus), paymentStatus.Value)) query = query.Where(o => o.PaymentStatus == (PaymentStatus)paymentStatus.Value);

            if (startDate.HasValue) query = query.Where(o => o.OrderDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(o => o.OrderDate < endDate.Value.Date.AddDays(1));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<OrderReadDto>
            {
                Items = _mapper.Map<IEnumerable<OrderReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<OrderReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.CustomerAddress)
                .Include(o => o.Warehouse)
                .Include(o => o.Details).ThenInclude(d => d.Variant)
                .Include(o => o.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(o => o.WarehouseId == null || allowedWarehouseIds.Contains(o.WarehouseId.Value));
            }

            var entity = await query.FirstOrDefaultAsync(o => o.Id == id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Đơn hàng.");

            return _mapper.Map<OrderReadDto>(entity);
        }

        public async Task<int> CreateAsync(OrderCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Đơn hàng phải có ít nhất 1 dòng sản phẩm.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Định tuyến kho thông minh nếu chưa chỉ định kho
                int assignedWarehouseId = dto.WarehouseId ?? 0;
                if (assignedWarehouseId == 0)
                {
                    var routing = await _routingService.DetermineOptimalWarehouseAsync(dto.CustomerAddressId, dto.Details);
                    assignedWarehouseId = routing.OptimalWarehouseId;
                }

                // 2. Lấy thông tin người nhận nếu có Address
                string? recName = dto.ReceiverName;
                string? recPhone = dto.ReceiverPhone;
                string? delAddress = dto.DeliveryAddress;

                if (dto.CustomerAddressId.HasValue && string.IsNullOrWhiteSpace(delAddress))
                {
                    var addr = await _context.CustomerAddresses.FindAsync(dto.CustomerAddressId.Value);
                    if (addr != null)
                    {
                        recName ??= addr.ReceiverName;
                        recPhone ??= addr.Phone;
                        delAddress ??= addr.FullAddress;
                    }
                }

                // 3. Khởi tạo Entity Đơn hàng
                var order = new Order
                {
                    OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                    CustomerId = dto.CustomerId,
                    CustomerAddressId = dto.CustomerAddressId,
                    ReceiverName = recName,
                    ReceiverPhone = recPhone,
                    DeliveryAddress = delAddress,
                    WarehouseId = assignedWarehouseId,
                    Status = OrderStatus.Confirmed,
                    PaymentStatus = PaymentStatus.Unpaid,
                    PaymentMethod = dto.PaymentMethod,
                    ShippingFee = dto.ShippingFee,
                    Note = dto.Note,
                    OrderDate = DateTime.UtcNow
                };

                decimal subTotal = 0;

                // 4. Xử lý từng dòng chi tiết & Khóa tồn kho (Reserve)
                foreach (var item in dto.Details)
                {
                    decimal unitPrice = item.UnitPrice ?? 0;
                    if (unitPrice <= 0)
                    {
                        // Lấy giá niêm yết từ VariantPrice theo UoM
                        var vp = await _context.ProductVariantPrices
                            .FirstOrDefaultAsync(p => p.VariantId == item.VariantId && p.UoMId == item.UoMId);
                        unitPrice = vp?.Price ?? 0;
                    }

                    decimal lineTotal = (item.Quantity * unitPrice) - item.DiscountAmount;
                    subTotal += lineTotal;

                    var detail = new OrderDetail
                    {
                        VariantId = item.VariantId,
                        UoMId = item.UoMId,
                        Quantity = item.Quantity,
                        BaseQuantity = item.Quantity, // TODO: nếu có quy đổi UoM, nhân tỷ lệ quy đổi
                        UnitPrice = unitPrice,
                        DiscountAmount = item.DiscountAmount,
                        TotalPrice = lineTotal,
                        IssuedQuantity = 0
                    };
                    order.Details.Add(detail);

                    // 5. GIỮ CHỖ TỒN KHO (Tăng QuantityReserved, Giảm QuantityAvailable)
                    var inventories = await _context.WarehouseInventories
                        .Where(i => i.WarehouseId == assignedWarehouseId && i.VariantId == item.VariantId && i.QuantityAvailable > 0)
                        .OrderBy(i => i.Batch != null ? i.Batch.ExpiryDate : DateTime.MaxValue)
                        .ToListAsync();

                    decimal remainingToReserve = item.Quantity;
                    foreach (var inv in inventories)
                    {
                        if (remainingToReserve <= 0) break;
                        var reserveAmount = Math.Min(inv.QuantityAvailable, remainingToReserve);
                        inv.QuantityAvailable -= reserveAmount;
                        inv.QuantityReserved += reserveAmount;
                        remainingToReserve -= reserveAmount;

                        // Ghi sổ cái Reserve
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                            WarehouseId = assignedWarehouseId,
                            VariantId = item.VariantId,
                            BatchId = inv.BatchId,
                            Type = TransactionType.Reserve,
                            Quantity = reserveAmount,
                            ReferenceCode = order.OrderCode,
                            Note = $"Giữ chỗ đơn hàng {order.OrderCode}",
                            CreatedById = 1
                        });
                    }
                }

                order.SubTotal = subTotal;
                order.TotalAmount = subTotal + order.ShippingFee;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, OrderUpdateDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) throw new KeyNotFoundException("Không tìm thấy Đơn hàng.");

            if (dto.Status.HasValue) order.Status = dto.Status.Value;
            if (dto.PaymentStatus.HasValue) order.PaymentStatus = dto.PaymentStatus.Value;
            if (dto.WarehouseId.HasValue) order.WarehouseId = dto.WarehouseId.Value;
            if (dto.Note != null) order.Note = dto.Note;
            if (dto.CancellationReason != null) order.CancellationReason = dto.CancellationReason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Details)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) throw new KeyNotFoundException("Không tìm thấy Đơn hàng.");
                if (order.Status == OrderStatus.Shipping || order.Status == OrderStatus.Completed)
                    throw new InvalidOperationException("Không thể hủy đơn hàng đã xuất kho hoặc đã hoàn tất.");

                // Hoàn trả tồn kho (Unreserve) nếu đơn hàng đã từng giữ chỗ
                if (order.WarehouseId.HasValue)
                {
                    foreach (var item in order.Details)
                    {
                        var unissuedQty = item.Quantity - item.IssuedQuantity;
                        if (unissuedQty <= 0) continue;

                        var inventories = await _context.WarehouseInventories
                            .Where(i => i.WarehouseId == order.WarehouseId.Value && i.VariantId == item.VariantId && i.QuantityReserved > 0)
                            .ToListAsync();

                        decimal remainingToRelease = unissuedQty;
                        foreach (var inv in inventories)
                        {
                            if (remainingToRelease <= 0) break;
                            var releaseAmount = Math.Min(inv.QuantityReserved, remainingToRelease);
                            inv.QuantityReserved -= releaseAmount;
                            inv.QuantityAvailable += releaseAmount;
                            remainingToRelease -= releaseAmount;

                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                                WarehouseId = order.WarehouseId.Value,
                                VariantId = item.VariantId,
                                BatchId = inv.BatchId,
                                Type = TransactionType.Unreserve,
                                Quantity = releaseAmount,
                                ReferenceCode = order.OrderCode,
                                Note = $"Hủy giữ chỗ đơn hàng {order.OrderCode}",
                                CreatedById = 1
                            });
                        }
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = reason;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
