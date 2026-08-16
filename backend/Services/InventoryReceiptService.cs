using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryReceiptDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryReceiptService : IInventoryReceiptService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryReceiptService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<InventoryReceiptReadDto>> GetPagedAsync(
            string? search, int? warehouseId, int? supplierId, int? status, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize)
        {
            var query = _context.InventoryReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.ReceivedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.ReceiptCode.ToLower().Contains(lowerSearch));
            }

            if (warehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            if (supplierId.HasValue)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            if (status.HasValue && Enum.IsDefined(typeof(InventoryReceiptStatus), status.Value))
                query = query.Where(x => x.Status == (InventoryReceiptStatus)status.Value);

            if (startDate.HasValue)
                query = query.Where(x => x.CreatedAt >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < end);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<InventoryReceiptReadDto>>(items);

            return new PagedResult<InventoryReceiptReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<InventoryReceiptReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.InventoryReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.ReceivedBy)
                .Include(x => x.Details).ThenInclude(d => d.Variant)
                .Include(x => x.Details).ThenInclude(d => d.Batch)
                .Include(x => x.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

            return _mapper.Map<InventoryReceiptReadDto>(entity);
        }

        public async Task<int> CreateAsync(InventoryReceiptCreateDto dto)
        {
            var entity = _mapper.Map<InventoryReceipt>(dto);

            // Generate ReceiptCode: IR-YYYYMMDD-HHMMSS
            entity.ReceiptCode = $"IR-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}";
            entity.Status = InventoryReceiptStatus.Pending;

            _context.InventoryReceipts.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> CompleteReceiptAsync(int id, int receivedById, string? note)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var receipt = await _context.InventoryReceipts
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status != InventoryReceiptStatus.Pending && receipt.Status != InventoryReceiptStatus.Inspecting)
                    throw new Exception("Phiếu nhập kho phải ở trạng thái Pending hoặc Inspecting mới có thể hoàn tất.");

                receipt.Status = InventoryReceiptStatus.Completed;
                receipt.ReceivedById = receivedById;
                receipt.ReceiptDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    receipt.Note = note;
                }

                var poIdsToUpdate = new HashSet<int>();

                foreach (var detail in receipt.Details)
                {
                    if (detail.AcceptedQuantity <= 0) continue;

                    // 1. Cập nhật tồn kho (WarehouseInventory)
                    var inventory = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(x => x.WarehouseId == receipt.WarehouseId && 
                                                  x.VariantId == detail.VariantId && 
                                                  x.BatchId == detail.BatchId);

                    if (inventory == null)
                    {
                        inventory = new WarehouseInventory
                        {
                            WarehouseId = receipt.WarehouseId,
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId,
                            QuantityAvailable = detail.AcceptedQuantity,
                            QuantityReserved = 0,
                            QuantityQC = 0,
                            QuantityDamaged = 0
                        };
                        _context.WarehouseInventories.Add(inventory);
                    }
                    else
                    {
                        inventory.QuantityAvailable += detail.AcceptedQuantity;
                    }

                    // 2. Ghi sổ cái (InventoryTransaction)
                    var invTransaction = new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0,4).ToUpper()}",
                        WarehouseId = receipt.WarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.Receipt,
                        Quantity = detail.AcceptedQuantity,
                        ReferenceCode = receipt.ReceiptCode,
                        Note = "Nhập kho hoàn tất",
                        CreatedById = receivedById
                    };
                    _context.InventoryTransactions.Add(invTransaction);

                    // 3. Cập nhật PO Detail
                    if (detail.PurchaseOrderDetailId.HasValue)
                    {
                        var poDetail = await _context.PurchaseOrderDetails
                            .Include(x => x.PurchaseOrder)
                            .FirstOrDefaultAsync(x => x.Id == detail.PurchaseOrderDetailId.Value);

                        if (poDetail != null)
                        {
                            poDetail.ReceivedQuantity += detail.AcceptedQuantity;
                            poIdsToUpdate.Add(poDetail.PurchaseOrderId);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // 4. Cập nhật trạng thái PO gốc
                foreach (var poId in poIdsToUpdate)
                {
                    var po = await _context.PurchaseOrders
                        .Include(x => x.Details)
                        .FirstOrDefaultAsync(x => x.Id == poId);

                    if (po != null && (po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.PartiallyReceived))
                    {
                        bool isFullyReceived = true;
                        foreach (var d in po.Details)
                        {
                            if (d.ReceivedQuantity < d.OrderQuantity)
                            {
                                isFullyReceived = false;
                                break;
                            }
                        }

                        po.Status = isFullyReceived ? PurchaseOrderStatus.Completed : PurchaseOrderStatus.PartiallyReceived;
                    }
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

        public async Task<bool> CancelReceiptAsync(int id, string reason)
        {
            var receipt = await _context.InventoryReceipts.FindAsync(id);
            if (receipt == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

            if (receipt.Status == InventoryReceiptStatus.Completed)
                throw new Exception("Không thể hủy Phiếu Nhập Kho đã hoàn tất.");

            receipt.Status = InventoryReceiptStatus.Cancelled;
            receipt.CancellationReason = reason;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
