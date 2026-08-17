namespace backend.DTOs.CustomerAddressDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin Địa chỉ khách hàng.
    /// </summary>
    public class CustomerAddressReadDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
