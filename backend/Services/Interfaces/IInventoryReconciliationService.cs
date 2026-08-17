using backend.DTOs;
using backend.DTOs.InventoryReconciliationDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Báo Cáo Chốt Ca & Đối Soát Sổ Cái Tồn Kho (Shift Closing & Stock Reconciliation).
    /// </summary>
    public interface IInventoryReconciliationService
    {
        /// <summary>
        /// Lập báo cáo chốt ca theo khoảng thời gian: Tính toán Tồn đầu, Tổng nhập, Tổng xuất, Tổng điều chỉnh, Tồn cuối và kiểm tra tính cân bằng toán học.
        /// </summary>
        Task<ShiftClosingReportDto> GetShiftClosingReportAsync(int warehouseId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Truy vấn sổ cái dòng thời gian giao dịch biến động tồn kho chi tiết (Stock Ledger Timeline).
        /// </summary>
        Task<PagedResult<StockLedgerEntryDto>> GetStockLedgerAsync(int warehouseId, int? variantId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize);
    }
}
