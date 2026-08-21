using backend.Data;
using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ShopOrderService : IShopOrderService
    {
        private readonly SolarisDbContext _context;
        private readonly IOrderRoutingService _routingService;

        public ShopOrderService(SolarisDbContext context, IOrderRoutingService routingService)
        {
            _context = context;
            _routingService = routingService;
        }

        public async Task<ShopOrderReadDto> CheckoutAsync(int customerId, ShopCheckoutRequestDto request)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerTier)
                .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.PromotionVariants)
                            .ThenInclude(pv => pv.PromotionCampaign)
                .Include(c => c.Items)
                    .ThenInclude(i => i.UoM)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("Giỏ hàng đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.");

            // 1. Xác định thông tin người nhận & Địa chỉ giao hàng
            string receiverName = request.ReceiverName?.Trim() ?? customer.Name;
            string receiverPhone = request.ReceiverPhone?.Trim() ?? customer.PhoneNumber;
            string deliveryAddress = string.Empty;
            double destLat = request.Latitude;
            double destLng = request.Longitude;
            int? customerAddressId = request.CustomerAddressId;

            if (customerAddressId.HasValue)
            {
                var addr = customer.Addresses.FirstOrDefault(a => a.Id == customerAddressId.Value);
                if (addr != null)
                {
                    receiverName = addr.ReceiverName;
                    receiverPhone = addr.Phone;
                    deliveryAddress = addr.FullAddress;
                    destLat = addr.Latitude;
                    destLng = addr.Longitude;
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.Province) && !string.IsNullOrWhiteSpace(request.StreetAddress))
            {
                deliveryAddress = $"{request.StreetAddress.Trim()}, {request.Ward?.Trim()}, {request.District?.Trim()}, {request.Province.Trim()}";
            }
            else
            {
                var defaultAddr = customer.Addresses.FirstOrDefault(a => a.IsDefault) ?? customer.Addresses.FirstOrDefault();
                if (defaultAddr != null)
                {
                    receiverName = defaultAddr.ReceiverName;
                    receiverPhone = defaultAddr.Phone;
                    deliveryAddress = defaultAddr.FullAddress;
                    destLat = defaultAddr.Latitude;
                    destLng = defaultAddr.Longitude;
                    customerAddressId = defaultAddr.Id;
                }
                else
                {
                    throw new InvalidOperationException("Vui lòng cung cấp địa chỉ giao hàng.");
                }
            }

            // 2. Chạy trong Database Transaction để khóa giữ chỗ tồn kho an toàn (ACID)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // Chọn kho tối ưu theo GPS hoặc kho mặc định
                var warehouses = await _context.Warehouses
                    .Include(w => w.Address)
                    .Where(w => w.IsActive && !w.IsDeleted)
                    .ToListAsync();

                if (!warehouses.Any())
                    throw new InvalidOperationException("Hệ thống chưa thiết lập kho hàng đang hoạt động.");

                var primaryWarehouse = warehouses.FirstOrDefault(w => w.WarehouseType == "Kho tổng") ?? warehouses.First();
                int selectedWarehouseId = primaryWarehouse.Id;

                // 3. Chuẩn bị chi tiết đơn hàng & Tính giá
                var orderDetails = new List<OrderDetail>();
                decimal subTotal = 0;
                decimal totalDiscount = 0;

                foreach (var item in cart.Items)
                {
                    if (item.Variant == null) continue;

                    // Kiểm tra tồn kho khả dụng tại kho được chọn
                    var inventory = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(wi => wi.WarehouseId == selectedWarehouseId && wi.VariantId == item.VariantId);

                    // Nếu kho chính không có thì tìm kho có hàng
                    if (inventory == null || inventory.QuantityAvailable < item.Quantity)
                    {
                        var anyStockInv = await _context.WarehouseInventories
                            .Where(wi => wi.VariantId == item.VariantId && wi.QuantityAvailable >= item.Quantity)
                            .FirstOrDefaultAsync();

                        if (anyStockInv != null)
                        {
                            selectedWarehouseId = anyStockInv.WarehouseId;
                            inventory = anyStockInv;
                        }
                    }

                    if (inventory == null || inventory.QuantityAvailable < item.Quantity)
                    {
                        decimal currentAvail = inventory?.QuantityAvailable ?? 0;
                        throw new InvalidOperationException($"Sản phẩm '{item.Variant.Name}' không đủ tồn kho khả dụng (Yêu cầu: {item.Quantity}, Còn: {currentAvail}).");
                    }

                    // Khóa giữ chỗ tồn kho
                    inventory.QuantityAvailable -= item.Quantity;
                    inventory.QuantityReserved += item.Quantity;
                    inventory.UpdatedAt = now;

                    // Ghi sổ cái bất biến Reserve
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TX-{now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpperInvariant(),
                        Type = TransactionType.Reserve,
                        WarehouseId = selectedWarehouseId,
                        VariantId = item.VariantId,
                        BatchId = inventory.BatchId,
                        Quantity = item.Quantity,
                        ReferenceCode = "CHECKOUT-SHOP",
                        Note = $"Khách hàng {customer.Name} đặt hàng qua Shop",
                        CreatedById = customer.Id,
                        CreatedAt = now
                    });

                    // Tính đơn giá
                    var variantPrice = item.Variant.Prices.FirstOrDefault(pr => pr.UoMId == item.UoMId) ?? item.Variant.Prices.FirstOrDefault();
                    decimal originalPrice = variantPrice?.Price ?? 0;
                    decimal unitPrice = originalPrice;
                    decimal discountAmount = 0;

                    var promo = item.Variant.PromotionVariants
                        .Select(pv => pv.PromotionCampaign)
                        .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                        .OrderByDescending(pc => pc.DiscountValue)
                        .FirstOrDefault();

                    if (promo != null)
                    {
                        if (promo.IsPercentage)
                        {
                            unitPrice = Math.Round(originalPrice * (1 - promo.DiscountValue / 100m));
                            discountAmount = originalPrice - unitPrice;
                        }
                        else
                        {
                            unitPrice = Math.Max(0, originalPrice - promo.DiscountValue);
                            discountAmount = originalPrice - unitPrice;
                        }
                    }

                    decimal lineTotal = unitPrice * item.Quantity;
                    subTotal += originalPrice * item.Quantity;
                    totalDiscount += discountAmount * item.Quantity;

                    orderDetails.Add(new OrderDetail
                    {
                        VariantId = item.VariantId,
                        UoMId = item.UoMId,
                        Quantity = item.Quantity,
                        BaseQuantity = item.Quantity,
                        UnitPrice = unitPrice,
                        DiscountAmount = discountAmount * item.Quantity,
                        TotalPrice = lineTotal,
                        IssuedQuantity = 0
                    });
                }

                // Chiết khấu hội viên thêm (nếu có)
                if (customer.CustomerTier != null && customer.CustomerTier.DiscountPercent > 0)
                {
                    decimal tierDiscount = Math.Round(subTotal * (customer.CustomerTier.DiscountPercent / 100m));
                    totalDiscount += tierDiscount;
                }

                decimal totalAmount = Math.Max(0, subTotal - totalDiscount);

                // 4. Sinh mã đơn hàng ORD-YYYYMMDD-XXXXXX
                string dateStr = now.ToString("yyyyMMdd");
                string randStr = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                string orderCode = $"ORD-{dateStr}-{randStr}";

                var order = new Order
                {
                    OrderCode = orderCode,
                    CustomerId = customerId,
                    CustomerAddressId = customerAddressId,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    DeliveryAddress = deliveryAddress,
                    WarehouseId = selectedWarehouseId,
                    OrderDate = now,
                    Status = OrderStatus.Confirmed, // Đã giữ chỗ tồn kho
                    PaymentStatus = PaymentStatus.Unpaid,
                    PaymentMethod = request.PaymentMethod,
                    SubTotal = subTotal,
                    DiscountAmount = totalDiscount,
                    ShippingFee = 0,
                    TotalAmount = totalAmount,
                    Note = request.Note?.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now,
                    Details = orderDetails
                };

                _context.Orders.Add(order);

                // Xóa giỏ hàng
                _context.ShoppingCartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetOrderByCodeInternalAsync(order.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PagedResult<ShopOrderReadDto>> GetCustomerOrdersAsync(int customerId, int pageIndex = 1, int pageSize = 10)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .Where(o => o.CustomerId == customerId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var orders = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = orders.Select(o => MapToShopOrderDto(o)).ToList();

            return new PagedResult<ShopOrderReadDto>
            {
                Items = items,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<ShopOrderReadDto?> GetOrderByCodeAsync(int customerId, string orderCode)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == orderCode.Trim() && !o.IsDeleted);

            if (order == null)
                return null;

            return MapToShopOrderDto(order);
        }

        public async Task<bool> CancelOrderAsync(int customerId, string orderCode, string reason)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == orderCode.Trim() && !o.IsDeleted);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                throw new InvalidOperationException("Đơn hàng đang được chuẩn bị hoặc đang giao, không thể tự hủy trực tiếp. Vui lòng liên hệ bộ phận hỗ trợ.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // Hoàn trả lại tồn kho đã giữ chỗ (Unreserve)
                if (order.WarehouseId.HasValue && order.Status == OrderStatus.Confirmed)
                {
                    foreach (var d in order.Details)
                    {
                        var inv = await _context.WarehouseInventories
                            .FirstOrDefaultAsync(wi => wi.WarehouseId == order.WarehouseId.Value && wi.VariantId == d.VariantId);

                        if (inv != null)
                        {
                            decimal unreserveQty = Math.Min(inv.QuantityReserved, d.Quantity);
                            inv.QuantityReserved -= unreserveQty;
                            inv.QuantityAvailable += unreserveQty;
                            inv.UpdatedAt = now;

                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                TransactionCode = $"TX-{now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpperInvariant(),
                                Type = TransactionType.Unreserve,
                                WarehouseId = order.WarehouseId.Value,
                                VariantId = d.VariantId,
                                BatchId = inv.BatchId,
                                Quantity = unreserveQty,
                                ReferenceCode = order.OrderCode,
                                Note = $"Hủy giữ chỗ do khách hủy đơn: {reason}",
                                CreatedById = customerId,
                                CreatedAt = now
                            });
                        }
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = reason.Trim();
                order.UpdatedAt = now;

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

        private async Task<ShopOrderReadDto> GetOrderByCodeInternalAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .FirstAsync(o => o.Id == orderId);

            return MapToShopOrderDto(order);
        }

        private ShopOrderReadDto MapToShopOrderDto(Order o)
        {
            return new ShopOrderReadDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                Status = o.Status,
                StatusName = GetStatusName(o.Status),
                PaymentStatus = o.PaymentStatus,
                PaymentStatusName = GetPaymentStatusName(o.PaymentStatus),
                PaymentMethod = o.PaymentMethod,
                PaymentMethodName = GetPaymentMethodName(o.PaymentMethod),
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                ShippingFee = o.ShippingFee,
                TotalAmount = o.TotalAmount,
                ReceiverName = o.ReceiverName,
                ReceiverPhone = o.ReceiverPhone,
                DeliveryAddress = o.DeliveryAddress,
                Note = o.Note,
                CancellationReason = o.CancellationReason,
                Items = o.Details.Select(d => new ShopOrderItemDto
                {
                    DetailId = d.Id,
                    VariantId = d.VariantId,
                    VariantName = d.Variant?.Name ?? string.Empty,
                    VariantCode = d.Variant?.Code ?? string.Empty,
                    ImagePath = d.Variant?.ImagePath,
                    UoMId = d.UoMId,
                    UoMName = d.UoM?.Name ?? string.Empty,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    DiscountAmount = d.DiscountAmount,
                    TotalPrice = d.TotalPrice,
                    IssuedQuantity = d.IssuedQuantity
                }).ToList()
            };
        }

        private string GetStatusName(OrderStatus status) => status switch
        {
            OrderStatus.Draft => "Nháp",
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Processing => "Đang chuẩn bị hàng",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Giao thành công",
            OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        private string GetPaymentStatusName(PaymentStatus status) => status switch
        {
            PaymentStatus.Unpaid => "Chưa thanh toán",
            PaymentStatus.PartiallyPaid => "Đã cọc 1 phần",
            PaymentStatus.Paid => "Đã thanh toán",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => status.ToString()
        };

        private string GetPaymentMethodName(PaymentMethod method) => method switch
        {
            PaymentMethod.COD => "Thanh toán khi nhận hàng (COD)",
            PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
            PaymentMethod.EWallet => "Ví điện tử",
            PaymentMethod.CreditCard => "Thẻ tín dụng / Ghi nợ",
            _ => method.ToString()
        };
    }
}
