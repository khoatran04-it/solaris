using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryIssueDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryIssueService : IInventoryIssueService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryIssueService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

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
                var s = search.ToLower();
                query = query.Where(i => i.IssueCode.ToLower().Contains(s) ||
                                         (i.ReceiverName != null && i.ReceiverName.ToLower().Contains(s)) ||
                                         (i.Order != null && i.Order.OrderCode.ToLower().Contains(s)));
            }

            if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
            if (status.HasValue && Enum.IsDefined(typeof(InventoryIssueStatus), status.Value))
                query = query.Where(i => i.Status == (InventoryIssueStatus)status.Value);

            if (startDate.HasValue) query = query.Where(i => i.IssueDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(i => i.IssueDate < endDate.Value.Date.AddDays(1));

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
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");

            return _mapper.Map<InventoryIssueReadDto>(entity);
        }

        public async Task<int> CreateAsync(InventoryIssueCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu xuất phải có ít nhất 1 dòng chi tiết.");

            var issue = new InventoryIssue
            {
                IssueCode = $"ISS-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                OrderId = dto.OrderId,
                WarehouseId = dto.WarehouseId,
                IssuedById = dto.IssuedById ?? 1,
                IssueDate = dto.IssueDate ?? DateTime.UtcNow,
                Status = InventoryIssueStatus.Pending,
                ReceiverName = dto.ReceiverName,
                ReceiverPhone = dto.ReceiverPhone,
                DeliveryAddress = dto.DeliveryAddress,
                Note = dto.Note
            };

            foreach (var item in dto.Details)
            {
                issue.Details.Add(new InventoryIssueDetail
                {
                    OrderDetailId = item.OrderDetailId,
                    VariantId = item.VariantId,
                    BatchId = item.BatchId,
                    UoMId = item.UoMId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Quantity * item.UnitPrice
                });
            }

            _context.InventoryIssues.Add(issue);
            await _context.SaveChangesAsync();

            return issue.Id;
        }

        public async Task<bool> CompleteIssueAsync(int id, int issuedById, string? note)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var issue = await _context.InventoryIssues
                    .Include(i => i.Details)
                    .Include(i => i.Order).ThenInclude(o => o!.Details)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (issue == null) throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");
                if (issue.Status != InventoryIssueStatus.Pending && issue.Status != InventoryIssueStatus.Picking)
                    throw new InvalidOperationException("Phiếu xuất phải ở trạng thái Pending hoặc Picking mới có thể hoàn tất.");

                issue.Status = InventoryIssueStatus.Completed;
                issue.IssuedById = issuedById;
                issue.IssueDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(note)) issue.Note = note;

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
                            // Nếu xuất nhiều hơn số giữ chỗ, trừ phần còn lại vào QuantityAvailable
                            var diff = detail.Quantity - inventory.QuantityReserved;
                            inventory.QuantityReserved = 0;
                            inventory.QuantityAvailable = Math.Max(0, inventory.QuantityAvailable - diff);
                        }
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = issue.WarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.Issue,
                        Quantity = detail.Quantity,
                        ReferenceCode = issue.IssueCode,
                        Note = $"Xuất kho theo phiếu {issue.IssueCode}",
                        CreatedById = issuedById
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
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelIssueAsync(int id, string reason)
        {
            var issue = await _context.InventoryIssues.FindAsync(id);
            if (issue == null) throw new KeyNotFoundException("Không tìm thấy Phiếu xuất kho.");
            if (issue.Status == InventoryIssueStatus.Completed)
                throw new InvalidOperationException("Không thể hủy Phiếu xuất kho đã hoàn tất.");

            issue.Status = InventoryIssueStatus.Cancelled;
            issue.CancellationReason = reason;

            await _context.SaveChangesAsync();
            return true;
        }

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
            }

            return result;
        }
    }
}
