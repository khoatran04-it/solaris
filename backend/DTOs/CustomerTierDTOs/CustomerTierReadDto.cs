namespace backend.DTOs.CustomerTierDTOs
{
    public class CustomerTierReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public decimal MinSpending { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
