using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryTransferDTOs;
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
    /// Service quản lý Phiếu Điều Chuyển Hàng Liên Kho 2 Bước (Dispatch -> Receive) và tính toán biến động sổ cái.
    /// </summary>
    public class InventoryTransferService : IInventoryTransferService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUoMConversionService _uomConversionService;

        public InventoryTransferService(SolarisDbContext context, IMapper mapper, IUoMConversionService? uomConversionService = null)
        {
            _context = context;
            _mapper = mapper;
            _uomConversionService = uomConversionService ?? new UoMConversionService(context, mapper);
        }

        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <inheritdoc />
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
                var s = search.ToLower().Trim();
                query = query.Where(t => t.TransferCode.ToLower().Contains(s) ||
                                         (t.Order != null && t.Order.OrderCode.ToLower().Contains(s)) ||
                                         (t.Note != null && t.Note.ToLower().Contains(s)));
            }

            if (fromWarehouseId.HasValue && fromWarehouseId.Value > 0)
                query = query.Where(t => t.FromWarehouseId == fromWarehouseId.Value);

            if (toWarehouseId.HasValue && toWarehouseId.Value > 0)
                query = query.Where(t => t.ToWarehouseId == toWarehouseId.Value);

            if (status.HasValue && Enum.IsDefined(typeof(InventoryTransferStatus), status.Value))
                query = query.Where(t => t.Status == (InventoryTransferStatus)status.Value);

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt < endDate.Value.Date.AddDays(1));

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

        /// <inheritdoc />
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
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");

            return _mapper.Map<InventoryTransferReadDto>(entity);
        }
        #endregion

        #region Khởi tạo & Hủy lệnh (Command)
        /// <inheritdoc />
        public async Task<int> CreateAsync(InventoryTransferCreateDto dto)
        {
            if (dto.FromWarehouseId == dto.ToWarehouseId)
                throw new InvalidOperationException("Kho nguồn và kho đích không được trùng nhau.");

            if (dto.Details == null || !dto.Details.Any())
                throw new InvalidOperationException("Phiếu chuyển kho phải có ít nhất 1 dòng chi tiết hàng hóa.");

            var fromWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.FromWarehouseId && !w.IsDeleted);
            if (fromWarehouse == null)
                throw new InvalidOperationException($"Kho nguồn với ID {dto.FromWarehouseId} không tồn tại hoặc đã bị vô hiệu hóa.");

            var toWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.ToWarehouseId && !w.IsDeleted);
            if (toWarehouse == null)
                throw new InvalidOperationException($"Kho đích với ID {dto.ToWarehouseId} không tồn tại hoặc đã bị vô hiệu hóa.");

            // SAFETY SHIELD: Không được phép điều chuyển từ Kho Hàng Lỗi sang các kho khác
            if (!string.IsNullOrWhiteSpace(fromWarehouse.WarehouseType) && fromWarehouse.WarehouseType == WarehouseTypeConstants.Damaged)
                throw new InvalidOperationException("Hàng hóa trong Kho Hàng Lỗi không được phép điều chuyển sang các kho vận hành khác.");

            int safeCreatedById = dto.CreatedById ?? 1;
            var userExists = await _context.IAUsers.AnyAsync(u => u.Id == safeCreatedById && !u.IsDeleted);
            if (!userExists)
            {
                var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                safeCreatedById = firstUser?.Id ?? 1;
            }

            // SAFETY SHIELD: Kiểm tra tồn kho khả dụng tại Kho Nguồn cho tất cả mặt hàng/lô hàng trước khi tạo lệnh
            var groupedDemands = new Dictionary<(int VariantId, int BatchId), decimal>();
            foreach (var detail in dto.Details)
            {
                if (detail.Quantity <= 0)
                    throw new InvalidOperationException("Số lượng chuyển kho phải lớn hơn 0.");

                decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.Quantity);
                var key = (detail.VariantId, detail.BatchId);
                if (!groupedDemands.ContainsKey(key))
                    groupedDemands[key] = 0;
                groupedDemands[key] += baseQty;
            }

            foreach (var (key, neededBaseQty) in groupedDemands)
            {
                var sourceInv = await _context.WarehouseInventories
                    .Include(i => i.Variant)
                    .Include(i => i.Batch)
                    .FirstOrDefaultAsync(i => i.WarehouseId == dto.FromWarehouseId &&
                                              i.VariantId == key.VariantId &&
                                              i.BatchId == key.BatchId);

                if (sourceInv == null || sourceInv.QuantityAvailable < neededBaseQty)
                {
                    var variantName = sourceInv?.Variant?.Name ?? $"mã SKU {key.VariantId}";
                    var batchCode = sourceInv?.Batch?.BatchCode ?? $"Lô #{key.BatchId}";
                    var available = sourceInv?.QuantityAvailable ?? 0;
                    throw new InvalidOperationException($"Kho nguồn không đủ số lượng tồn kho khả dụng cho mặt hàng {variantName} (Lô {batchCode}). Tồn khả dụng hiện có: {available}, yêu cầu chuyển: {neededBaseQty} (theo ĐVT cơ sở).");
                }
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var transfer = _mapper.Map<InventoryTransfer>(dto);

                transfer.TransferCode = $"TRF-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                transfer.Status = InventoryTransferStatus.Draft;
                transfer.CreatedById = safeCreatedById;
                transfer.CreatedAt = DateTime.UtcNow;
                transfer.UpdatedAt = DateTime.UtcNow;
                transfer.IsDeleted = false;

                _context.InventoryTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                return transfer.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CancelTransferAsync(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy phiếu điều chuyển không được để trống.", nameof(reason));

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");

                if (transfer.Status != InventoryTransferStatus.Draft)
                    throw new InvalidOperationException("Chỉ được phép hủy khi phiếu chuyển kho đang ở trạng thái Nháp (Draft).");

                transfer.Status = InventoryTransferStatus.Cancelled;
                transfer.CancellationReason = reason.Trim();
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion

        #region Quy trình 2 bước: Điều chuyển vật lý (2-Step Workflow)
        /// <inheritdoc />
        public async Task<bool> DispatchTransferAsync(int id, int dispatchedById)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");

                if (transfer.Status != InventoryTransferStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể xuất phát phiếu chuyển đang ở trạng thái Nháp (Draft).");

                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == dispatchedById && !u.IsDeleted);
                if (!userExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                    dispatchedById = firstUser?.Id ?? 1;
                }

                // 1. Trừ tồn kho tại Kho Nguồn & Ghi sổ cái TransferOut (quy đổi về Base UoM)
                foreach (var detail in transfer.Details)
                {
                    decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.Quantity);

                    var sourceInv = await _context.WarehouseInventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == transfer.FromWarehouseId &&
                                                  i.VariantId == detail.VariantId &&
                                                  i.BatchId == detail.BatchId);

                    if (sourceInv == null || sourceInv.QuantityAvailable < baseQty)
                        throw new InvalidOperationException($"Kho nguồn không đủ số lượng khả dụng cho mặt hàng mã {detail.VariantId}, lô {detail.BatchId} (Hiện có: {sourceInv?.QuantityAvailable ?? 0}, Cần xuất: {baseQty} theo ĐVT cơ sở).");

                    sourceInv.QuantityAvailable -= baseQty;
                    sourceInv.UpdatedAt = DateTime.UtcNow;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = transfer.FromWarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.TransferOut,
                        Quantity = baseQty,
                        ReferenceCode = transfer.TransferCode,
                        Note = $"Xuất chuyển kho sang kho #{transfer.ToWarehouseId} (Phiếu {transfer.TransferCode}, {detail.Quantity} ĐVT -> {baseQty} Base UoM)",
                        CreatedById = dispatchedById,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                transfer.Status = InventoryTransferStatus.InTransit;
                transfer.DispatchedById = dispatchedById;
                transfer.DispatchedDate = DateTime.UtcNow;
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> ReceiveTransferAsync(int id, int receivedById)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var transfer = await _context.InventoryTransfers
                    .Include(t => t.Details)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu chuyển kho.");

                if (transfer.Status != InventoryTransferStatus.InTransit)
                    throw new InvalidOperationException("Phiếu chuyển phải ở trạng thái Đang vận chuyển (InTransit) mới có thể nhận vào kho đích.");

                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == receivedById && !u.IsDeleted);
                if (!userExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                    receivedById = firstUser?.Id ?? 1;
                }

                // 2. Cộng tồn kho tại Kho Đích & Ghi sổ cái TransferIn (quy đổi về Base UoM)
                foreach (var detail in transfer.Details)
                {
                    decimal baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.Quantity);

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
                            QuantityAvailable = baseQty,
                            QuantityReserved = 0,
                            QuantityQC = 0,
                            QuantityDamaged = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.WarehouseInventories.Add(targetInv);
                    }
                    else
                    {
                        targetInv.QuantityAvailable += baseQty;
                        targetInv.UpdatedAt = DateTime.UtcNow;
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                        WarehouseId = transfer.ToWarehouseId,
                        VariantId = detail.VariantId,
                        BatchId = detail.BatchId,
                        Type = TransactionType.TransferIn,
                        Quantity = baseQty,
                        ReferenceCode = transfer.TransferCode,
                        Note = $"Nhận hàng chuyển từ kho #{transfer.FromWarehouseId} (Phiếu {transfer.TransferCode}, {detail.Quantity} ĐVT -> {baseQty} Base UoM)",
                        CreatedById = receivedById,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                transfer.Status = InventoryTransferStatus.Completed;
                transfer.ReceivedById = receivedById;
                transfer.ReceivedDate = DateTime.UtcNow;
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion
    }
}
