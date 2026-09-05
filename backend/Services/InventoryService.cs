using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryDTOs;
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
    /// Core Service quản lý Tồn kho 4 ngăn (Available, Reserved, QC, Damaged) và Sổ cái Giao dịch (Inventory Ledger).
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn & Báo cáo Tồn kho (Query & Reporting)
        /// <inheritdoc />
        public async Task<IEnumerable<InventoryReadDto>> GetAllListAsync(List<int>? allowedWarehouseIds = null)
        {
            var query = _context.WarehouseInventories
                .Include(x => x.Warehouse)
                .Include(x => x.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(x => x.Batch!).ThenInclude(b => b.Supplier)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            var items = await query.ToListAsync();
            return _mapper.Map<IEnumerable<InventoryReadDto>>(items);
        }

        /// <inheritdoc />
        public async Task<PagedResult<InventoryReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            bool? isExpiringSoon,
            bool? isOutOfStock,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.WarehouseInventories
                .Include(x => x.Warehouse)
                .Include(x => x.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(x => x.Batch!).ThenInclude(b => b.Supplier)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(x =>
                    (x.Variant != null && x.Variant.Code.ToLower().Contains(s)) ||
                    (x.Variant != null && x.Variant.Name.ToLower().Contains(s)) ||
                    (x.Batch != null && x.Batch.BatchCode.ToLower().Contains(s))
                );
            }

            if (isExpiringSoon.HasValue && isExpiringSoon.Value)
            {
                var alertDate = DateTime.UtcNow.AddDays(7);
                query = query.Where(x => x.Batch != null && x.Batch.ExpiryDate <= alertDate && x.QuantityAvailable > 0);
            }

            if (isOutOfStock.HasValue && isOutOfStock.Value)
            {
                query = query.Where(x => x.QuantityAvailable == 0 && x.QuantityReserved == 0);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Batch != null ? x.Batch.ExpiryDate : DateTime.MaxValue)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<InventoryReadDto>
            {
                Items = _mapper.Map<IEnumerable<InventoryReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<InventoryReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.WarehouseInventories
                .Include(x => x.Warehouse)
                .Include(x => x.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(x => x.Batch!).ThenInclude(b => b.Supplier)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy dữ liệu tồn kho hoặc bạn không có quyền truy cập kho này.");

            return _mapper.Map<InventoryReadDto>(entity);
        }
        #endregion

        #region Core Engine - Biến động Tồn kho (Commands)
        /// <inheritdoc />
        public async Task IncreaseAvailableAsync(int warehouseId, int variantId, int batchId, decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Số lượng nhập kho phải lớn hơn 0.", nameof(quantity));

            await _context.ExecuteInTransactionAsync(async () =>
            {
                var inventory = await _context.WarehouseInventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.VariantId == variantId && x.BatchId == batchId);

                if (inventory == null)
                {
                    inventory = new WarehouseInventory
                    {
                        WarehouseId = warehouseId,
                        VariantId = variantId,
                        BatchId = batchId,
                        QuantityAvailable = quantity,
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
                    inventory.QuantityAvailable += quantity;
                    inventory.UpdatedAt = DateTime.UtcNow;
                }

                var txn = new InventoryTransaction
                {
                    TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                    WarehouseId = warehouseId,
                    VariantId = variantId,
                    BatchId = batchId,
                    Type = TransactionType.Receipt,
                    Quantity = quantity,
                    ReferenceCode = null,
                    Note = "Cộng tồn kho khả dụng qua Core Engine",
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryTransactions.Add(txn);

                await _context.SaveChangesAsync();
            });
        }

        /// <inheritdoc />
        public async Task ReserveInventoryAsync(int warehouseId, int variantId, int batchId, decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Số lượng giữ chỗ phải lớn hơn 0.", nameof(quantity));

            await _context.ExecuteInTransactionAsync(async () =>
            {
                var inventory = await _context.WarehouseInventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.VariantId == variantId && x.BatchId == batchId);

                if (inventory == null || inventory.QuantityAvailable < quantity)
                    throw new InvalidOperationException($"Không đủ tồn kho khả dụng để giữ chỗ (Khả dụng: {inventory?.QuantityAvailable ?? 0}, Yêu cầu: {quantity}).");

                inventory.QuantityAvailable -= quantity;
                inventory.QuantityReserved += quantity;
                inventory.UpdatedAt = DateTime.UtcNow;

                var txn = new InventoryTransaction
                {
                    TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                    WarehouseId = warehouseId,
                    VariantId = variantId,
                    BatchId = batchId,
                    Type = TransactionType.Reserve,
                    Quantity = quantity,
                    ReferenceCode = null,
                    Note = "Giữ chỗ hàng hóa cho đơn đặt hàng",
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryTransactions.Add(txn);

                await _context.SaveChangesAsync();
            });
        }

        /// <inheritdoc />
        public async Task IssueReservedAsync(int warehouseId, int variantId, int batchId, decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Số lượng xuất kho phải lớn hơn 0.", nameof(quantity));

            await _context.ExecuteInTransactionAsync(async () =>
            {
                var inventory = await _context.WarehouseInventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.VariantId == variantId && x.BatchId == batchId);

                if (inventory == null)
                    throw new InvalidOperationException("Không tìm thấy dòng tồn kho tương ứng để xuất hàng.");

                if (inventory.QuantityReserved >= quantity)
                {
                    inventory.QuantityReserved -= quantity;
                }
                else
                {
                    var diff = quantity - inventory.QuantityReserved;
                    if (inventory.QuantityAvailable < diff)
                        throw new InvalidOperationException($"Không đủ tồn kho (Khả dụng + Giữ chỗ) để xuất hàng. Thiếu: {diff - inventory.QuantityAvailable}.");

                    inventory.QuantityReserved = 0;
                    inventory.QuantityAvailable -= diff;
                }

                inventory.UpdatedAt = DateTime.UtcNow;

                var txn = new InventoryTransaction
                {
                    TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                    WarehouseId = warehouseId,
                    VariantId = variantId,
                    BatchId = batchId,
                    Type = TransactionType.Issue,
                    Quantity = quantity,
                    ReferenceCode = null,
                    Note = "Xuất kho thực tế trừ tồn kho",
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryTransactions.Add(txn);

                await _context.SaveChangesAsync();
            });
        }

        /// <inheritdoc />
        public async Task ReceiveCustomerReturnAsync(int warehouseId, int variantId, int batchId, decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Số lượng hàng hoàn trả phải lớn hơn 0.", nameof(quantity));

            await _context.ExecuteInTransactionAsync(async () =>
            {
                var inventory = await _context.WarehouseInventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.VariantId == variantId && x.BatchId == batchId);

                if (inventory == null)
                {
                    inventory = new WarehouseInventory
                    {
                        WarehouseId = warehouseId,
                        VariantId = variantId,
                        BatchId = batchId,
                        QuantityAvailable = 0,
                        QuantityReserved = 0,
                        QuantityQC = quantity,
                        QuantityDamaged = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.WarehouseInventories.Add(inventory);
                }
                else
                {
                    inventory.QuantityQC += quantity;
                    inventory.UpdatedAt = DateTime.UtcNow;
                }

                var txn = new InventoryTransaction
                {
                    TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                    WarehouseId = warehouseId,
                    VariantId = variantId,
                    BatchId = batchId,
                    Type = TransactionType.CustomerReturn,
                    Quantity = quantity,
                    ReferenceCode = null,
                    Note = "Nhận hàng khách hoàn trả vào ngăn QC kiểm định",
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryTransactions.Add(txn);

                await _context.SaveChangesAsync();
            });
        }
        #endregion
    }
}