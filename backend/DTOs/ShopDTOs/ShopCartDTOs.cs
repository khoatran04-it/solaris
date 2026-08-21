namespace backend.DTOs.ShopDTOs
{
    public class ShopCartItemDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public required string VariantName { get; set; }
        public required string VariantCode { get; set; }
        public string? ProductSlug { get; set; }
        public string? ImagePath { get; set; }

        public int UoMId { get; set; }
        public required string UoMName { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        public decimal AvailableStock { get; set; }
        public bool IsOutOfStock { get; set; }
        public string? Origin { get; set; }
    }

    public class ShopCartDto
    {
        public int CartId { get; set; }
        public List<ShopCartItemDto> Items { get; set; } = new List<ShopCartItemDto>();
        public int TotalItems { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal EstimatedTotal { get; set; }
    }

    public class ShopCartAddDto
    {
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class ShopCartUpdateDto
    {
        public decimal Quantity { get; set; }
    }

    public class ShopGuestCartItemDto
    {
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class ShopSyncGuestCartDto
    {
        public List<ShopGuestCartItemDto> Items { get; set; } = new List<ShopGuestCartItemDto>();
    }
}
