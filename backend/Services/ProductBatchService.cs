using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductBatchDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ProductBatchService : IProductBatchService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductBatchService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductBatchReadDto>> GetAllListAsync()
        {
            var items = await _context.ProductBatches
                .Include(x => x.Variant) // Móc tên Biến thể
                .Include(x => x.Supplier) // Móc tên Nhà cung cấp
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductBatchReadDto>>(items);
        }

        public async Task<PagedResult<ProductBatchReadDto>> GetPagedAsync(
            string? search,
            int? variantId,
            int? supplierId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ProductBatches
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsQueryable();

            // 1. Filter: Tìm kiếm theo Mã lô
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.BatchCode.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Theo Biến thể sản phẩm
            if (variantId.HasValue)
                query = query.Where(x => x.VariantId == variantId.Value);

            // 3. Filter: Theo Nhà cung cấp
            if (supplierId.HasValue)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            // 4. Filter: Trạng thái
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // 5. Filter: Ngày tạo
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            if (updatedAt.HasValue)
            {
                var startDate = updatedAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.UpdatedAt >= startDate && x.UpdatedAt < endDate);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<ProductBatchReadDto>>(items);

            return new PagedResult<ProductBatchReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<ProductBatchReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.ProductBatches
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy lô hàng.");

            return _mapper.Map<ProductBatchReadDto>(entity);
        }

        public async Task<int> CreateAsync(ProductBatchCreateDto dto)
        {
            // 🔥 Business Rule: Validate NSX & HSD
            if (dto.ExpiryDate <= dto.ManufactureDate)
                throw new Exception("Hạn sử dụng phải lớn hơn Ngày sản xuất.");

            // 🔥 Bắt lỗi trùng Mã Lô
            if (await _context.ProductBatches.AnyAsync(x => x.BatchCode.ToLower() == dto.BatchCode.ToLower()))
                throw new Exception($"Mã lô hàng '{dto.BatchCode}' đã tồn tại trong hệ thống.");

            var entity = _mapper.Map<ProductBatch>(dto);

            _context.ProductBatches.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, ProductBatchUpdateDto dto)
        {
            var entity = await _context.ProductBatches.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy lô hàng cần sửa.");

            // 🔥 Business Rule: Validate NSX & HSD
            if (dto.ExpiryDate <= dto.ManufactureDate)
                throw new Exception("Hạn sử dụng phải lớn hơn Ngày sản xuất.");

            // Kiểm tra trùng Mã lô (Trừ bản thân nó)
            if (await _context.ProductBatches.AnyAsync(x => x.Id != id && x.BatchCode.ToLower() == dto.BatchCode.ToLower()))
                throw new Exception($"Cập nhật thất bại: Mã lô '{dto.BatchCode}' đã bị trùng lặp.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductBatches.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy lô hàng để xóa.");

            _context.ProductBatches.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.ProductBatches.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy lô hàng.");

            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}