using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public InventoryService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<InventoryReadDto>> GetAllListAsync(List<int>? allowedWarehouseIds = null)
        {
            var query = _context.WarehouseInventories
                .Include(x => x.Warehouse)
                .Include(x => x.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(x => x.Batch!).ThenInclude(b => b.Supplier)
                .AsNoTracking()
                .AsQueryable();

            // 🔥 CHÌA KHÓA PHÂN QUYỀN Ở ĐÂY
            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            var items = await query.ToListAsync();
            return _mapper.Map<IEnumerable<InventoryReadDto>>(items);
        }

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

            // 🔥 CHÌA KHÓA PHÂN QUYỀN Ở ĐÂY
            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            // 1. Lọc theo Kho (Từ UI)
            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            }

            // 2. Lọc theo Search (Mã SKU, Tên SP, Lô)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(x =>
                    x.Variant!.Code.ToLower().Contains(s) ||
                    x.Variant.Name.ToLower().Contains(s) ||
                    x.Batch!.BatchCode.ToLower().Contains(s)
                );
            }

            // 3. Lọc Nông sản: Sắp hết hạn (<= 7 ngày)
            if (isExpiringSoon.HasValue && isExpiringSoon.Value)
            {
                var alertDate = DateTime.UtcNow.AddDays(7);
                query = query.Where(x => x.Batch!.ExpiryDate <= alertDate && x.QuantityAvailable > 0);
            }

            // 4. Lọc hết hàng
            if (isOutOfStock.HasValue && isOutOfStock.Value)
            {
                query = query.Where(x => x.QuantityAvailable == 0 && x.QuantityReserved == 0);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Batch!.ExpiryDate) // Ưu tiên hàng sát Date lên đầu
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

        public async Task<InventoryReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.WarehouseInventories
                .Include(x => x.Warehouse)
                .Include(x => x.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.BaseUoM)
                .Include(x => x.Batch!).ThenInclude(b => b.Supplier)
                .AsNoTracking()
                .AsQueryable();

            // Phân quyền: Cố tình gõ ID trên URL để xem trộm cũng bị chặn
            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu tồn kho hoặc bạn không có quyền truy cập kho này.");
            return _mapper.Map<InventoryReadDto>(entity);
        }

        // ... Các hàm Tăng_Tồn, Trừ_Tồn, Giữ_Chỗ sếp giữ nguyên như phiên bản trước nhé ...
        public async Task IncreaseAvailableAsync(int warehouseId, int variantId, int batchId, decimal quantity) { /*...*/ }
        public async Task ReserveInventoryAsync(int warehouseId, int variantId, int batchId, decimal quantity) { /*...*/ }
        public async Task IssueReservedAsync(int warehouseId, int variantId, int batchId, decimal quantity) { /*...*/ }
        public async Task ReceiveCustomerReturnAsync(int warehouseId, int variantId, int batchId, decimal quantity) { /*...*/ }
    }
}