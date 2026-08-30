using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryAuditDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    /// <summary>
    /// Service Quản lý Quy trình Kiểm kê Kho Hàng (Stocktake / Inventory Audits & Blind Count).
    /// </summary>
    public class InventoryAuditService : IInventoryAuditService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryAuditService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <inheritdoc />
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
                var s = search.ToLower().Trim();
                query = query.Where(a => a.AuditCode.ToLower().Contains(s) ||
                                         (a.Auditor != null && a.Auditor.FullName.ToLower().Contains(s)) ||
                                         (a.Note != null && a.Note.ToLower().Contains(s)));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(a => a.WarehouseId == warehouseId.Value);
            }

            if (status.HasValue && Enum.IsDefined(typeof(InventoryAuditStatus), status.Value))
            {
                query = query.Where(a => a.Status == (InventoryAuditStatus)status.Value);
            }

            if (auditType.HasValue && Enum.IsDefined(typeof(InventoryAuditType), auditType.Value))
            {
                query = query.Where(a => a.AuditType == (InventoryAuditType)auditType.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AuditDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AuditDate < endDate.Value.Date.AddDays(1));
            }

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

        /// <inheritdoc />
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
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê hoặc bạn không có quyền truy cập kho này.");

            return _mapper.Map<InventoryAuditReadDto>(entity);
        }
        #endregion

        #region Quy trình 1: Khởi tạo & Đếm thực tế (Snapshot & Blind Count)
        /// <inheritdoc />
        public async Task<int> CreateAsync(InventoryAuditCreateDto dto)
        {
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId);
            if (!warehouseExists)
                throw new KeyNotFoundException($"Kho hàng với ID {dto.WarehouseId} không tồn tại.");

            int auditorId = dto.AuditorId ?? 1;
            var auditorExists = await _context.IAUsers.AnyAsync(u => u.Id == auditorId);
            if (!auditorExists)
            {
                var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                if (firstUser != null) auditorId = firstUser.Id;
            }

            var auditCode = $"AUD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            var audit = new InventoryAudit
            {
                AuditCode = auditCode,
                WarehouseId = dto.WarehouseId,
                AuditType = dto.AuditType,
                AuditorId = auditorId,
                AuditDate = DateTime.UtcNow,
                Status = InventoryAuditStatus.InProgress,
                Note = dto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // 1. Chụp ảnh số liệu tồn hệ thống (Snapshot System Quantity)
            var invQuery = _context.WarehouseInventories
                .Include(i => i.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(i => i.Variant!).ThenInclude(v => v.Prices)
                .Where(i => i.WarehouseId == dto.WarehouseId);

            if (dto.SpecificItems != null && dto.SpecificItems.Any())
            {
                var specificVariantIds = dto.SpecificItems.Select(s => s.VariantId).Distinct().ToList();
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
            audit.TotalVarianceAmount = audit.Details.Sum(d => d.VarianceAmount);

            await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.InventoryAudits.Add(audit);
                await _context.SaveChangesAsync();
            });

            return audit.Id;
        }

        /// <inheritdoc />
        public async Task<bool> SubmitCountAsync(int id, InventoryAuditSubmitCountDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var audit = await _context.InventoryAudits
                    .Include(a => a.Details)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (audit == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");

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
                        detail.ReasonNote = countItem.ReasonNote?.Trim();
                    }

                    totalActual += detail.ActualQuantity;
                    totalVariance += detail.VarianceQuantity;
                    totalVarianceAmount += detail.VarianceAmount;
                }

                audit.TotalActualQty = totalActual;
                audit.TotalVarianceQty = totalVariance;
                audit.TotalVarianceAmount = totalVarianceAmount;
                audit.Status = InventoryAuditStatus.PendingApproval;
                audit.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(dto.Note))
                {
                    audit.Note = dto.Note.Trim();
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion

        #region Quy trình 2: Chốt sổ & Bù trừ (Reconciliation)
        /// <inheritdoc />
        public async Task<int> ApproveAndReconcileAsync(int id, int approvedById)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var audit = await _context.InventoryAudits
                    .Include(a => a.Details)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (audit == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");

                if (audit.Status != InventoryAuditStatus.PendingApproval && audit.Status != InventoryAuditStatus.InProgress)
                    throw new InvalidOperationException("Phiếu kiểm kê phải ở trạng thái Đang kiểm đếm hoặc Chờ duyệt để thực hiện chốt sổ.");

                var approverExists = await _context.IAUsers.AnyAsync(u => u.Id == approvedById);
                if (!approverExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                    if (firstUser != null) approvedById = firstUser.Id;
                }

                audit.Status = InventoryAuditStatus.Completed;
                audit.ApprovedById = approvedById;
                audit.CompletedDate = DateTime.UtcNow;
                audit.UpdatedAt = DateTime.UtcNow;

                // Tự động sinh Phiếu Điều Chỉnh Tồn Kho để cân bằng số liệu
                var nonZeroDetails = audit.Details.Where(d => d.VarianceQuantity != 0).ToList();
                int adjustmentId = 0;

                if (nonZeroDetails.Any())
                {
                    var reason = audit.TotalVarianceQty >= 0
                        ? InventoryAdjustmentReason.Surplus
                        : InventoryAdjustmentReason.Shrinkage;

                    var adjustment = new InventoryAdjustment
                    {
                        AdjustmentCode = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                        WarehouseId = audit.WarehouseId,
                        AuditId = audit.Id,
                        Status = InventoryAdjustmentStatus.Approved,
                        Reason = reason,
                        CreatedById = audit.AuditorId,
                        ApprovedById = approvedById,
                        AdjustmentDate = DateTime.UtcNow,
                        ApprovedDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        TotalVarianceAmount = Math.Abs(audit.TotalVarianceAmount),
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
                            Quantity = detail.VarianceQuantity,
                            ReferenceCode = audit.AuditCode,
                            Note = $"Cân bằng kiểm kê {audit.AuditCode}: Thực tế={detail.ActualQuantity}, Hệ thống={detail.SystemQuantity} ({detail.ReasonNote})",
                            CreatedById = approvedById,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    _context.InventoryAdjustments.Add(adjustment);
                    await _context.SaveChangesAsync();
                    adjustmentId = adjustment.Id;
                }
                else
                {
                    await _context.SaveChangesAsync();
                }

                return adjustmentId;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CancelAsync(int id, string reason)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var audit = await _context.InventoryAudits.FindAsync(id);
                if (audit == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu kiểm kê.");

                if (audit.Status == InventoryAuditStatus.Completed)
                    throw new InvalidOperationException("Không thể hủy Phiếu kiểm kê đã hoàn tất và chốt sổ.");

                audit.Status = InventoryAuditStatus.Cancelled;
                audit.Note = string.IsNullOrWhiteSpace(audit.Note)
                    ? $"Hủy phiếu: {reason}"
                    : $"{audit.Note} | Hủy phiếu: {reason}";
                audit.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion
    }
}
