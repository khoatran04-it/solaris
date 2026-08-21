namespace backend.DTOs.ShopDTOs
{
    public class ShopProductCardDto
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? ImagePath { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }
        public string? CategoryGroupName { get; set; }
        public string? CategoryGroupSlug { get; set; }

        public string BaseUoMName { get; set; } = string.Empty;

        // Giá bán (Lấy theo biến thể mặc định)
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool HasPromotion { get; set; }
        public string? PromotionName { get; set; }

        // Thuộc tính EAV nông sản
        public string? Origin { get; set; }              // Vùng trồng (Đà Lạt, Lâm Đồng...)
        public string? Certification { get; set; }       // VietGAP, GlobalGAP, Organic...
        public string? BrixLevel { get; set; }           // Độ ngọt (Brix)

        // Trạng thái kho
        public bool IsInStock { get; set; }
        public decimal TotalAvailableStock { get; set; }
    }

    public class ShopProductDetailDto
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }
        public string? CategoryGroupName { get; set; }
        public string? CategoryGroupSlug { get; set; }

        public int BaseUoMId { get; set; }
        public string BaseUoMName { get; set; } = string.Empty;

        // Thuộc tính EAV nông sản (Nghị định 15/2018/NĐ-CP)
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

        // Danh sách biến thể đóng gói (SKUs)
        public List<ShopProductVariantDto> Variants { get; set; } = new List<ShopProductVariantDto>();

        // Khuyến mãi đang áp dụng
        public List<ShopPromotionBadgeDto> ActivePromotions { get; set; } = new List<ShopPromotionBadgeDto>();
    }

    public class ShopProductVariantDto
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        public List<ShopVariantPriceDto> Prices { get; set; } = new List<ShopVariantPriceDto>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

        public decimal QuantityAvailable { get; set; }
        public bool IsInStock { get; set; }
    }

    public class ShopVariantPriceDto
    {
        public int PriceId { get; set; }
        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsDefault { get; set; }
    }

    public class ShopPromotionBadgeDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public bool IsPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ShopCategoryItemDto
    {
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public required string CategorySlug { get; set; }
        public string? CategoryImage { get; set; }
        public int ProductCount { get; set; }
    }

    public class ShopCategoryTreeDto
    {
        public int GroupId { get; set; }
        public required string GroupName { get; set; }
        public required string GroupSlug { get; set; }
        public string? GroupImage { get; set; }
        public List<ShopCategoryItemDto> Categories { get; set; } = new List<ShopCategoryItemDto>();
    }

    public class ShopProductFilterParams
    {
        public string? Search { get; set; }
        public string? CategoryGroupSlug { get; set; }
        public string? CategorySlug { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Origin { get; set; }
        public string? Certification { get; set; }
        public string? SortBy { get; set; } // price-asc, price-desc, name-asc, newest, discount
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class ShopPromotionDetailDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? BannerImagePath { get; set; }
        public bool IsPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ShopProductCardDto> Products { get; set; } = new List<ShopProductCardDto>();
    }
}
