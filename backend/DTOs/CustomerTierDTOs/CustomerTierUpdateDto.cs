namespace backend.DTOs.CustomerTierDTOs
{
    public class CustomerTierUpdateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; } = 0;
        public decimal MinSpending { get; set; } = 0;
    }
}
