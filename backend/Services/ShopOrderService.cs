using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ShopDTOs;
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
    /// <summary>
    /// Service xử lý Đơn hàng cho Storefront B2C (Customer Self-Service Orders).
    /// Chịu trách nhiệm quy trình Checkout giỏ hàng, giữ chỗ tồn kho (Stock Reservation),
    /// tra cứu lịch sử mua hàng, và hỗ trợ khách hàng tự hủy đơn.
    /// </summary>
    public class ShopOrderService : IShopOrderService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOrderRoutingService _routingService;
        private readonly IUoMConversionService _uomConversionService;

        public ShopOrderService(
            SolarisDbContext context,
            IMapper mapper,
            IOrderRoutingService routingService,
            IUoMConversionService? uomConversionService = null)
        {
            _context = context;
            _mapper = mapper;
            _routingService = routingService;
            _uomConversionService = uomConversionService ?? new UoMConversionService(context, mapper);
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
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
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

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var now = DateTime.UtcNow;

                // Lấy tài khoản người dùng hệ thống để ghi nhận sổ cái tồn kho
                var systemUser = await _context.IAUsers.FirstOrDefaultAsync(u => u.IsActive && !u.IsDeleted)
                                 ?? await _context.IAUsers.FirstOrDefaultAsync();
                int systemUserId = systemUser?.Id ?? 2;

                // 1. Chạy Smart Routing Engine để xác định Kho đích tối ưu gần khách hàng nhất
                var cartItemsDto = cart.Items.Select(i => new backend.DTOs.OrderDTOs.OrderDetailCreateDto
                {
                    VariantId = i.VariantId,
                    Quantity = i.Quantity
                }).ToList();

                var routing = await _routingService.DetermineOptimalWarehouseAsync(customerAddressId, cartItemsDto);
                int selectedWarehouseId = routing.OptimalWarehouseId;

                // 2. Rào chắn Khoảng cách Chuỗi lạnh (Cold-Chain Geo-fencing Guard)
                bool hasColdChain = cart.Items.Any(i => i.Variant?.Product?.Category?.RequiresColdChain == true);
                var targetWarehouse = await _context.Warehouses
                    .Include(w => w.Address)
                    .FirstOrDefaultAsync(w => w.Id == selectedWarehouseId);

                double distanceKm = routing.DistanceKm;
                if (distanceKm <= 0 && destLat != 0 && destLng != 0 && targetWarehouse?.Address != null)
                {
                    var distanceService = new DistanceService();
                    distanceKm = distanceService.CalculateDistanceKm(destLat, destLng, targetWarehouse.Address.Latitude, targetWarehouse.Address.Longitude);
                }

                double maxRadius = (targetWarehouse != null && targetWarehouse.MaxColdChainRadiusKm > 0)
                    ? targetWarehouse.MaxColdChainRadiusKm
                    : 15.0;

                if (hasColdChain && distanceKm > maxRadius)
                {
                    throw new InvalidOperationException($"Đơn hàng có sản phẩm chuỗi lạnh (thịt, cá, rau củ tươi sống) nhưng khoảng cách giao hàng ({distanceKm:F1} km) vượt quá bán kính bảo quản tối đa ({maxRadius} km) của kho {targetWarehouse?.Name ?? "xuất hàng"}. Quý khách vui lòng chọn địa chỉ gần hơn hoặc loại bỏ các sản phẩm tươi sống để giao hàng thường.");
                }

                // 3. Chuẩn bị chi tiết đơn hàng & Tính giá
                var orderDetails = new List<OrderDetail>();
                decimal subTotal = 0;
                decimal totalDiscount = 0;

                // 3. Sinh trước mã đơn hàng ORD-YYYYMMDD-XXXXXX
                string dateStr = DateTimeHelper.VietnamDateString;
                string randStr = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                string orderCode = $"ORD-{dateStr}-{randStr}";

                foreach (var item in cart.Items)
                {
                    if (item.Variant == null) continue;

                    // Kiểm tra tồn kho khả dụng tại kho đích trước (ưu tiên lô còn hạn sử dụng theo FEFO)
                    var inventory = await _context.WarehouseInventories
                        .Include(wi => wi.Batch)
                        .Where(wi => wi.WarehouseId == selectedWarehouseId && 
                                     wi.VariantId == item.VariantId && 
                                     wi.QuantityAvailable >= item.Quantity &&
                                     (wi.Batch == null || wi.Batch.ExpiryDate > now))
                        .OrderBy(wi => wi.Batch != null ? wi.Batch.ExpiryDate : DateTime.MaxValue)
                        .FirstOrDefaultAsync();

                    int reserveWarehouseId = selectedWarehouseId;

                    // Quy đổi số lượng đặt hàng sang ĐVT cơ sở (Base UoM) để kiểm tra và giữ chỗ trong két sắt tồn kho
                    decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(item.VariantId, item.UoMId, item.Quantity);

                    // Nếu kho đích không đủ hàng, tìm kho bán lẻ khác có lô còn hạn đủ số lượng để giữ chỗ (Reserve)
                    if (inventory == null)
                    {
                        var fallbackQuery = _context.WarehouseInventories
                            .Include(wi => wi.Batch)
                            .Where(wi => wi.VariantId == item.VariantId && 
                                         wi.QuantityAvailable >= baseQty &&
                                         (wi.Batch == null || wi.Batch.ExpiryDate > now));

                        var filteredFallback = await fallbackQuery.FilterRetailOnlyAsync(_context);

                        var anyStockInv = await filteredFallback
                            .OrderBy(wi => wi.Batch != null ? wi.Batch.ExpiryDate : DateTime.MaxValue)
                            .FirstOrDefaultAsync();

                        if (anyStockInv != null)
                        {
                            reserveWarehouseId = anyStockInv.WarehouseId;
                            inventory = anyStockInv;
                        }
                    }

                    // Fallback: nếu không có lô còn hạn đủ số lượng, lấy bản ghi tồn kho có sẵn tại Kho Bán Lẻ
                    if (inventory == null)
                    {
                        inventory = await _context.WarehouseInventories
                            .FirstOrDefaultAsync(wi => wi.WarehouseId == selectedWarehouseId && wi.VariantId == item.VariantId && wi.QuantityAvailable >= baseQty);

                        if (inventory == null)
                        {
                            var anyStockQuery = _context.WarehouseInventories
                                .Where(wi => wi.VariantId == item.VariantId && wi.QuantityAvailable >= baseQty);
                            var filteredAnyStock = await anyStockQuery.FilterRetailOnlyAsync(_context);
                            inventory = await filteredAnyStock.FirstOrDefaultAsync();
                        }

                        if (inventory != null)
                        {
                            reserveWarehouseId = inventory.WarehouseId;
                        }
                    }

                    if (inventory == null || inventory.QuantityAvailable < baseQty)
                    {
                        decimal currentAvail = inventory?.QuantityAvailable ?? 0;
                        throw new InvalidOperationException($"Sản phẩm '{item.Variant.Name}' không đủ tồn kho khả dụng (Yêu cầu: {baseQty} {item.Variant.Product?.BaseUoM?.Name ?? "ĐVT"}, Còn: {currentAvail}).");
                    }

                    // Khóa giữ chỗ tồn kho tại kho thực tế đang giữ hàng theo Base UoM
                    inventory.QuantityAvailable -= baseQty;
                    inventory.QuantityReserved += baseQty;
                    inventory.UpdatedAt = now;

                    // Ghi sổ cái bất biến Reserve với CreatedById là ID nhân viên/hệ thống hợp lệ theo Base UoM
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TX-{now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpperInvariant(),
                        Type = TransactionType.Reserve,
                        WarehouseId = reserveWarehouseId,
                        VariantId = item.VariantId,
                        BatchId = inventory.BatchId,
                        Quantity = baseQty,
                        ReferenceCode = orderCode,
                        Note = $"Khách hàng {customer.Name} ({customer.Code}) đặt hàng qua Shop ({item.Quantity} ĐVT -> {baseQty} Base UoM, Giữ hàng tại kho #{reserveWarehouseId})",
                        CreatedById = systemUserId,
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
                        .OrderByDescending(pc => pc!.DiscountValue)
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
                        BaseQuantity = baseQty,
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
                    Status = OrderStatus.Pending, // Chờ duyệt đơn hàng
                    PaymentStatus = PaymentStatus.Unpaid,
                    PaymentMethod = request.PaymentMethod,
                    SubTotal = subTotal,
                    DiscountAmount = totalDiscount,
                    ShippingFee = request.ShippingFee,
                    TotalAmount = Math.Max(0, subTotal - totalDiscount + request.ShippingFee),
                    GhnDistrictId = request.GhnDistrictId,
                    GhnWardCode = request.GhnWardCode,
                    ShippingProvider = hasColdChain ? "Internal" : "GHN",
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

                return await GetOrderByCodeInternalAsync(order.Id);
            });
        }

        public async Task<PagedResult<ShopOrderReadDto>> GetCustomerOrdersAsync(int customerId, int pageIndex = 1, int pageSize = 10)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
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
                .AsNoTracking()
                .ToListAsync();

            var items = _mapper.Map<List<ShopOrderReadDto>>(orders);

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
                        .ThenInclude(v => v!.Product)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == orderCode.Trim() && !o.IsDeleted);

            if (order == null)
                return null;

            return _mapper.Map<ShopOrderReadDto>(order);
        }

        public async Task<bool> CancelOrderAsync(int customerId, string orderCode, string reason)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var order = await _context.Orders
                    .Include(o => o.Details)
                    .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == orderCode.Trim() && !o.IsDeleted);

                if (order == null)
                    throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                    throw new InvalidOperationException("Đơn hàng đang được chuẩn bị hoặc đang giao, không thể tự hủy trực tiếp. Vui lòng liên hệ bộ phận hỗ trợ.");

                var now = DateTime.UtcNow;

                var systemUser = await _context.IAUsers.FirstOrDefaultAsync(u => u.IsActive && !u.IsDeleted)
                                 ?? await _context.IAUsers.FirstOrDefaultAsync();
                int systemUserId = systemUser?.Id ?? 2;

                // Hoàn trả lại tồn kho đã giữ chỗ (Unreserve)
                if (order.WarehouseId.HasValue && (order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Pending))
                {
                    foreach (var d in order.Details)
                    {
                        var inv = await _context.WarehouseInventories
                            .FirstOrDefaultAsync(wi => wi.WarehouseId == order.WarehouseId.Value && wi.VariantId == d.VariantId && wi.QuantityReserved > 0)
                            ?? await _context.WarehouseInventories
                            .FirstOrDefaultAsync(wi => wi.VariantId == d.VariantId && wi.QuantityReserved > 0);

                        if (inv != null)
                        {
                            decimal neededUnreserve = d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity;
                            decimal unreserveQty = Math.Min(inv.QuantityReserved, neededUnreserve);
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
                                CreatedById = systemUserId,
                                CreatedAt = now
                            });
                        }
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = reason.Trim();
                order.UpdatedAt = now;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<ShopOrderReadDto> ConfirmDeliveryAsync(int customerId, string orderCode)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var order = await _context.Orders
                    .Include(o => o.Details)
                    .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == orderCode.Trim() && !o.IsDeleted);

                if (order == null)
                    throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

                if (order.Status == OrderStatus.Cancelled)
                    throw new InvalidOperationException("Đơn hàng này đã bị hủy.");

                if (order.Status == OrderStatus.Completed)
                    return await GetOrderByCodeInternalAsync(order.Id);

                var now = DateTime.UtcNow;

                // 1. Chuyển trạng thái đơn hàng sang Hoàn Tất (Giao thành công)
                order.Status = OrderStatus.Completed;
                order.UpdatedAt = now;

                // 2. Nếu là COD hoặc chưa thanh toán, đánh dấu Đã Thanh Toán (khách đã nhận hàng và trả tiền)
                if (order.PaymentStatus != PaymentStatus.Paid)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                }

                await _context.SaveChangesAsync();

                // 3. Tự động tính tổng chi tiêu hoàn tất và xét nâng hạng thành viên (Customer Tier)
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted);
                if (customer != null)
                {
                    var totalSpent = await _context.Orders
                        .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Completed && !o.IsDeleted)
                        .SumAsync(o => o.TotalAmount);

                    var highestTier = await _context.CustomerTiers
                        .Where(t => t.IsActive && !t.IsDeleted && t.MinSpending <= totalSpent)
                        .OrderByDescending(t => t.MinSpending)
                        .FirstOrDefaultAsync();

                    if (highestTier != null && customer.CustomerTierId != highestTier.Id)
                    {
                        customer.CustomerTierId = highestTier.Id;
                        customer.UpdatedAt = now;
                        await _context.SaveChangesAsync();
                    }
                }

                return await GetOrderByCodeInternalAsync(order.Id);
            });
        }

        private async Task<ShopOrderReadDto> GetOrderByCodeInternalAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstAsync(o => o.Id == orderId);

            return _mapper.Map<ShopOrderReadDto>(order);
        }
    }
}
