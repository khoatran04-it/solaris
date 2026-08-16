namespace backend.DTOs.UoMConversionDTOs
{
    public class UoMConversionCreateDto
    {
        public bool IsActive { get; set; }
        public int? ProductId { get; set; }
        public int FromUoMId { get; set; }
        public int ToUoMId { get; set; }
        public bool IsStandard => ProductId == null;
        public decimal ConversionFactor { get; set; }
    }
}
