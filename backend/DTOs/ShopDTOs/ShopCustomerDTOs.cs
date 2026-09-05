namespace backend.DTOs.ShopDTOs
{
    public class ShopCustomerProfileDto
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? Gender { get; set; }
        public string? AvatarPath { get; set; }
        public string? CustomerTierName { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalSpent { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;
        public string? NextTierName { get; set; }
        public decimal? NextTierMinSpending { get; set; }
        public decimal AmountToNextTier { get; set; } = 0;
        public decimal TierProgressPercent { get; set; } = 0;
        public List<ShopAddressDto> Addresses { get; set; } = new List<ShopAddressDto>();
    }

    public class ShopCustomerProfileUpdateDto
    {
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? Gender { get; set; }
        public string? AvatarPath { get; set; }
    }

    public class ShopAddressDto
    {
        public int Id { get; set; }
        public required string ReceiverName { get; set; }
        public required string Phone { get; set; }
        public required string Province { get; set; }
        public required string District { get; set; }
        public required string Ward { get; set; }
        public required string StreetAddress { get; set; }
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";
        public bool IsDefault { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ShopAddressCreateDto
    {
        public required string ReceiverName { get; set; }
        public required string Phone { get; set; }
        public required string Province { get; set; }
        public required string District { get; set; }
        public required string Ward { get; set; }
        public required string StreetAddress { get; set; }
        public bool IsDefault { get; set; } = false;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ShopAddressUpdateDto : ShopAddressCreateDto
    {
    }
}
