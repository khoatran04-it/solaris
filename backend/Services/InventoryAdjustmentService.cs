using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryAdjustmentDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryAdjustmentService : IInventoryAdjustmentService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryAdjustmentService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

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
                var s = search.ToLower();
                query = query.Where(a => a.AdjustmentCode.ToLower().Contains(s) ||
                                         (a.Audit != null && a.Audit.AuditCode.ToLower().Contains(s)) ||
                                         a.CreatedBy!.FullName.ToLower().Contains(s));
            }

            if (warehouseId.HasValue) query = query.Where(a => a.WarehouseId == warehouseId.Value);
            if (status.HasValue && Enum.IsDefined(typeof(InventoryAdjustmentStatus), status.Value))
                query = query.Where(a => a.Status == (InventoryAdjustmentStatus)status.Value);
            if (reason.HasValue && Enum.IsDefined(typeof(InventoryAdjustmentReason), reason.Value))
                query = query.Where(a => a.Reason == (InventoryAdjustmentReason)reason.Value);

            if (startDate.HasValue) query = query.Where(a => a.AdjustmentDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(a => a.AdjustmentDate < endDate.Value.Date.AddDays(1));

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
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho.");

            return _mapper.Map<InventoryAdjustmentReadDto>(entity);
        }

        public async Task<int> CreateAsync(InventoryAdjustmentCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu điều chỉnh phải có ít nhất 1 dòng chi tiết.");

            var adjustment = new InventoryAdjustment
            {
                AdjustmentCode = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                WarehouseId = dto.WarehouseId,
                AuditId = dto.AuditId,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = dto.Reason,
                CreatedById = dto.CreatedById ?? 1,
                AdjustmentDate = DateTime.UtcNow,
                Note = dto.Note
            };

            decimal totalAmount = 0;

            foreach (var item in dto.Details)
            {
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
                    ReasonDetail = item.ReasonDetail
                });
            }

            adjustment.TotalVarianceAmount = totalAmount;

            _context.InventoryAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();

            return adjustment.Id;
        }

        public async Task<bool> ApproveAdjustmentAsync(int id, int approvedById)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var adj = await _context.InventoryAdjustments
                    .Include(a => a.Details)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (adj == null) throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho.");
                if (adj.Status != InventoryAdjustmentStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể duyệt phiếu điều chỉnh ở trạng thái Nháp.");

                adj.Status = InventoryAdjustmentStatus.Approved;
                adj.ApprovedById = approvedById;
                adj.ApprovedDate = DateTime.UtcNow;

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

                    TransactionType txnType = TransactionType.Adjustment;
                    decimal qtySign = detail.Quantity;

                    switch (detail.AdjustmentType)
                    {
                        case InventoryAdjustmentType.IncreaseAvailable:
                            inv.QuantityAvailable += detail.Quantity;
                            qtySign = detail.Quantity;
                            txnType = TransactionType.Adjustment;
                            break;

                        case InventoryAdjustmentType.DecreaseAvailable:
                            inv.QuantityAvailable = Math.Max(0, inv.QuantityAvailable - detail.Quantity);
                            qtySign = -detail.Quantity;
                            txnType = TransactionType.Adjustment;
                            break;

                        case InventoryAdjustmentType.MoveToDamaged:
                            inv.QuantityAvailable = Math.Max(0, inv.QuantityAvailable - detail.Quantity);
                            inv.QuantityDamaged += detail.Quantity;
                            qtySign = detail.Quantity;
                            txnType = TransactionType.Adjustment;
                            break;

                        case InventoryAdjustmentType.DisposeDamaged:
                            inv.QuantityDamaged = Math.Max(0, inv.QuantityDamaged - detail.Quantity);
                            qtySign = -detail.Quantity;
                            txnType = TransactionType.Adjustment;
                            break;
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = adj.WarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = txnType,
                        Quantity = qtySign,
                        ReferenceCode = adj.AdjustmentCode,
                        Note = $"Điều chỉnh kho ({adj.Reason}): {detail.ReasonDetail}",
                        CreatedById = approvedById
                    });
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

        public async Task<bool> CancelAsync(int id, string reason)
        {
            var adj = await _context.InventoryAdjustments.FindAsync(id);
            if (adj == null) throw new KeyNotFoundException("Không tìm thấy Phiếu điều chỉnh tồn kho.");
            if (adj.Status == InventoryAdjustmentStatus.Approved)
                throw new InvalidOperationException("Không thể hủy Phiếu điều chỉnh đã được duyệt.");

            adj.Status = InventoryAdjustmentStatus.Cancelled;
            adj.Note = $"Hủy phiếu: {reason}";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
