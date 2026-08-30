using AutoMapper;
using backend.Data;
using backend.DTOs.ShopDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    /// <summary>
    /// Service Quản Lý Giỏ Hàng Mua Sắm (Module 12 - Shopping Cart Service).
    /// Chịu trách nhiệm thực hiện các nghiệp vụ: Xem giỏ hàng, Thêm sản phẩm, Cập nhật số lượng, 
    /// Xóa dòng hàng, Làm sạch giỏ và Đồng bộ giỏ hàng khách vãng lai (Guest Cart).
    /// Tích hợp tính toán tồn kho khả dụng thời gian thực và tự động áp dụng chương trình khuyến mãi tốt nhất.
    /// </summary>
    public class ShopCartService : IShopCartService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ShopCartService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region 1. TRUY VẤN GIỎ HÀNG (READ QUERIES)

        /// <inheritdoc />
        public async Task<ShopCartDto> GetCartAsync(int customerId)
        {
            var cart = await GetOrCreateCartEntityAsync(customerId);
            return await BuildCartDtoAsync(cart);
        }

        #endregion

        #region 2. THAO TÁC GIỎ HÀNG (COMMAND OPERATIONS)

        /// <inheritdoc />
        public async Task<ShopCartDto> AddItemAsync(int customerId, ShopCartAddDto request)
        {
            if (request.Quantity <= 0)
                throw new ArgumentException("Số lượng đặt mua phải lớn hơn 0.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var cart = await GetOrCreateCartEntityAsync(customerId);

                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.IsActive && !v.IsDeleted);

                if (variant == null)
                    throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc sản phẩm đã ngừng kinh doanh.");

                var uom = await _context.UoMs
                    .FirstOrDefaultAsync(u => u.Id == request.UoMId && u.IsActive && !u.IsDeleted);

                if (uom == null)
                    throw new KeyNotFoundException("Không tìm thấy đơn vị tính.");

                // Kiểm tra xem mặt hàng cùng SKU và cùng ĐVT đã có trong giỏ chưa
                var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == request.VariantId && i.UoMId == request.UoMId);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newItem = _mapper.Map<ShoppingCartItem>(request);
                    newItem.CartId = cart.Id;
                    newItem.AddedAt = DateTime.UtcNow;
                    newItem.UpdatedAt = DateTime.UtcNow;
                    cart.Items.Add(newItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await BuildCartDtoAsync(cart);
            });
        }

        /// <inheritdoc />
        public async Task<ShopCartDto> UpdateItemQuantityAsync(int customerId, int cartItemId, decimal quantity)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var cart = await GetOrCreateCartEntityAsync(customerId);
                var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

                if (item == null)
                    throw new KeyNotFoundException("Không tìm thấy sản phẩm trong giỏ hàng.");

                if (quantity <= 0)
                {
                    _context.ShoppingCartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                    item.UpdatedAt = DateTime.UtcNow;
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await BuildCartDtoAsync(cart);
            });
        }

        /// <inheritdoc />
        public async Task<ShopCartDto> RemoveItemAsync(int customerId, int cartItemId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var cart = await GetOrCreateCartEntityAsync(customerId);
                var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

                if (item != null)
                {
                    _context.ShoppingCartItems.Remove(item);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return await BuildCartDtoAsync(cart);
            });
        }

        /// <inheritdoc />
        public async Task<ShopCartDto> ClearCartAsync(int customerId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var cart = await GetOrCreateCartEntityAsync(customerId);
                if (cart.Items.Any())
                {
                    _context.ShoppingCartItems.RemoveRange(cart.Items);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return await BuildCartDtoAsync(cart);
            });
        }

        /// <inheritdoc />
        public async Task<ShopCartDto> SyncGuestCartAsync(int customerId, ShopSyncGuestCartDto request)
        {
            if (request.Items == null || !request.Items.Any())
                return await GetCartAsync(customerId);

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var cart = await GetOrCreateCartEntityAsync(customerId);

                foreach (var guestItem in request.Items)
                {
                    if (guestItem.Quantity <= 0) continue;

                    // Kiểm tra xem sản phẩm và ĐVT có hợp lệ không
                    var variantExists = await _context.ProductVariants
                        .AnyAsync(v => v.Id == guestItem.VariantId && v.IsActive && !v.IsDeleted);
                    var uomExists = await _context.UoMs
                        .AnyAsync(u => u.Id == guestItem.UoMId && u.IsActive && !u.IsDeleted);

                    if (!variantExists || !uomExists) continue;

                    var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == guestItem.VariantId && i.UoMId == guestItem.UoMId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += guestItem.Quantity;
                        existingItem.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var newItem = _mapper.Map<ShoppingCartItem>(guestItem);
                        newItem.CartId = cart.Id;
                        newItem.AddedAt = DateTime.UtcNow;
                        newItem.UpdatedAt = DateTime.UtcNow;
                        cart.Items.Add(newItem);
                    }
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await BuildCartDtoAsync(cart);
            });
        }

        #endregion

        #region 3. HELPER METHODS

        /// <summary>
        /// Truy vấn hoặc tự động khởi tạo Giỏ hàng cho Khách hàng nếu chưa có trong Database.
        /// </summary>
        private async Task<ShoppingCart> GetOrCreateCartEntityAsync(int customerId)
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.Product)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                            .ThenInclude(pr => pr.UoM)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.Attributes)
                            .ThenInclude(a => a.AttributeDefinition)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.PromotionVariants)
                            .ThenInclude(pv => pv.PromotionCampaign)
                .Include(c => c.Items)
                    .ThenInclude(i => i.UoM)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        /// <summary>
        /// Xây dựng DTO phản hồi hoàn chỉnh cho Giỏ hàng:
        /// - Ánh xạ AutoMapper thông tin cơ bản của dòng hàng.
        /// - Tính toán tồn kho khả dụng thời gian thực từ bảng WarehouseInventories.
        /// - Xác định đơn giá theo ĐVT và chiết khấu từ các chiến dịch khuyến mãi đang hiệu lực.
        /// </summary>
        private async Task<ShopCartDto> BuildCartDtoAsync(ShoppingCart cart)
        {
            var now = DateTime.UtcNow;
            var variantIds = cart.Items.Select(i => i.VariantId).Distinct().ToList();

            // Truy vấn tổng tồn kho khả dụng thời gian thực (Chỉ tính các Lô hàng chưa hết hạn)
            var inventories = await _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => variantIds.Contains(wi.VariantId) &&
                             wi.QuantityAvailable > 0 &&
                             (wi.Batch == null || wi.Batch.ExpiryDate > now))
                .GroupBy(wi => wi.VariantId)
                .Select(g => new { VariantId = g.Key, TotalAvailable = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.VariantId, x => x.TotalAvailable);

            var itemDtos = new List<ShopCartItemDto>();
            decimal subTotal = 0;
            decimal totalDiscount = 0;

            foreach (var item in cart.Items)
            {
                if (item.Variant == null) continue;

                // 1. Ánh xạ cơ bản qua AutoMapper Profile
                var itemDto = _mapper.Map<ShopCartItemDto>(item);

                // 2. Tính toán tồn kho khả dụng & cờ hết hàng
                decimal availableStock = inventories.TryGetValue(item.VariantId, out decimal qty) ? qty : 0;
                itemDto.AvailableStock = availableStock;
                itemDto.IsOutOfStock = availableStock < item.Quantity;

                // 3. Xác định đơn giá theo Đơn vị tính (UoM)
                var variantPrice = item.Variant.Prices
                    .FirstOrDefault(pr => pr.UoMId == item.UoMId && pr.IsActive && !pr.IsDeleted)
                    ?? item.Variant.Prices.FirstOrDefault(pr => pr.IsActive && !pr.IsDeleted);

                decimal originalPrice = variantPrice?.Price ?? 0;
                decimal unitPrice = originalPrice;
                decimal discountAmount = 0;

                // 4. Áp dụng chương trình khuyến mãi tốt nhất (nếu có)
                var promo = item.Variant.PromotionVariants
                    .Select(pv => pv.PromotionCampaign)
                    .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                    .OrderByDescending(pc => pc!.DiscountValue)
                    .FirstOrDefault();

                if (promo != null)
                {
                    if (promo.IsPercentage)
                    {
                        unitPrice = Math.Max(0, Math.Round(originalPrice * (1 - promo.DiscountValue / 100m)));
                        discountAmount = Math.Max(0, originalPrice - unitPrice);
                    }
                    else
                    {
                        unitPrice = Math.Max(0, originalPrice - promo.DiscountValue);
                        discountAmount = Math.Max(0, originalPrice - unitPrice);
                    }
                }

                itemDto.OriginalPrice = originalPrice;
                itemDto.UnitPrice = unitPrice;
                itemDto.DiscountAmount = discountAmount;
                itemDto.TotalPrice = unitPrice * item.Quantity;

                itemDtos.Add(itemDto);

                subTotal += originalPrice * item.Quantity;
                totalDiscount += discountAmount * item.Quantity;
            }

            var cartDto = _mapper.Map<ShopCartDto>(cart);
            cartDto.Items = itemDtos;
            cartDto.TotalItems = itemDtos.Count;
            cartDto.SubTotal = subTotal;
            cartDto.TotalDiscount = totalDiscount;
            cartDto.EstimatedTotal = Math.Max(0, subTotal - totalDiscount);

            return cartDto;
        }

        #endregion
    }
}
