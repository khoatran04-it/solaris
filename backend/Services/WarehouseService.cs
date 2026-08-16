using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public WarehouseService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<WarehouseReadDto>> GetAllListAsync()
        {
            var items = await _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<WarehouseReadDto>>(items);
        }

        public async Task<PagedResult<WarehouseReadDto>> GetPagedAsync(string? search, bool? isActive, string? province, int pageIndex, int pageSize)
        {
            // Bắt buộc Include Address và Manager để AutoMapper có dữ liệu bóc tách
            var query = _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            // 1. Lọc theo Search (Mã kho, Tên kho)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(searchLower) || 
                                         x.Name.ToLower().Contains(searchLower));
            }

            // 2. Lọc theo Trạng thái
            if (isActive.HasValue) 
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 3. 🔥 Lọc theo Tỉnh/Thành phố (Chọc qua bảng Address)
            if (!string.IsNullOrWhiteSpace(province))
            {
                query = query.Where(x => x.Address != null && x.Address.Province == province);
            }

            var totalRecords = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<WarehouseReadDto>
            {
                Items = _mapper.Map<IEnumerable<WarehouseReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<WarehouseReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

            return _mapper.Map<WarehouseReadDto>(entity);
        }

        public async Task<int> CreateAsync(WarehouseCreateDto dto)
        {
            // Kiểm tra trùng Mã Kho
            if (await _context.Warehouses.AnyAsync(x => x.Code == dto.Code && !x.IsDeleted))
                throw new Exception("Mã Kho đã tồn tại trong hệ thống.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // AutoMapper sẽ tự động map cả đối tượng AddressPayload thành WarehouseAddress 
                // và gán vào thuộc tính Address của Warehouse
                var entity = _mapper.Map<Warehouse>(dto);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                if (entity.Address != null)
                {
                    entity.Address.CreatedAt = DateTime.UtcNow;
                    entity.Address.UpdatedAt = DateTime.UtcNow;
                }

                _context.Warehouses.Add(entity);
                await _context.SaveChangesAsync(); // Lưu 1 phát ăn luôn 2 bảng
                
                await transaction.CommitAsync();
                return entity.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, WarehouseUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.Warehouses
                    .Include(x => x.Address)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

                // 1. Map đè dữ liệu cơ bản của Kho (Đã cấu hình Ignore Id, Code, Address)
                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                // 2. 🔥 Xử lý Map thủ công cho phần Address
                if (entity.Address != null)
                {
                    _mapper.Map(dto.Address, entity.Address);
                    entity.Address.UpdatedAt = DateTime.UtcNow;
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

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Warehouses.FindAsync(id);
            if (entity == null || entity.IsDeleted) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");
            
            // Thực hiện Soft Delete
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.Warehouses.FindAsync(id);
            if (entity == null || entity.IsDeleted) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");
            
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}