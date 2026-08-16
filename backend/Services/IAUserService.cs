using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.AuthDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class IAUserService : IIAUserService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public IAUserService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<IAUserReadDto>> GetAllListAsync()
        {
            var items = await _context.IAUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserWarehouses)
                .Include(x => x.UserPermissions)
                .AsNoTracking()
                .OrderBy(x => x.FullName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<IAUserReadDto>>(items);
        }

        public async Task<PagedResult<IAUserReadDto>> GetPagedAsync(
            string? search, int? roleId, int? warehouseId, bool? isActive, int pageIndex, int pageSize)
        {
            var query = _context.IAUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserWarehouses)
                .Include(x => x.UserPermissions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(searchLower) ||
                    x.FullName.ToLower().Contains(searchLower) ||
                    x.PhoneNumber.Contains(searchLower));
            }

            if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

            // 🔥 Lọc nhân viên theo Phòng ban (Role) và Kho (Warehouse)
            if (roleId.HasValue)
                query = query.Where(x => x.UserRoles.Any(ur => ur.RoleId == roleId.Value));

            if (warehouseId.HasValue)
                query = query.Where(x => x.UserWarehouses.Any(uw => uw.WarehouseId == warehouseId.Value));

            var totalRecords = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<IAUserReadDto>
            {
                Items = _mapper.Map<IEnumerable<IAUserReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<IAUserReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.IAUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserWarehouses)
                .Include(x => x.UserPermissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy nhân viên.");

            return _mapper.Map<IAUserReadDto>(entity);
        }

        public async Task<int> CreateAsync(IAUserCreateDto dto)
        {
            // Kiểm tra trùng lặp (Username, CCCD, Email, SĐT)
            if (await _context.IAUsers.AnyAsync(x => x.Username == dto.Username || x.CitizenId == dto.CitizenId || x.Email == dto.Email || x.PhoneNumber == dto.PhoneNumber))
                throw new Exception("Dữ liệu định danh (Username, CCCD, Email hoặc SĐT) đã bị trùng lặp trong hệ thống.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = _mapper.Map<IAUser>(dto);

                // 🔥 Hash mật khẩu trước khi lưu
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                _context.IAUsers.Add(entity);
                await _context.SaveChangesAsync();

                // 1. Thêm Roles
                if (dto.RoleIds.Any()) _context.IAUserRoles.AddRange(dto.RoleIds.Select(rId => new IAUserRole { UserId = entity.Id, RoleId = rId }));

                // 2. Thêm Kho
                if (dto.WarehouseIds.Any()) _context.IAUserWarehouses.AddRange(dto.WarehouseIds.Select(wId => new IAUserWarehouse { UserId = entity.Id, WarehouseId = wId }));

                // 3. Thêm Ngoại lệ Phân quyền
                if (dto.CustomPermissions.Any()) _context.IAUserPermissions.AddRange(dto.CustomPermissions.Select(p => new IAUserPermission { UserId = entity.Id, PermissionId = p.PermissionId, IsGranted = p.IsGranted }));

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

        public async Task<bool> UpdateAsync(int id, IAUserUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.IAUsers
                    .Include(x => x.UserRoles)
                    .Include(x => x.UserWarehouses)
                    .Include(x => x.UserPermissions)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) throw new KeyNotFoundException("Không tìm thấy nhân viên.");

                // Kiểm tra trùng CCCD, Email, SĐT (trừ bản thân nó)
                if (await _context.IAUsers.AnyAsync(x => x.Id != id && (x.CitizenId == dto.CitizenId || x.Email == dto.Email || x.PhoneNumber == dto.PhoneNumber)))
                    throw new Exception("Dữ liệu định danh (CCCD, Email hoặc SĐT) đã bị trùng lặp với nhân viên khác.");

                _mapper.Map(dto, entity);

                // --- XÓA CŨ ---
                if (entity.UserRoles.Any()) _context.IAUserRoles.RemoveRange(entity.UserRoles);
                if (entity.UserWarehouses.Any()) _context.IAUserWarehouses.RemoveRange(entity.UserWarehouses);
                if (entity.UserPermissions.Any()) _context.IAUserPermissions.RemoveRange(entity.UserPermissions);

                // --- THÊM MỚI ---
                if (dto.RoleIds.Any()) _context.IAUserRoles.AddRange(dto.RoleIds.Select(rId => new IAUserRole { UserId = id, RoleId = rId }));
                if (dto.WarehouseIds.Any()) _context.IAUserWarehouses.AddRange(dto.WarehouseIds.Select(wId => new IAUserWarehouse { UserId = id, WarehouseId = wId }));
                if (dto.CustomPermissions.Any()) _context.IAUserPermissions.AddRange(dto.CustomPermissions.Select(p => new IAUserPermission { UserId = id, PermissionId = p.PermissionId, IsGranted = p.IsGranted }));

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

        public async Task<bool> ChangePasswordAsync(int id, IAUserChangePasswordDto dto)
        {
            var entity = await _context.IAUsers.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy nhân viên.");

            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.IAUsers.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy nhân viên.");
            _context.IAUsers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.IAUsers.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy nhân viên.");
            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}