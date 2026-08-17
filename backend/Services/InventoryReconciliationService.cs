using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryReconciliationDTOs;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryReconciliationService : IInventoryReconciliationService
    {
        private readonly SolarisDbContext _context;

        public InventoryReconciliationService(SolarisDbContext context)
        {
            _context = context;
        }

        public async Task<ShiftClosingReportDto> GetShiftClosingReportAsync(int warehouseId, DateTime fromDate, DateTime toDate)
        {
            var warehouse = await _context.Warehouses.FindAsync(warehouseId);
            if (warehouse == null) throw new KeyNotFoundException("Không tìm thấy Kho.");

            var fromUtc = fromDate.Date;
            var toUtc = toDate.Date.AddDays(1);

            // 1. Lấy toàn bộ giao dịch trong kỳ của kho
            var transactions = await _context.InventoryTransactions
                .Include(t => t.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Where(t => t.WarehouseId == warehouseId && t.CreatedAt >= fromUtc && t.CreatedAt < toUtc)
                .ToListAsync();

            // 2. Lấy số dư hiện tại trong kho
            var currentInventories = await _context.WarehouseInventories
                .Include(i => i.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Where(i => i.WarehouseId == warehouseId)
                .ToListAsync();

            // Tập hợp tất cả các Variant xuất hiện
            var variantIds = transactions.Select(t => t.VariantId)
                .Union(currentInventories.Select(i => i.VariantId))
                .Distinct()
                .ToList();

            var reportItems = new List<ShiftClosingItemDto>();

            foreach (var vId in variantIds)
            {
                var vTxns = transactions.Where(t => t.VariantId == vId).ToList();
                var vInvs = currentInventories.Where(i => i.VariantId == vId).ToList();

                var variant = vTxns.FirstOrDefault()?.Variant ?? vInvs.FirstOrDefault()?.Variant;
                var uomName = variant?.Product?.BaseUoM?.Name ?? "Cái";

                decimal receipt = vTxns.Where(t => t.Type == TransactionType.Receipt).Sum(t => t.Quantity);
                decimal transferIn = vTxns.Where(t => t.Type == TransactionType.TransferIn).Sum(t => t.Quantity);
                decimal customerReturn = vTxns.Where(t => t.Type == TransactionType.CustomerReturn).Sum(t => t.Quantity);

                decimal issue = vTxns.Where(t => t.Type == TransactionType.Issue).Sum(t => t.Quantity);
                decimal transferOut = vTxns.Where(t => t.Type == TransactionType.TransferOut).Sum(t => t.Quantity);
                decimal adjustment = vTxns.Where(t => t.Type == TransactionType.Adjustment).Sum(t => t.Quantity);

                decimal curAvailable = vInvs.Sum(i => i.QuantityAvailable);
                decimal curReserved = vInvs.Sum(i => i.QuantityReserved);
                decimal curDamaged = vInvs.Sum(i => i.QuantityDamaged);

                decimal totalCurrent = curAvailable + curReserved + curDamaged;
                // Tính ngược lại tồn đầu ca
                decimal netPeriodChange = (receipt + transferIn + customerReturn) - (issue + transferOut) + adjustment;
                decimal openingStock = Math.Max(0, totalCurrent - netPeriodChange);
                decimal closingStock = openingStock + netPeriodChange;

                reportItems.Add(new ShiftClosingItemDto
                {
                    VariantId = vId,
                    VariantName = variant?.Name ?? $"SP #{vId}",
                    VariantCode = variant?.Code ?? string.Empty,
                    UoMName = uomName,
                    OpeningStock = openingStock,
                    TotalReceipt = receipt,
                    TotalTransferIn = transferIn,
                    TotalReturn = customerReturn,
                    TotalIssue = issue,
                    TotalTransferOut = transferOut,
                    TotalAdjustment = adjustment,
                    ClosingStock = closingStock,
                    CurrentAvailable = curAvailable,
                    CurrentReserved = curReserved,
                    CurrentDamaged = curDamaged
                });
            }

            return new ShiftClosingReportDto
            {
                WarehouseId = warehouseId,
                WarehouseName = warehouse.Name,
                FromDate = fromDate,
                ToDate = toDate,
                TotalOpeningItems = reportItems.Sum(i => i.OpeningStock),
                TotalInflowItems = reportItems.Sum(i => i.TotalReceipt + i.TotalTransferIn + i.TotalReturn),
                TotalOutflowItems = reportItems.Sum(i => i.TotalIssue + i.TotalTransferOut),
                TotalClosingItems = reportItems.Sum(i => i.ClosingStock),
                Items = reportItems
            };
        }

        public async Task<PagedResult<StockLedgerEntryDto>> GetStockLedgerAsync(
            int warehouseId,
            int? variantId,
            DateTime? fromDate,
            DateTime? toDate,
            int pageIndex,
            int pageSize)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.Variant)
                .Include(t => t.Batch)
                .Include(t => t.CreatedBy)
                .Where(t => t.WarehouseId == warehouseId)
                .AsQueryable();

            if (variantId.HasValue) query = query.Where(t => t.VariantId == variantId.Value);
            if (fromDate.HasValue) query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(t => t.CreatedAt < toDate.Value.Date.AddDays(1));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var mappedItems = items.Select(t => new StockLedgerEntryDto
            {
                TransactionDate = t.CreatedAt,
                TransactionCode = t.TransactionCode,
                TransactionType = t.Type.ToString(),
                ReferenceCode = t.ReferenceCode ?? string.Empty,
                VariantName = t.Variant?.Name ?? string.Empty,
                BatchCode = t.Batch?.BatchCode ?? string.Empty,
                Quantity = t.Quantity,
                Note = t.Note ?? string.Empty,
                PerformedBy = t.CreatedBy?.FullName ?? "Hệ thống"
            }).ToList();

            return new PagedResult<StockLedgerEntryDto>
            {
                Items = mappedItems,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }
    }
}
