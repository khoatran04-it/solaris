using backend.DTOs;
using backend.DTOs.InventoryReconciliationDTOs;

namespace backend.Services.Interfaces
{
    public interface IInventoryReconciliationService
    {
        Task<ShiftClosingReportDto> GetShiftClosingReportAsync(int warehouseId, DateTime fromDate, DateTime toDate);
        Task<PagedResult<StockLedgerEntryDto>> GetStockLedgerAsync(int warehouseId, int? variantId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize);
    }
}
