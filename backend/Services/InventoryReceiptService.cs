using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryReceiptDTOs;
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
    /// Service xử lý toàn bộ vòng đời Phiếu Nhập Kho & Kiểm định chất lượng hàng hóa (Goods Receipt Note - GRN).
    /// </summary>
    public class InventoryReceiptService : IInventoryReceiptService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryReceiptService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn (Query)
        /// <inheritdoc />
        public async Task<PagedResult<InventoryReceiptReadDto>> GetPagedAsync(
            string? search, int? warehouseId, int? supplierId, int? status, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.ReceivedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower().Trim();
                query = query.Where(x => x.ReceiptCode.ToLower().Contains(lowerSearch) ||
                                         (x.Supplier != null && x.Supplier.Name.ToLower().Contains(lowerSearch)) ||
                                         (x.Note != null && x.Note.ToLower().Contains(lowerSearch)));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            if (supplierId.HasValue && supplierId.Value > 0)
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

        /// <inheritdoc />
        public async Task<InventoryReceiptReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.ReceivedBy)
                .Include(x => x.Details).ThenInclude(d => d.Variant)
                .Include(x => x.Details).ThenInclude(d => d.Batch)
                .Include(x => x.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho hoặc bạn không có quyền truy cập kho này.");

            return _mapper.Map<InventoryReceiptReadDto>(entity);
        }
        #endregion

        #region Thao tác Dữ liệu & Quy trình (Command & Workflow)
        /// <inheritdoc />
        public async Task<int> CreateAsync(InventoryReceiptCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new InvalidOperationException("Phiếu nhập kho phải có ít nhất 1 dòng kiểm đếm hàng hóa.");

            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
            if (!warehouseExists)
                throw new InvalidOperationException($"Kho nhận hàng với ID {dto.WarehouseId} không tồn tại hoặc đã bị vô hiệu hóa.");

            if (dto.SupplierId.HasValue)
            {
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId.Value && !s.IsDeleted);
                if (!supplierExists)
                    throw new InvalidOperationException($"Nhà cung cấp với ID {dto.SupplierId.Value} không tồn tại.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<InventoryReceipt>(dto);

                entity.ReceiptCode = $"IR-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                entity.Status = InventoryReceiptStatus.Pending;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                _context.InventoryReceipts.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CompleteReceiptAsync(int id, int receivedById, string? note)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var receipt = await _context.InventoryReceipts
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status != InventoryReceiptStatus.Pending && receipt.Status != InventoryReceiptStatus.Inspecting)
                    throw new InvalidOperationException("Phiếu nhập kho phải ở trạng thái Chờ nhập kho (Pending) hoặc Đang kiểm tra (Inspecting) mới có thể hoàn tất.");

                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == receivedById && !u.IsDeleted);
                if (!userExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                    if (firstUser != null) receivedById = firstUser.Id;
                }

                receipt.Status = InventoryReceiptStatus.Completed;
                receipt.ReceivedById = receivedById;
                receipt.ReceiptDate = DateTime.UtcNow;
                receipt.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(note))
                {
                    receipt.Note = note.Trim();
                }

                var poIdsToUpdate = new HashSet<int>();

                foreach (var detail in receipt.Details)
                {
                    if (detail.AcceptedQuantity <= 0) continue;

                    // 1. Cập nhật két sắt tồn kho 4 ngăn (WarehouseInventory -> QuantityAvailable)
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
                            QuantityDamaged = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.WarehouseInventories.Add(inventory);
                    }
                    else
                    {
                        inventory.QuantityAvailable += detail.AcceptedQuantity;
                        inventory.UpdatedAt = DateTime.UtcNow;
                    }

                    // 2. Ghi sổ cái bất biến (InventoryTransaction)
                    var invTransaction = new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = receipt.WarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.Receipt,
                        Quantity = detail.AcceptedQuantity,
                        ReferenceCode = receipt.ReceiptCode,
                        Note = $"Nhập kho hoàn tất theo phiếu {receipt.ReceiptCode}",
                        CreatedById = receivedById,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.InventoryTransactions.Add(invTransaction);

                    // 3. Cập nhật tiến độ dòng PO Detail
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

                // 4. Cập nhật trạng thái Đơn mua hàng gốc (PO)
                foreach (var poId in poIdsToUpdate)
                {
                    var po = await _context.PurchaseOrders
                        .Include(x => x.Details)
                        .FirstOrDefaultAsync(x => x.Id == poId);

                    if (po != null && (po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.PartiallyReceived))
                    {
                        bool isFullyReceived = po.Details.All(d => d.ReceivedQuantity >= d.OrderQuantity);
                        po.Status = isFullyReceived ? PurchaseOrderStatus.Completed : PurchaseOrderStatus.PartiallyReceived;
                        po.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CancelReceiptAsync(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy phiếu nhập kho không được để trống.", nameof(reason));

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var receipt = await _context.InventoryReceipts.FindAsync(id);
                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status == InventoryReceiptStatus.Completed)
                    throw new InvalidOperationException("Không thể hủy Phiếu Nhập Kho đã hoàn tất vào sổ cái.");

                receipt.Status = InventoryReceiptStatus.Cancelled;
                receipt.CancellationReason = reason.Trim();
                receipt.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var receipt = await _context.InventoryReceipts.FindAsync(id);
                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status == InventoryReceiptStatus.Completed)
                    throw new InvalidOperationException("Không thể xóa Phiếu Nhập Kho đã hoàn tất vào sổ cái.");

                receipt.IsDeleted = true;
                receipt.DeletedAt = DateTime.UtcNow;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion
    }
}
