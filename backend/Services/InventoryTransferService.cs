using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryTransferDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryTransferService : IInventoryTransferService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryTransferService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<InventoryTransferReadDto>> GetPagedAsync(
            string? search,
            int? fromWarehouseId,
            int? toWarehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.Order)
                .Include(t => t.CreatedBy)
                .Include(t => t.DispatchedBy)
                .Include(t => t.ReceivedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(t => allowedWarehouseIds.Contains(t.FromWarehouseId) || allowedWarehouseIds.Contains(t.ToWarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(t => t.TransferCode.ToLower().Contains(s) ||
                                         (t.Order != null && t.Order.OrderCode.ToLower().Contains(s)));
            }

            if (fromWarehouseId.HasValue) query = query.Where(t => t.FromWarehouseId == fromWarehouseId.Value);
            if (toWarehouseId.HasValue) query = query.Where(t => t.ToWarehouseId == toWarehouseId.Value);
            if (status.HasValue && Enum.IsDefined(typeof(InventoryTransferStatus), status.Value))
                query = query.Where(t => t.Status == (InventoryTransferStatus)status.Value);

            if (startDate.HasValue) query = query.Where(t => t.CreatedAt >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(t => t.CreatedAt < endDate.Value.Date.AddDays(1));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<InventoryTransferReadDto>
            {
                Items = _mapper.Map<IEnumerable<InventoryTransferReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<InventoryTransferReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.Order)
                .Include(t => t.CreatedBy)
                .Include(t => t.DispatchedBy)
                .Include(t => t.ReceivedBy)
                .Include(t => t.Details).ThenInclude(d => d.Variant)
                .Include(t => t.Details).ThenInclude(d => d.Batch)
                .Include(t => t.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(t => allowedWarehouseIds.Contains(t.FromWarehouseId) || allowedWarehouseIds.Contains(t.ToWarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");

            return _mapper.Map<InventoryTransferReadDto>(entity);
        }

        public async Task<int> CreateAsync(InventoryTransferCreateDto dto)
        {
            if (dto.FromWarehouseId == dto.ToWarehouseId)
                throw new ArgumentException("Kho nguồn và kho đích không được trùng nhau.");

            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu chuyển kho phải có ít nhất 1 dòng chi tiết.");

            var transfer = new InventoryTransfer
            {
                TransferCode = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                OrderId = dto.OrderId,
                Status = InventoryTransferStatus.Draft,
                CreatedById = dto.CreatedById ?? 1,
                Note = dto.Note
            };

            foreach (var item in dto.Details)
            {
                transfer.Details.Add(new InventoryTransferDetail
                {
                    VariantId = item.VariantId,
                    BatchId = item.BatchId,
                    UoMId = item.UoMId,
                    Quantity = item.Quantity
                });
            }

            _context.InventoryTransfers.Add(transfer);
            await _context.SaveChangesAsync();

            return transfer.Id;
        }

        public async Task<bool> DispatchTransferAsync(int id, int dispatchedById)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null) throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");
                if (transfer.Status != InventoryTransferStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể xuất phát phiếu chuyển đang ở trạng thái Nháp.");

                // 1. Trừ tồn kho tại Kho Nguồn & Ghi sổ cái TransferOut (Hàng chuyển sang trạng thái InTransit)
                foreach (var detail in transfer.Details)
                {
                    var sourceInv = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == transfer.FromWarehouseId &&
                                                  i.VariantId == detail.VariantId &&
                                                  i.BatchId == detail.BatchId);

                    if (sourceInv == null || sourceInv.QuantityAvailable < detail.Quantity)
                        throw new InvalidOperationException($"Kho nguồn không đủ số lượng khả dụng cho mặt hàng mã {detail.VariantId}, lô {detail.BatchId}.");

                    sourceInv.QuantityAvailable -= detail.Quantity;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = transfer.FromWarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.TransferOut,
                        Quantity = detail.Quantity,
                        ReferenceCode = transfer.TransferCode,
                        Note = $"Xuất chuyển kho sang {transfer.ToWarehouseId} (Phiếu {transfer.TransferCode})",
                        CreatedById = dispatchedById
                    });
                }

                transfer.Status = InventoryTransferStatus.InTransit;
                transfer.DispatchedById = dispatchedById;
                transfer.DispatchedDate = DateTime.UtcNow;

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

        public async Task<bool> ReceiveTransferAsync(int id, int receivedById)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null) throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");
                if (transfer.Status != InventoryTransferStatus.InTransit)
                    throw new InvalidOperationException("Phiếu chuyển phải ở trạng thái Đang vận chuyển (InTransit) mới có thể nhận vào kho đích.");

                // 2. Cộng tồn kho tại Kho Đích & Ghi sổ cái TransferIn
                foreach (var detail in transfer.Details)
                {
                    var targetInv = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == transfer.ToWarehouseId &&
                                                  i.VariantId == detail.VariantId &&
                                                  i.BatchId == detail.BatchId);

                    if (targetInv == null)
                    {
                        targetInv = new WarehouseInventory
                        {
                            WarehouseId = transfer.ToWarehouseId,
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId,
                            QuantityAvailable = detail.Quantity,
                            QuantityReserved = 0,
                            QuantityQC = 0,
                            QuantityDamaged = 0
                        };
                        _context.WarehouseInventories.Add(targetInv);
                    }
                    else
                    {
                        targetInv.QuantityAvailable += detail.Quantity;
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = transfer.ToWarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.TransferIn,
                        Quantity = detail.Quantity,
                        ReferenceCode = transfer.TransferCode,
                        Note = $"Nhận hàng chuyển từ kho {transfer.FromWarehouseId} (Phiếu {transfer.TransferCode})",
                        CreatedById = receivedById
                    });
                }

                transfer.Status = InventoryTransferStatus.Completed;
                transfer.ReceivedById = receivedById;
                transfer.ReceivedDate = DateTime.UtcNow;

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

        public async Task<bool> CancelTransferAsync(int id, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null) throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");
                if (transfer.Status == InventoryTransferStatus.Completed)
                    throw new InvalidOperationException("Không thể hủy Phiếu chuyển kho đã hoàn tất.");

                // Nếu hủy khi hàng đang đi đường (InTransit), hoàn trả lại tồn kho Kho Nguồn
                if (transfer.Status == InventoryTransferStatus.InTransit)
                {
                    foreach (var detail in transfer.Details)
                    {
                        var sourceInv = await _context.WarehouseInventories
                            .FirstOrDefaultAsync(i => i.WarehouseId == transfer.FromWarehouseId &&
                                                      i.VariantId == detail.VariantId &&
                                                      i.BatchId == detail.BatchId);

                        if (sourceInv != null)
                        {
                            sourceInv.QuantityAvailable += detail.Quantity;
                        }

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                            WarehouseId = transfer.FromWarehouseId,
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId,
                            Type = TransactionType.Adjustment,
                            Quantity = detail.Quantity,
                            ReferenceCode = transfer.TransferCode,
                            Note = $"Hoàn trả tồn kho do hủy chuyển kho {transfer.TransferCode}",
                            CreatedById = 1
                        });
                    }
                }

                transfer.Status = InventoryTransferStatus.Cancelled;
                transfer.CancellationReason = reason;

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
    }
}
