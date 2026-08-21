using backend.Models.Enums;

namespace backend.DTOs.ShopDTOs
{
    public class ShopReturnItemRequestDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
        public decimal ReturnedQuantity { get; set; }
        public string? Reason { get; set; }
    }

    public class ShopReturnCreateRequestDto
    {
        public required string OrderCode { get; set; }
        public string? Reason { get; set; }
        public List<ShopReturnItemRequestDto> Items { get; set; } = new List<ShopReturnItemRequestDto>();
    }

    public class ShopReturnItemReadDto
    {
        public int VariantId { get; set; }
        public required string VariantName { get; set; }
        public required string VariantCode { get; set; }
        public string? BatchCode { get; set; }
        public required string UoMName { get; set; }
        public decimal ReturnedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal DamagedQuantity { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RejectReason { get; set; }
    }

    public class ShopReturnReadDto
    {
        public int Id { get; set; }
        public required string ReturnCode { get; set; }
        public required string OrderCode { get; set; }
        public DateTime ReturnDate { get; set; }
        public CustomerReturnStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public string? Reason { get; set; }
        public string? InspectionNotes { get; set; }
        public List<ShopReturnItemReadDto> Details { get; set; } = new List<ShopReturnItemReadDto>();
    }
}
