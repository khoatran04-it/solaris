using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryIssueDTOs;
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
    /// Service xử lý toàn bộ quy trình Phiếu Xuất Kho (Goods Issue Note) và thuật toán gợi ý lấy hàng FEFO.
    /// </summary>
    public class InventoryIssueService : IInventoryIssueService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryIssueService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <inheritdoc />
        public async Task<PagedResult<InventoryIssueReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryIssues
                .Include(i => i.Order)
                .Include(i => i.Warehouse)
                .Include(i => i.IssuedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(i => allowedWarehouseIds.Contains(i.WarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(i => i.IssueCode.ToLower().Contains(s) ||
                                         (i.ReceiverName != null && i.ReceiverName.ToLower().Contains(s)) ||
                                         (i.Order != null && i.Order.OrderCode.ToLower().Contains(s)) ||
                                         (i.Note != null && i.Note.ToLower().Contains(s)));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(i => i.WarehouseId == warehouseId.Value);

            if (status.HasValue && Enum.IsDefined(typeof(InventoryIssueStatus), status.Value))
                query = query.Where(i => i.Status == (InventoryIssueStatus)status.Value);

            if (startDate.HasValue)
                query = query.Where(i => i.IssueDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(i => i.IssueDate < endDate.Value.Date.AddDays(1));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<InventoryIssueReadDto>
            {
                Items = _mapper.Map<IEnumerable<InventoryIssueReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<InventoryIssueReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryIssues
                .Include(i => i.Order)
                .Include(i => i.Warehouse)
                .Include(i => i.IssuedBy)
                .Include(i => i.Details).ThenInclude(d => d.Variant)
                .Include(i => i.Details).ThenInclude(d => d.Batch)
                .Include(i => i.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(i => allowedWarehouseIds.Contains(i.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(i => i.Id == id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");

            return _mapper.Map<InventoryIssueReadDto>(entity);
        }
        #endregion

        #region Thao tác Dữ liệu & Quy trình (Command & Workflow)
        /// <inheritdoc />
        public async Task<int> CreateAsync(InventoryIssueCreateDto dto, int? currentUserId = null)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new InvalidOperationException("Phiếu xuất kho phải có ít nhất 1 dòng chi tiết.");

            var whExists = await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
            if (!whExists)
                throw new InvalidOperationException($"Kho hàng với ID {dto.WarehouseId} không tồn tại hoặc đã bị vô hiệu hóa.");

            int safeUserId = currentUserId ?? dto.IssuedById ?? 1;
            var userExists = await _context.IAUsers.AnyAsync(u => u.Id == safeUserId && !u.IsDeleted);
            if (!userExists)
            {
                var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                safeUserId = firstUser?.Id ?? 1;
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var issue = _mapper.Map<InventoryIssue>(dto);

                issue.IssueCode = $"ISS-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                issue.IssuedById = safeUserId;
                issue.IssueDate = dto.IssueDate ?? DateTime.UtcNow;
                issue.Status = InventoryIssueStatus.Pending;
                issue.CreatedAt = DateTime.UtcNow;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.IsDeleted = false;

                // Tự động tính toán Thể tích (TotalCbm) và Trọng lượng (TotalWeightKg) của kiện hàng xuất kho
                var variantIds = issue.Details.Select(d => d.VariantId).Distinct().ToList();
                var variants = await _context.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);

                foreach (var d in issue.Details)
                {
                    if (variants.TryGetValue(d.VariantId, out var variant))
                    {
                        if (!d.TotalCbm.HasValue || d.TotalCbm.Value <= 0)
                        {
                            var unitCbm = variant.UnitCbm ?? (
                                (variant.LengthCm > 0 && variant.WidthCm > 0 && variant.HeightCm > 0)
                                    ? (variant.LengthCm.Value * variant.WidthCm.Value * variant.HeightCm.Value) / 1000000m
                                    : 0.02m
                            );
                            d.TotalCbm = Math.Round(unitCbm * d.Quantity, 4);
                        }

                        if (!d.TotalWeightKg.HasValue || d.TotalWeightKg.Value <= 0)
                        {
                            var unitWeight = variant.GrossWeightKg ?? 1.0m;
                            d.TotalWeightKg = Math.Round(unitWeight * d.Quantity, 2);
                        }
                    }
                }

                _context.InventoryIssues.Add(issue);
                await _context.SaveChangesAsync();

                return issue.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CompleteIssueAsync(int id, int issuedById, string? note)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var issue = await _context.InventoryIssues
                    .Include(i => i.Details)
                    .Include(i => i.Order).ThenInclude(o => o!.Details)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (issue == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");

                if (issue.Status != InventoryIssueStatus.Pending && issue.Status != InventoryIssueStatus.Picking)
                    throw new InvalidOperationException("Phiếu xuất kho phải ở trạng thái Chờ xử lý (Pending) hoặc Đang nhặt hàng (Picking) mới có thể hoàn tất.");

                int safeUserId = issuedById;
                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == safeUserId && !u.IsDeleted);
                if (!userExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                    safeUserId = firstUser?.Id ?? 1;
                }

                issue.Status = InventoryIssueStatus.Completed;
                issue.IssuedById = safeUserId;
                issue.IssueDate = DateTime.UtcNow;
                issue.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(note)) issue.Note = note.Trim();

                // 1. Cập nhật trừ kho (Trừ QuantityReserved) & Ghi sổ cái Issue
                foreach (var detail in issue.Details)
                {
                    var inventory = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(x => x.WarehouseId == issue.WarehouseId &&
                                                  x.VariantId == detail.VariantId &&
                                                  x.BatchId == detail.BatchId);

                    if (inventory != null)
                    {
                        if (inventory.QuantityReserved >= detail.Quantity)
                        {
                            inventory.QuantityReserved -= detail.Quantity;
                        }
                        else
                        {
                            var diff = detail.Quantity - inventory.QuantityReserved;
                            inventory.QuantityReserved = 0;
                            inventory.QuantityAvailable = Math.Max(0, inventory.QuantityAvailable - diff);
                        }
                        inventory.UpdatedAt = DateTime.UtcNow;
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = issue.WarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.Issue,
                        Quantity = detail.Quantity,
                        ReferenceCode = issue.IssueCode,
                        Note = $"Xuất kho theo phiếu {issue.IssueCode}",
                        CreatedById = safeUserId,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Cập nhật tiến độ trên OrderDetail
                    if (detail.OrderDetailId.HasValue)
                    {
                        var od = await _context.OrderDetails.FindAsync(detail.OrderDetailId.Value);
                        if (od != null)
                        {
                            od.IssuedQuantity += detail.Quantity;
                        }
                    }
                }

                // 2. Cập nhật trạng thái Order liên quan
                if (issue.Order != null)
                {
                    bool isFullyIssued = issue.Order.Details.All(d => d.IssuedQuantity >= d.Quantity);
                    issue.Order.Status = isFullyIssued ? OrderStatus.Shipping : OrderStatus.Processing;
                    issue.Order.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CancelIssueAsync(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy phiếu xuất kho không được để trống.", nameof(reason));

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var issue = await _context.InventoryIssues.FindAsync(id);
                if (issue == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");

                if (issue.Status == InventoryIssueStatus.Completed)
                    throw new InvalidOperationException("Không thể hủy Phiếu xuất kho đã hoàn tất.");

                issue.Status = InventoryIssueStatus.Cancelled;
                issue.CancellationReason = reason.Trim();
                issue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var issue = await _context.InventoryIssues.FindAsync(id);
                if (issue == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");

                if (issue.Status == InventoryIssueStatus.Completed)
                    throw new InvalidOperationException("Không thể xóa Phiếu xuất kho đã hoàn tất.");

                issue.IsDeleted = true;
                issue.DeletedAt = DateTime.UtcNow;
                issue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion

        #region Thuật toán Kho (Smart Logistics)
        /// <inheritdoc />
        public async Task<List<SuggestedBatchDto>> GetSuggestedBatchesAsync(int warehouseId, int variantId, decimal neededQuantity)
        {
            var inventories = await _context.WarehouseInventories
                .Include(i => i.Batch)
                .Where(i => i.WarehouseId == warehouseId && i.VariantId == variantId && (i.QuantityAvailable > 0 || i.QuantityReserved > 0))
                .OrderBy(i => i.Batch != null ? i.Batch.ExpiryDate : DateTime.MaxValue) // FEFO
                .ToListAsync();

            var result = new List<SuggestedBatchDto>();
            decimal remaining = neededQuantity;

            foreach (var inv in inventories)
            {
                var pickQty = Math.Min(inv.QuantityAvailable + inv.QuantityReserved, remaining);
                result.Add(new SuggestedBatchDto
                {
                    BatchId = inv.BatchId,
                    BatchCode = inv.Batch?.BatchCode ?? string.Empty,
                    ExpiryDate = inv.Batch?.ExpiryDate,
                    QuantityAvailable = inv.QuantityAvailable,
                    QuantityReserved = inv.QuantityReserved,
                    SuggestedPickQuantity = pickQty
                });

                remaining -= pickQty;
                if (remaining <= 0) break;
            }

            return result;
        }
        #endregion
    }
}
