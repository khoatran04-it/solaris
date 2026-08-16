namespace backend.DTOs.UoMConversionDTOs
{
    public class UoMConversionReadDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int FromUoMId { get; set; }
        public string FromUoMName { get; set; } = string.Empty;
        public int ToUoMId { get; set; }
        public string ToUoMName { get; set; } = string.Empty;
        public bool IsStandard => ProductId == null;
        public decimal ConversionFactor { get; set; }
        public string SemanticDescription => $"1 {FromUoMName} = {ConversionFactor.ToString("0.######")} {ToUoMName}";
    }
}
