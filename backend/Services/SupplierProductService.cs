using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.SupplierProductDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class SupplierProductService : ISupplierProductService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public SupplierProductService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SupplierProductReadDto>> GetAllListAsync()
        {
            var items = await _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierProductReadDto>>(items);
        }

        public async Task<PagedResult<SupplierProductReadDto>> GetPagedAsync(
            string? search,
            int? variantId,
            int? supplierId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsQueryable();

            // 1. Filter: Tìm theo mã SupplierSKU
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.SupplierSKU != null && x.SupplierSKU.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Theo Biến thể (Dùng để hiển thị: Sản phẩm này có thể nhập từ ai?)
            if (variantId.HasValue)
                query = query.Where(x => x.VariantId == variantId.Value);

            // 3. Filter: Theo Nhà cung cấp (Dùng để hiển thị: Nhà cung cấp này đang bán những gì?)
            if (supplierId.HasValue)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            // 4. Filter: Trạng thái
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // 5. Filter: Ngày tháng
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

            var dtos = _mapper.Map<IEnumerable<SupplierProductReadDto>>(items);

            return new PagedResult<SupplierProductReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<SupplierProductReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin giá nhập.");

            return _mapper.Map<SupplierProductReadDto>(entity);
        }

        public async Task<int> CreateAsync(SupplierProductCreateDto dto)
        {
            // 🔥 Business Rule: 1 Sản phẩm - 1 NCC chỉ được phép có 1 dòng cấu hình giá
            var isDuplicate = await _context.SupplierProducts
                .AnyAsync(x => x.VariantId == dto.VariantId && x.SupplierId == dto.SupplierId);

            if (isDuplicate)
                throw new Exception("Nhà cung cấp này đã có cấu hình giá nhập cho sản phẩm này. Hãy cập nhật dòng dữ liệu hiện tại thay vì tạo mới.");

            var entity = _mapper.Map<SupplierProduct>(dto);

            _context.SupplierProducts.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, SupplierProductUpdateDto dto)
        {
            var entity = await _context.SupplierProducts.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình giá nhập cần sửa.");

            // Bắt lỗi trùng lặp khi người dùng sửa Variant hoặc Supplier (ngoại trừ dòng hiện tại)
            var isDuplicate = await _context.SupplierProducts
                .AnyAsync(x => x.Id != id && x.VariantId == dto.VariantId && x.SupplierId == dto.SupplierId);

            if (isDuplicate)
                throw new Exception("Cập nhật thất bại: Cấu hình liên kết giữa Sản phẩm và Nhà cung cấp này đã tồn tại.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.SupplierProducts.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình giá nhập để xóa.");

            _context.SupplierProducts.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.SupplierProducts.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình giá nhập.");

            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}