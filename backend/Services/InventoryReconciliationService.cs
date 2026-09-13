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

            // Tự động kiểm tra và chuẩn hóa các giao dịch Khách trả (RMA) nếu trước đó bị ghi nhầm thành Receipt (Nhập mua)
            bool hasHealed = false;
            var receiptCodes = transactions
                .Where(t => t.Type == TransactionType.Receipt && !string.IsNullOrEmpty(t.ReferenceCode) && t.ReferenceCode.StartsWith("IR-"))
                .Select(t => t.ReferenceCode!)
                .Distinct()
                .ToList();

            if (receiptCodes.Any())
            {
                var rmaReceiptCodes = await _context.InventoryReceipts
                    .Where(r => receiptCodes.Contains(r.ReceiptCode) && r.Note != null &&
                               (r.Note.Contains("RET-") || r.Note.Contains("thu hồi") || r.Note.Contains("trả hàng")))
                    .Select(r => r.ReceiptCode)
                    .ToListAsync();

                if (rmaReceiptCodes.Any())
                {
                    var txnsToHeal = transactions
                        .Where(t => t.Type == TransactionType.Receipt && t.ReferenceCode != null && rmaReceiptCodes.Contains(t.ReferenceCode))
                        .ToList();

                    foreach (var txn in txnsToHeal)
                    {
                        txn.Type = TransactionType.CustomerReturn;
                        hasHealed = true;
                    }
                }
            }

            var directRmaTxns = transactions
                .Where(t => t.Type == TransactionType.Receipt &&
                           ((t.ReferenceCode != null && t.ReferenceCode.StartsWith("RET-")) ||
                            (t.Note != null && (t.Note.Contains("RET-") || t.Note.Contains("thu hồi") || t.Note.Contains("trả hàng")))))
                .ToList();

            if (directRmaTxns.Any())
            {
                foreach (var txn in directRmaTxns)
                {
                    txn.Type = TransactionType.CustomerReturn;
                    hasHealed = true;
                }
            }

            if (hasHealed)
            {
                await _context.SaveChangesAsync();
            }

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

            var mappedItems = items.Select(t =>
            {
                var txnType = t.Type;
                if (txnType == TransactionType.Receipt &&
                    ((t.ReferenceCode != null && t.ReferenceCode.StartsWith("RET-")) ||
                     (t.Note != null && (t.Note.Contains("RET-") || t.Note.Contains("thu hồi") || t.Note.Contains("trả hàng")))))
                {
                    txnType = TransactionType.CustomerReturn;
                }

                return new StockLedgerEntryDto
                {
                    TransactionDate = t.CreatedAt,
                    TransactionCode = t.TransactionCode,
                    TransactionType = txnType.ToString(),
                    ReferenceCode = t.ReferenceCode ?? string.Empty,
                    VariantName = t.Variant?.Name ?? string.Empty,
                    BatchCode = t.Batch?.BatchCode ?? string.Empty,
                    Quantity = t.Quantity,
                    Note = t.Note ?? string.Empty,
                    PerformedBy = t.CreatedBy?.FullName ?? "Hệ thống"
                };
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
