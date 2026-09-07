using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryAdjustmentDTOs;
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
    /// Service Quản lý Phiếu Điều Chỉnh & Xuất Hủy Tồn Kho (Inventory Adjustments & Write-Offs).
    /// </summary>
    public class InventoryAdjustmentService : IInventoryAdjustmentService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUoMConversionService _uomConversionService;

        public InventoryAdjustmentService(SolarisDbContext context, IMapper mapper, IUoMConversionService? uomConversionService = null)
        {
            _context = context;
            _mapper = mapper;
            _uomConversionService = uomConversionService ?? new UoMConversionService(context, mapper);
        }

        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <inheritdoc />
        public async Task<PagedResult<InventoryAdjustmentReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            int? reason,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryAdjustments
                .Include(a => a.Warehouse)
                .Include(a => a.Audit)
                .Include(a => a.CreatedBy)
                .Include(a => a.ApprovedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(a => allowedWarehouseIds.Contains(a.WarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(a => a.AdjustmentCode.ToLower().Contains(s) ||
                                         (a.Audit != null && a.Audit.AuditCode.ToLower().Contains(s)) ||
                                         (a.CreatedBy != null && a.CreatedBy.FullName.ToLower().Contains(s)) ||
                                         (a.Note != null && a.Note.ToLower().Contains(s)));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(a => a.WarehouseId == warehouseId.Value);
            }

            if (status.HasValue && Enum.IsDefined(typeof(InventoryAdjustmentStatus), status.Value))
            {
                query = query.Where(a => a.Status == (InventoryAdjustmentStatus)status.Value);
            }

            if (reason.HasValue && Enum.IsDefined(typeof(InventoryAdjustmentReason), reason.Value))
            {
                query = query.Where(a => a.Reason == (InventoryAdjustmentReason)reason.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AdjustmentDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AdjustmentDate < endDate.Value.Date.AddDays(1));
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<InventoryAdjustmentReadDto>
            {
                Items = _mapper.Map<IEnumerable<InventoryAdjustmentReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<InventoryAdjustmentReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryAdjustments
                .Include(a => a.Warehouse)
                .Include(a => a.Audit)
                .Include(a => a.CreatedBy)
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
                throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho hoặc bạn không có quyền truy cập kho này.");

            return _mapper.Map<InventoryAdjustmentReadDto>(entity);
        }
        #endregion

        #region Khởi tạo & Quy trình Xử lý (Command & Workflow)
        /// <inheritdoc />
        public async Task<int> CreateAsync(InventoryAdjustmentCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new InvalidOperationException("Phiếu điều chỉnh phải có ít nhất 1 dòng chi tiết.");

            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId);
            if (!warehouseExists)
                throw new KeyNotFoundException($"Kho hàng với ID {dto.WarehouseId} không tồn tại.");

            int creatorId = dto.CreatedById ?? 1;
            var userExists = await _context.IAUsers.AnyAsync(u => u.Id == creatorId);
            if (!userExists)
            {
                var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                if (firstUser != null) creatorId = firstUser.Id;
            }

            var adjustmentCode = $"ADJ-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            var adjustment = new InventoryAdjustment
            {
                AdjustmentCode = adjustmentCode,
                WarehouseId = dto.WarehouseId,
                AuditId = dto.AuditId,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = dto.Reason,
                CreatedById = creatorId,
                AdjustmentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                Note = dto.Note?.Trim()
            };

            decimal totalAmount = 0;

            foreach (var item in dto.Details)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("Số lượng điều chỉnh của từng mặt hàng phải lớn hơn 0.");

                if (item.UnitPrice < 0)
                    throw new InvalidOperationException("Đơn giá không được là số âm.");

                decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(item.VariantId, item.UoMId, item.Quantity);

                // Kiểm tra tồn kho khả dụng khi tạo phiếu điều chỉnh giảm hoặc chuyển sang hàng hỏng
                if (item.AdjustmentType == InventoryAdjustmentType.DecreaseAvailable || item.AdjustmentType == InventoryAdjustmentType.MoveToDamaged)
                {
                    var currentStock = await _context.WarehouseInventories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId &&
                                                  x.VariantId == item.VariantId &&
                                                  x.BatchId == item.BatchId);

                    var available = currentStock?.QuantityAvailable ?? 0;
                    if (available < baseQty)
                    {
                        throw new InvalidOperationException($"Số lượng điều chỉnh ({item.Quantity}) quy đổi ({baseQty}) vượt quá tồn kho khả dụng hiện tại ({available}) của sản phẩm ID {item.VariantId}, Lô ID {item.BatchId}.");
                    }
                }
                else if (item.AdjustmentType == InventoryAdjustmentType.DisposeDamaged)
                {
                    var currentStock = await _context.WarehouseInventories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId &&
                                                  x.VariantId == item.VariantId &&
                                                  x.BatchId == item.BatchId);

                    var damaged = currentStock?.QuantityDamaged ?? 0;
                    if (damaged < baseQty)
                    {
                        throw new InvalidOperationException($"Số lượng tiêu hủy ({item.Quantity}) quy đổi ({baseQty}) vượt quá lượng hàng hư hỏng hiện có ({damaged}) của sản phẩm ID {item.VariantId}, Lô ID {item.BatchId}.");
                    }
                }

                var lineAmount = item.Quantity * item.UnitPrice;
                totalAmount += lineAmount;

                adjustment.Details.Add(new InventoryAdjustmentDetail
                {
                    VariantId = item.VariantId,
                    BatchId = item.BatchId,
                    UoMId = item.UoMId,
                    AdjustmentType = item.AdjustmentType,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = lineAmount,
                    ReasonDetail = item.ReasonDetail?.Trim()
                });
            }

            adjustment.TotalVarianceAmount = totalAmount;

            await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.InventoryAdjustments.Add(adjustment);
                await _context.SaveChangesAsync();
            });

            return adjustment.Id;
        }

        /// <inheritdoc />
        public async Task<bool> ApproveAdjustmentAsync(int id, int approvedById)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var adj = await _context.InventoryAdjustments
                    .Include(a => a.Details)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (adj == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho.");

                if (adj.Status != InventoryAdjustmentStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể duyệt phiếu điều chỉnh ở trạng thái Nháp.");

                var approverExists = await _context.IAUsers.AnyAsync(u => u.Id == approvedById);
                if (!approverExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                    if (firstUser != null) approvedById = firstUser.Id;
                }

                adj.Status = InventoryAdjustmentStatus.Approved;
                adj.ApprovedById = approvedById;
                adj.ApprovedDate = DateTime.UtcNow;
                adj.UpdatedAt = DateTime.UtcNow;

                foreach (var detail in adj.Details)
                {
                    var inv = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(x => x.WarehouseId == adj.WarehouseId &&
                                                  x.VariantId == detail.VariantId &&
                                                  x.BatchId == detail.BatchId);

                    if (inv == null)
                    {
                        inv = new WarehouseInventory
                        {
                            WarehouseId = adj.WarehouseId,
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId,
                            QuantityAvailable = 0,
                            QuantityReserved = 0,
                            QuantityQC = 0,
                            QuantityDamaged = 0
                        };
                        _context.WarehouseInventories.Add(inv);
                    }

                    decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.Quantity);

                    TransactionType txnType = TransactionType.Adjustment;
                    decimal qtySign = baseQty;

                    switch (detail.AdjustmentType)
                    {
                        case InventoryAdjustmentType.IncreaseAvailable:
                            inv.QuantityAvailable += baseQty;
                            qtySign = baseQty;
                            txnType = TransactionType.Adjustment;
                            break;

                        case InventoryAdjustmentType.DecreaseAvailable:
                            if (inv.QuantityAvailable < baseQty)
                            {
                                throw new InvalidOperationException($"Không thể duyệt phiếu: Tồn kho khả dụng hiện tại ({inv.QuantityAvailable}) không đủ để giảm ({baseQty}) cho sản phẩm ID {detail.VariantId}, Lô ID {detail.BatchId}.");
                            }
                            inv.QuantityAvailable -= baseQty;
                            qtySign = -baseQty;
                            txnType = TransactionType.Adjustment;
                            break;

                        case InventoryAdjustmentType.MoveToDamaged:
                            if (inv.QuantityAvailable < baseQty)
                            {
                                throw new InvalidOperationException($"Không thể duyệt phiếu: Tồn kho khả dụng hiện tại ({inv.QuantityAvailable}) không đủ để chuyển sang hàng hỏng ({baseQty}) cho sản phẩm ID {detail.VariantId}, Lô ID {detail.BatchId}.");
                            }
                            inv.QuantityAvailable -= baseQty;
                            inv.QuantityDamaged += baseQty;
                            qtySign = -baseQty;
                            txnType = TransactionType.Adjustment;
                            break;

                        case InventoryAdjustmentType.DisposeDamaged:
                            if (inv.QuantityDamaged < baseQty)
                            {
                                throw new InvalidOperationException($"Không thể duyệt phiếu: Lượng hàng hư hỏng hiện tại ({inv.QuantityDamaged}) không đủ để tiêu hủy ({baseQty}) cho sản phẩm ID {detail.VariantId}, Lô ID {detail.BatchId}.");
                            }
                            inv.QuantityDamaged -= baseQty;
                            qtySign = -baseQty;
                            txnType = TransactionType.Adjustment;
                            break;
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = adj.WarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = txnType,
                        Quantity = qtySign,
                        ReferenceCode = adj.AdjustmentCode,
                        Note = $"Điều chỉnh kho ({adj.Reason}): {detail.ReasonDetail} ({detail.Quantity} ĐVT -> {baseQty} Base UoM)",
                        CreatedById = approvedById,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion

        #region Hủy bỏ & Dọn dẹp (Cancellation & Soft Delete)
        /// <inheritdoc />
        public async Task<bool> CancelAsync(int id, string reason)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var adj = await _context.InventoryAdjustments.FindAsync(id);
                if (adj == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho.");

                if (adj.Status == InventoryAdjustmentStatus.Approved)
                    throw new InvalidOperationException("Không thể hủy Phiếu điều chỉnh đã được duyệt.");

                adj.Status = InventoryAdjustmentStatus.Cancelled;
                adj.Note = string.IsNullOrWhiteSpace(adj.Note)
                    ? $"Hủy phiếu: {reason}"
                    : $"{adj.Note} | Hủy phiếu: {reason}";
                adj.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var adj = await _context.InventoryAdjustments.FindAsync(id);
                if (adj == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho.");

                if (adj.Status == InventoryAdjustmentStatus.Approved)
                    throw new InvalidOperationException("Không thể xóa Phiếu điều chỉnh đã được duyệt và cập nhật vào sổ cái tồn kho.");

                adj.IsDeleted = true;
                adj.DeletedAt = DateTime.UtcNow;
                adj.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion
    }
}
