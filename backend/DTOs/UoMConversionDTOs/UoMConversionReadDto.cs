namespace backend.DTOs.UoMConversionDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết quy tắc quy đổi kèm diễn giải ngữ nghĩa (Semantic Description).
    /// </summary>
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

        /// <summary>Diễn giải ngữ nghĩa trực quan (Ví dụ: "1 Thùng = 24 Chai")</summary>
        public string SemanticDescription => $"1 {FromUoMName} = {ConversionFactor:0.######} {ToUoMName}";
    }
}
