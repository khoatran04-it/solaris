using backend.DTOs;
using backend.DTOs.InventoryIssueDTOs;

namespace backend.Services.Interfaces
{
    public class SuggestedBatchDto
    {
        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal SuggestedPickQuantity { get; set; }
    }

    public interface IInventoryIssueService
    {
        Task<PagedResult<InventoryIssueReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        Task<InventoryIssueReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        Task<int> CreateAsync(InventoryIssueCreateDto dto, int? currentUserId = null);
        Task<bool> CompleteIssueAsync(int id, int issuedById, string? note);
        Task<bool> CancelIssueAsync(int id, string reason);
        Task<bool> DeleteAsync(int id);
        Task<List<SuggestedBatchDto>> GetSuggestedBatchesAsync(int warehouseId, int variantId, decimal neededQuantity);
    }
}
