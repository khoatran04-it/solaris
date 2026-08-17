using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryAuditDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryAuditService : IInventoryAuditService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryAuditService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<InventoryAuditReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            int? auditType,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryAudits
                .Include(a => a.Warehouse)
                .Include(a => a.Auditor)
                .Include(a => a.ApprovedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(a => allowedWarehouseIds.Contains(a.WarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(a => a.AuditCode.ToLower().Contains(s) ||
                                         a.Auditor!.FullName.ToLower().Contains(s));
            }

            if (warehouseId.HasValue) query = query.Where(a => a.WarehouseId == warehouseId.Value);
            if (status.HasValue && Enum.IsDefined(typeof(InventoryAuditStatus), status.Value))
                query = query.Where(a => a.Status == (InventoryAuditStatus)status.Value);
            if (auditType.HasValue && Enum.IsDefined(typeof(InventoryAuditType), auditType.Value))
                query = query.Where(a => a.AuditType == (InventoryAuditType)auditType.Value);

            if (startDate.HasValue) query = query.Where(a => a.AuditDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(a => a.AuditDate < endDate.Value.Date.AddDays(1));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<InventoryAuditReadDto>
            {
                Items = _mapper.Map<IEnumerable<InventoryAuditReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<InventoryAuditReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryAudits
                .Include(a => a.Warehouse)
                .Include(a => a.Auditor)
                .Include(a => a.ApprovedBy)
                .Include(a => a.Details).ThenInclude(d => d.Variant)
                .Include(a => a.Details).ThenInclude(d => d.Batch)
                .Include(a => a.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(a => allowedWarehouseIds.Contains(a.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");

            return _mapper.Map<InventoryAuditReadDto>(entity);
        }

        public async Task<int> CreateAsync(InventoryAuditCreateDto dto)
        {
            var audit = new InventoryAudit
            {
                AuditCode = $"AUD-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                WarehouseId = dto.WarehouseId,
                AuditType = dto.AuditType,
                AuditorId = dto.AuditorId ?? 1,
                AuditDate = DateTime.UtcNow,
                Status = InventoryAuditStatus.InProgress,
                Note = dto.Note
            };

            // 1. Chụp ảnh số liệu tồn hệ thống (Snapshot System Quantity)
            var invQuery = _context.WarehouseInventories
                .Include(i => i.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(i => i.Variant!).ThenInclude(v => v.Prices)
                .Where(i => i.WarehouseId == dto.WarehouseId);

            if (dto.SpecificItems != null && dto.SpecificItems.Any())
            {
                var specificVariantIds = dto.SpecificItems.Select(s => s.VariantId).ToList();
                invQuery = invQuery.Where(i => specificVariantIds.Contains(i.VariantId));
            }

            var inventories = await invQuery.ToListAsync();

            decimal totalSysQty = 0;

            foreach (var inv in inventories)
            {
                var uomId = inv.Variant?.Product?.BaseUoMId ?? inv.Variant?.Prices?.FirstOrDefault()?.UoMId ?? 1;
                var unitPrice = inv.Variant?.Prices?.FirstOrDefault(p => p.UoMId == uomId)?.Price ?? 0;

                totalSysQty += inv.QuantityAvailable;

                audit.Details.Add(new InventoryAuditDetail
                {
                    VariantId = inv.VariantId,
                    BatchId = inv.BatchId,
                    UoMId = uomId,
                    SystemQuantity = inv.QuantityAvailable,
                    ActualQuantity = 0,
                    VarianceQuantity = -inv.QuantityAvailable,
                    UnitPrice = unitPrice,
                    VarianceAmount = -inv.QuantityAvailable * unitPrice
                });
            }

            audit.TotalSystemQty = totalSysQty;
            audit.TotalActualQty = 0;
            audit.TotalVarianceQty = -totalSysQty;

            _context.InventoryAudits.Add(audit);
            await _context.SaveChangesAsync();

            return audit.Id;
        }

        public async Task<bool> SubmitCountAsync(int id, InventoryAuditSubmitCountDto dto)
        {
            var audit = await _context.InventoryAudits
                .Include(a => a.Details)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audit == null) throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");
            if (audit.Status != InventoryAuditStatus.Draft && audit.Status != InventoryAuditStatus.InProgress)
                throw new InvalidOperationException("Chỉ có thể nộp số liệu khi phiếu đang ở trạng thái Nháp hoặc Đang kiểm đếm.");

            decimal totalActual = 0;
            decimal totalVariance = 0;
            decimal totalVarianceAmount = 0;

            foreach (var detail in audit.Details)
            {
                var countItem = dto.Items.FirstOrDefault(i => i.DetailId == detail.Id);
                if (countItem != null)
                {
                    detail.ActualQuantity = countItem.ActualQuantity;
                    detail.VarianceQuantity = detail.ActualQuantity - detail.SystemQuantity;
                    detail.VarianceAmount = detail.VarianceQuantity * detail.UnitPrice;
                    detail.ReasonNote = countItem.ReasonNote;
                }

                totalActual += detail.ActualQuantity;
                totalVariance += detail.VarianceQuantity;
                totalVarianceAmount += detail.VarianceAmount;
            }

            audit.TotalActualQty = totalActual;
            audit.TotalVarianceQty = totalVariance;
            audit.TotalVarianceAmount = totalVarianceAmount;
            audit.Status = InventoryAuditStatus.PendingApproval;
            if (!string.IsNullOrWhiteSpace(dto.Note)) audit.Note = dto.Note;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ApproveAndReconcileAsync(int id, int approvedById)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var audit = await _context.InventoryAudits
                    .Include(a => a.Details)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (audit == null) throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");
                if (audit.Status != InventoryAuditStatus.PendingApproval && audit.Status != InventoryAuditStatus.InProgress)
                    throw new InvalidOperationException("Phiếu kiểm kê phải ở trạng thái Chờ duyệt.");

                audit.Status = InventoryAuditStatus.Completed;
                audit.ApprovedById = approvedById;
                audit.CompletedDate = DateTime.UtcNow;

                // Tự động sinh Phiếu Điều Chỉnh Tồn Kho để cân bằng số liệu
                var nonZeroDetails = audit.Details.Where(d => d.VarianceQuantity != 0).ToList();
                int adjustmentId = 0;

                if (nonZeroDetails.Any())
                {
                    var adjustment = new InventoryAdjustment
                    {
                        AdjustmentCode = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                        WarehouseId = audit.WarehouseId,
                        AuditId = audit.Id,
                        Status = InventoryAdjustmentStatus.Approved,
                        Reason = InventoryAdjustmentReason.Surplus,
                        CreatedById = audit.AuditorId,
                        ApprovedById = approvedById,
                        AdjustmentDate = DateTime.UtcNow,
                        ApprovedDate = DateTime.UtcNow,
                        TotalVarianceAmount = audit.TotalVarianceAmount,
                        Note = $"Cân bằng tồn kho tự động theo kết quả kiểm kê {audit.AuditCode}"
                    };

                    foreach (var detail in nonZeroDetails)
                    {
                        var adjType = detail.VarianceQuantity > 0 
                            ? InventoryAdjustmentType.IncreaseAvailable 
                            : InventoryAdjustmentType.DecreaseAvailable;

                        var qty = Math.Abs(detail.VarianceQuantity);

                        adjustment.Details.Add(new InventoryAdjustmentDetail
                        {
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId,
                            UoMId = detail.UoMId,
                            AdjustmentType = adjType,
                            Quantity = qty,
                            UnitPrice = detail.UnitPrice,
                            TotalAmount = qty * detail.UnitPrice,
                            ReasonDetail = detail.ReasonNote ?? $"Điều chỉnh kiểm kê {audit.AuditCode}"
                        });

                        // Cập nhật ngay vào WarehouseInventory
                        var inv = await _context.WarehouseInventories
                            .FirstOrDefaultAsync(x => x.WarehouseId == audit.WarehouseId &&
                                                      x.VariantId == detail.VariantId &&
                                                      x.BatchId == detail.BatchId);

                        if (inv == null && adjType == InventoryAdjustmentType.IncreaseAvailable)
                        {
                            inv = new WarehouseInventory
                            {
                                WarehouseId = audit.WarehouseId,
                                VariantId = detail.VariantId,
                                BatchId = detail.BatchId,
                                QuantityAvailable = qty,
                                QuantityReserved = 0,
                                QuantityQC = 0,
                                QuantityDamaged = 0
                            };
                            _context.WarehouseInventories.Add(inv);
                        }
                        else if (inv != null)
                        {
                            if (adjType == InventoryAdjustmentType.IncreaseAvailable)
                            {
                                inv.QuantityAvailable += qty;
                            }
                            else
                            {
                                inv.QuantityAvailable = Math.Max(0, inv.QuantityAvailable - qty);
                            }
                        }

                        // Ghi Sổ cái giao dịch InventoryTransaction
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                            WarehouseId = audit.WarehouseId,
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId,
                            Type = TransactionType.Adjustment,
                            Quantity = detail.VarianceQuantity, // dương hoặc âm
                            ReferenceCode = audit.AuditCode,
                            Note = $"Cân bằng kiểm kê: Thực tế={detail.ActualQuantity}, Hệ thống={detail.SystemQuantity} ({detail.ReasonNote})",
                            CreatedById = approvedById
                        });
                    }

                    _context.InventoryAdjustments.Add(adjustment);
                    await _context.SaveChangesAsync();
                    adjustmentId = adjustment.Id;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return adjustmentId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelAsync(int id, string reason)
        {
            var audit = await _context.InventoryAudits.FindAsync(id);
            if (audit == null) throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");
            if (audit.Status == InventoryAuditStatus.Completed)
                throw new InvalidOperationException("Không thể hủy Phiếu kiểm kê đã hoàn tất và chốt sổ.");

            audit.Status = InventoryAuditStatus.Cancelled;
            audit.Note = $"Hủy phiếu: {reason}";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
