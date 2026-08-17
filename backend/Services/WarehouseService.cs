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

        public async Task<IEnumerable<WarehouseReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<WarehouseReadDto>>(items);
        }

        public async Task<PagedResult<WarehouseReadDto>> GetPagedAsync(string? search, bool? isActive, string? province, int pageIndex, int pageSize)
        {
            var query = _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .AsQueryable();

            // 1. Lọc theo Search (Mã kho, Tên kho)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(searchLower) || 
                                         x.Name.ToLower().Contains(searchLower));
            }

            // 2. Lọc theo Trạng thái
            if (isActive.HasValue) 
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 3. Lọc theo Tỉnh/Thành phố
            if (!string.IsNullOrWhiteSpace(province))
            {
                var provinceTrimmed = province.Trim().ToLower();
                query = query.Where(x => x.Address != null && x.Address.Province.ToLower() == provinceTrimmed);
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
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

            return _mapper.Map<WarehouseReadDto>(entity);
        }

        public async Task<int> CreateAsync(WarehouseCreateDto dto)
        {
            var trimmedCode = dto.Code.Trim();
            var trimmedName = dto.Name.Trim();

            // Kiểm tra trùng Mã Kho
            if (await _context.Warehouses.AnyAsync(x => x.Code.ToLower() == trimmedCode.ToLower()))
                throw new Exception($"Mã kho '{trimmedCode}' đã tồn tại trong hệ thống.");

            // Kiểm tra trùng Tên Kho
            if (await _context.Warehouses.AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower()))
                throw new Exception($"Tên kho '{trimmedName}' đã tồn tại trong hệ thống.");

            // Kiểm tra Trưởng kho nếu có chọn
            if (dto.ManagerId.HasValue && dto.ManagerId.Value > 0)
            {
                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == dto.ManagerId.Value && !u.IsDeleted);
                if (!userExists)
                    throw new Exception("Trưởng kho được chọn không tồn tại trong hệ thống.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = _mapper.Map<Warehouse>(dto);
                entity.Code = trimmedCode;
                entity.Name = trimmedName;
                entity.WarehouseType = dto.WarehouseType?.Trim();
                entity.ManagerId = (dto.ManagerId.HasValue && dto.ManagerId.Value > 0) ? dto.ManagerId : null;
                entity.IsActive = dto.IsActive;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                if (entity.Address != null)
                {
                    entity.Address.Province = dto.Address.Province.Trim();
                    entity.Address.District = dto.Address.District.Trim();
                    entity.Address.Ward = dto.Address.Ward.Trim();
                    entity.Address.StreetAddress = dto.Address.StreetAddress.Trim();
                    entity.Address.Latitude = dto.Address.Latitude;
                    entity.Address.Longitude = dto.Address.Longitude;
                    entity.Address.CreatedAt = DateTime.UtcNow;
                    entity.Address.UpdatedAt = DateTime.UtcNow;
                }

                _context.Warehouses.Add(entity);
                await _context.SaveChangesAsync();
                
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
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

                var trimmedName = dto.Name.Trim();

                // Kiểm tra trùng Tên Kho (trừ chính nó)
                if (await _context.Warehouses.AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower()))
                    throw new Exception($"Cập nhật thất bại: Tên kho '{trimmedName}' đã bị trùng.");

                // Kiểm tra Trưởng kho nếu có chọn
                if (dto.ManagerId.HasValue && dto.ManagerId.Value > 0)
                {
                    var userExists = await _context.IAUsers.AnyAsync(u => u.Id == dto.ManagerId.Value && !u.IsDeleted);
                    if (!userExists)
                        throw new Exception("Trưởng kho được chọn không tồn tại trong hệ thống.");
                }

                // 1. Map đè dữ liệu cơ bản của Kho
                _mapper.Map(dto, entity);
                entity.Name = trimmedName;
                entity.WarehouseType = dto.WarehouseType?.Trim();
                entity.ManagerId = (dto.ManagerId.HasValue && dto.ManagerId.Value > 0) ? dto.ManagerId : null;
                entity.IsActive = dto.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                // 2. Xử lý Map thủ công cho phần Address
                if (entity.Address != null)
                {
                    entity.Address.Province = dto.Address.Province.Trim();
                    entity.Address.District = dto.Address.District.Trim();
                    entity.Address.Ward = dto.Address.Ward.Trim();
                    entity.Address.StreetAddress = dto.Address.StreetAddress.Trim();
                    entity.Address.Latitude = dto.Address.Latitude;
                    entity.Address.Longitude = dto.Address.Longitude;
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
            var entity = await _context.Warehouses
                .Include(x => x.Address)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

            // 1. SAFETY SHIELD: Chặn xóa nếu còn tồn kho
            var hasStock = await _context.WarehouseInventories
                .AnyAsync(wi => wi.WarehouseId == id && (wi.QuantityAvailable + wi.QuantityReserved + wi.QuantityQC + wi.QuantityDamaged) > 0);
            if (hasStock)
                throw new Exception("Không thể xóa kho hàng này vì vẫn còn tồn kho sản phẩm.");

            // 2. SAFETY SHIELD: Chặn xóa nếu có Phiếu nhập kho
            var hasReceipts = await _context.InventoryReceipts.AnyAsync(ir => ir.WarehouseId == id);
            if (hasReceipts)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu nhập kho.");

            // 3. SAFETY SHIELD: Chặn xóa nếu có Phiếu xuất kho
            var hasIssues = await _context.InventoryIssues.AnyAsync(ii => ii.WarehouseId == id);
            if (hasIssues)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu xuất kho.");

            // 4. SAFETY SHIELD: Chặn xóa nếu có Phiếu chuyển kho
            var hasTransfers = await _context.InventoryTransfers
                .AnyAsync(it => it.FromWarehouseId == id || it.ToWarehouseId == id);
            if (hasTransfers)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu chuyển kho.");

            // 5. SAFETY SHIELD: Chặn xóa nếu có Phiếu kiểm kê
            var hasAudits = await _context.InventoryAudits.AnyAsync(ia => ia.WarehouseId == id);
            if (hasAudits)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu kiểm kê.");

            // 6. SAFETY SHIELD: Chặn xóa nếu có Phiếu điều chỉnh
            var hasAdjustments = await _context.InventoryAdjustments.AnyAsync(ia => ia.WarehouseId == id);
            if (hasAdjustments)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu điều chỉnh tồn kho.");

            // 7. SAFETY SHIELD: Chặn xóa nếu có Đơn hàng
            var hasOrders = await _context.Orders.AnyAsync(o => o.WarehouseId == id);
            if (hasOrders)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh đơn hàng bán xuất từ kho này.");

            // 8. SAFETY SHIELD: Chặn xóa nếu có Đơn trả hàng
            var hasReturns = await _context.CustomerReturns.AnyAsync(cr => cr.WarehouseId == id);
            if (hasReturns)
                throw new Exception("Không thể xóa kho hàng vì đã phát sinh đơn trả hàng nhập vào kho này.");

            // Thực hiện Soft Delete cả Kho và Địa chỉ
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entity.Address != null)
            {
                entity.Address.IsDeleted = true;
                entity.Address.DeletedAt = DateTime.UtcNow;
                entity.Address.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.Warehouses.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");
            
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}