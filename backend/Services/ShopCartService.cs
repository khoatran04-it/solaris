using backend.Data;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ShopCartService : IShopCartService
    {
        private readonly SolarisDbContext _context;

        public ShopCartService(SolarisDbContext context)
        {
            _context = context;
        }

        public async Task<ShopCartDto> GetCartAsync(int customerId)
        {
            var cart = await GetOrCreateCartEntityAsync(customerId);
            return await BuildCartDtoAsync(cart);
        }

        public async Task<ShopCartDto> AddItemAsync(int customerId, ShopCartAddDto request)
        {
            if (request.Quantity <= 0)
                throw new ArgumentException("Số lượng đặt mua phải lớn hơn 0.");

            var cart = await GetOrCreateCartEntityAsync(customerId);

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.IsActive && !v.IsDeleted);

            if (variant == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            var uom = await _context.UoMs.FirstOrDefaultAsync(u => u.Id == request.UoMId && u.IsActive && !u.IsDeleted);
            if (uom == null)
                throw new KeyNotFoundException("Không tìm thấy đơn vị tính.");

            // Kiểm tra tồn kho khả dụng
            var now = DateTime.UtcNow;
            var availableStock = await _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => wi.VariantId == request.VariantId &&
                             wi.QuantityAvailable > 0 &&
                             (wi.Batch == null || wi.Batch.ExpiryDate > now))
                .SumAsync(wi => wi.QuantityAvailable);

            // Kiểm tra xem item đã tồn tại trong giỏ chưa
            var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == request.VariantId && i.UoMId == request.UoMId);
            decimal targetQuantity = request.Quantity;

            if (existingItem != null)
            {
                targetQuantity += existingItem.Quantity;
                existingItem.Quantity = targetQuantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cart.Items.Add(new ShoppingCartItem
                {
                    CartId = cart.Id,
                    VariantId = request.VariantId,
                    UoMId = request.UoMId,
                    Quantity = request.Quantity,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await BuildCartDtoAsync(cart);
        }

        public async Task<ShopCartDto> UpdateItemQuantityAsync(int customerId, int cartItemId, decimal quantity)
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
        }

        public async Task<ShopCartDto> RemoveItemAsync(int customerId, int cartItemId)
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
        }

        public async Task<ShopCartDto> ClearCartAsync(int customerId)
        {
            var cart = await GetOrCreateCartEntityAsync(customerId);
            if (cart.Items.Any())
            {
                _context.ShoppingCartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return await BuildCartDtoAsync(cart);
        }

        public async Task<ShopCartDto> SyncGuestCartAsync(int customerId, ShopSyncGuestCartDto request)
        {
            if (request.Items == null || !request.Items.Any())
                return await GetCartAsync(customerId);

            var cart = await GetOrCreateCartEntityAsync(customerId);

            foreach (var guestItem in request.Items)
            {
                if (guestItem.Quantity <= 0) continue;

                var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == guestItem.VariantId && i.UoMId == guestItem.UoMId);
                if (existingItem != null)
                {
                    existingItem.Quantity += guestItem.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    cart.Items.Add(new ShoppingCartItem
                    {
                        CartId = cart.Id,
                        VariantId = guestItem.VariantId,
                        UoMId = guestItem.UoMId,
                        Quantity = guestItem.Quantity,
                        AddedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await BuildCartDtoAsync(cart);
        }

        private async Task<ShoppingCart> GetOrCreateCartEntityAsync(int customerId)
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.Product)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v!.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
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

        private async Task<ShopCartDto> BuildCartDtoAsync(ShoppingCart cart)
        {
            var now = DateTime.UtcNow;
            var variantIds = cart.Items.Select(i => i.VariantId).Distinct().ToList();

            // Tồn kho khả dụng
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

                decimal availableStock = inventories.TryGetValue(item.VariantId, out decimal qty) ? qty : 0;
                bool isOutOfStock = availableStock < item.Quantity;

                // Lấy đơn giá theo ĐVT
                var variantPrice = item.Variant.Prices
                    .FirstOrDefault(pr => pr.UoMId == item.UoMId)
                    ?? item.Variant.Prices.FirstOrDefault();

                decimal originalPrice = variantPrice?.Price ?? 0;
                decimal unitPrice = originalPrice;
                decimal discountAmount = 0;

                // Khuyến mãi
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

                // Xuất xứ
                var originAttr = item.Variant.Attributes
                    .FirstOrDefault(a => a.AttributeDefinition != null &&
                                         (a.AttributeDefinition.Name.ToLower().Contains("xuất xứ") || a.AttributeDefinition.Name.ToLower().Contains("vùng trồng")));

                itemDtos.Add(new ShopCartItemDto
                {
                    Id = item.Id,
                    VariantId = item.VariantId,
                    VariantName = item.Variant.Name,
                    VariantCode = item.Variant.Code,
                    ProductSlug = item.Variant.Product?.Slug,
                    ImagePath = item.Variant.ImagePath ?? item.Variant.Product?.ImagePath,
                    UoMId = item.UoMId,
                    UoMName = item.UoM?.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    OriginalPrice = originalPrice,
                    UnitPrice = unitPrice,
                    DiscountAmount = discountAmount,
                    TotalPrice = lineTotal,
                    AvailableStock = availableStock,
                    IsOutOfStock = isOutOfStock,
                    Origin = originAttr?.AttributeValue
                });
            }

            return new ShopCartDto
            {
                CartId = cart.Id,
                Items = itemDtos,
                TotalItems = itemDtos.Count,
                SubTotal = subTotal,
                TotalDiscount = totalDiscount,
                EstimatedTotal = Math.Max(0, subTotal - totalDiscount)
            };
        }
    }
}
