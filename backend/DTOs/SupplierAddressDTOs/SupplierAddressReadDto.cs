namespace backend.DTOs.SupplierAddressDTOs
{
    public class SupplierAddressReadDto
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }

        public string ContactName { get; set; }
        public string ContactPhone { get; set; }

        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string StreetAddress { get; set; }

        public string? FullAddress { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDefault { get; set; }
    }
}