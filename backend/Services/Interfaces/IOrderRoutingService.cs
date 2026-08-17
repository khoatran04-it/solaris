using backend.DTOs.OrderDTOs;

namespace backend.Services.Interfaces
{
    public class RoutingResultDto
    {
        public int OptimalWarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public bool IsFullyStocked { get; set; }
        public List<MissingItemDto> MissingItems { get; set; } = new();
        public int? SuggestedSourceWarehouseId { get; set; }
        public string? SuggestedSourceWarehouseName { get; set; }
    }

    public class MissingItemDto
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal MissingQuantity => Math.Max(0, RequestedQuantity - AvailableQuantity);
    }

    public interface IOrderRoutingService
    {
        Task<RoutingResultDto> DetermineOptimalWarehouseAsync(int? customerAddressId, List<OrderDetailCreateDto> items);
    }
}
