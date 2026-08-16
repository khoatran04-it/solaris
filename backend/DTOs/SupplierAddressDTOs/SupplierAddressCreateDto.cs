namespace backend.DTOs.SupplierAddressDTOs
{
    public class SupplierAddressCreateDto
    {
        public int SupplierId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone {  get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }

}
