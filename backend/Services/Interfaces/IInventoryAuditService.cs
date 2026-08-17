using backend.DTOs;
using backend.DTOs.InventoryAuditDTOs;

namespace backend.Services.Interfaces
{
    public interface IInventoryAuditService
    {
        Task<PagedResult<InventoryAuditReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            int? auditType,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        Task<InventoryAuditReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        Task<int> CreateAsync(InventoryAuditCreateDto dto);
        Task<bool> SubmitCountAsync(int id, InventoryAuditSubmitCountDto dto);
        Task<int> ApproveAndReconcileAsync(int id, int approvedById);
        Task<bool> CancelAsync(int id, string reason);
    }
}
