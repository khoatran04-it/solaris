namespace backend.DTOs.SupplierAddressDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Địa chỉ giao nhận của Nhà cung cấp.
    /// </summary>
    public class SupplierAddressReadDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }

        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;

        public string? FullAddress { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDefault { get; set; }
    }
}