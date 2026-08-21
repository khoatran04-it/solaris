namespace backend.DTOs.ShopDTOs
{
    public class ShopRegisterRequestDto
    {
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public required string Password { get; set; }

        // Địa chỉ ban đầu (tùy chọn)
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? StreetAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ShopLoginRequestDto
    {
        public required string Username { get; set; } // Email hoặc Số điện thoại
        public required string Password { get; set; }
    }

    public class ShopCustomerInfoDto
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? AvatarPath { get; set; }
        public int? CustomerTierId { get; set; }
        public string? CustomerTierName { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class ShopAuthResponseDto
    {
        public required string Token { get; set; }
        public required ShopCustomerInfoDto CustomerInfo { get; set; }
    }
}
