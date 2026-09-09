using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.AuthDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Dịch vụ xử lý nghiệp vụ quản lý tài khoản và hồ sơ nhân viên (IAM).
    /// </summary>
    public class IAUserService : IIAUserService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public IAUserService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn (Query)
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
            string? search,
            int? roleId,
            int? warehouseId,
            bool? isActive,
            int pageIndex,
            int pageSize)
        {
            var query = _context.IAUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserWarehouses)
                .Include(x => x.UserPermissions)
                .AsNoTracking()
                .AsQueryable();

            #region Bộ lọc tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(searchLower) ||
                    x.FullName.ToLower().Contains(searchLower) ||
                    x.PhoneNumber.Contains(searchLower) ||
                    x.CitizenId.Contains(searchLower) ||
                    x.Email.ToLower().Contains(searchLower));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // Lọc theo Vai trò (Role)
            if (roleId.HasValue)
            {
                query = query.Where(x => x.UserRoles.Any(ur => ur.RoleId == roleId.Value));
            }

            // Lọc theo Kho hàng được phân quyền (Warehouse)
            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.UserWarehouses.Any(uw => uw.WarehouseId == warehouseId.Value));
            }
            #endregion

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
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

            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy tài khoản nhân viên với ID = {id}.");
            }

            return _mapper.Map<IAUserReadDto>(entity);
        }
        #endregion

        #region Thao tác Dữ liệu (Command)
        public async Task<int> CreateAsync(IAUserCreateDto dto)
        {
            var normalizedUsername = dto.Username.Trim().ToLower();
            var normalizedEmail = dto.Email.Trim().ToLower();
            var normalizedPhone = dto.PhoneNumber.Trim();
            var normalizedCitizenId = dto.CitizenId.Trim();

            // Kiểm tra trùng lặp từng trường định danh để báo lỗi chi tiết
            if (await _context.IAUsers.AnyAsync(x => x.Username.ToLower() == normalizedUsername))
            {
                throw new InvalidOperationException($"Tên đăng nhập '{dto.Username}' đã tồn tại trong hệ thống.");
            }

            if (await _context.IAUsers.AnyAsync(x => x.Email.ToLower() == normalizedEmail))
            {
                throw new InvalidOperationException($"Email '{dto.Email}' đã được sử dụng bởi tài khoản khác.");
            }

            if (await _context.IAUsers.AnyAsync(x => x.CitizenId == normalizedCitizenId))
            {
                throw new InvalidOperationException($"Số CCCD '{dto.CitizenId}' đã tồn tại trong hệ thống.");
            }

            if (await _context.IAUsers.AnyAsync(x => x.PhoneNumber == normalizedPhone))
            {
                throw new InvalidOperationException($"Số điện thoại '{dto.PhoneNumber}' đã tồn tại trong hệ thống.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<IAUser>(dto);
                entity.Username = dto.Username.Trim();
                entity.Email = normalizedEmail;
                entity.PhoneNumber = normalizedPhone;
                entity.CitizenId = normalizedCitizenId;

                // Mã hóa mật khẩu bằng BCrypt
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                _context.IAUsers.Add(entity);
                await _context.SaveChangesAsync();

                // Gán danh sách vai trò ban đầu
                if (dto.RoleIds.Any())
                {
                    _context.IAUserRoles.AddRange(
                        dto.RoleIds.Distinct().Select(rId => new IAUserRole { UserId = entity.Id, RoleId = rId }));
                }

                // Gán danh sách kho phụ trách ban đầu
                if (dto.WarehouseIds.Any())
                {
                    _context.IAUserWarehouses.AddRange(
                        dto.WarehouseIds.Distinct().Select(wId => new IAUserWarehouse { UserId = entity.Id, WarehouseId = wId }));
                }

                // Thiết lập các quyền ngoại lệ ban đầu
                if (dto.CustomPermissions.Any())
                {
                    _context.IAUserPermissions.AddRange(
                        dto.CustomPermissions.Select(p => new IAUserPermission
                        {
                            UserId = entity.Id,
                            PermissionId = p.PermissionId,
                            IsGranted = p.IsGranted
                        }));
                }

                await _context.SaveChangesAsync();
                return entity.Id;
            });
        }

        public async Task<bool> UpdateAsync(int id, IAUserUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.IAUsers
                    .Include(x => x.UserRoles)
                    .Include(x => x.UserWarehouses)
                    .Include(x => x.UserPermissions)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy tài khoản nhân viên với ID = {id}.");
                }

                var normalizedEmail = dto.Email.Trim().ToLower();
                var normalizedPhone = dto.PhoneNumber.Trim();
                var normalizedCitizenId = dto.CitizenId.Trim();

                // Kiểm tra trùng lặp từng trường định danh với các tài khoản khác
                if (await _context.IAUsers.AnyAsync(x => x.Id != id && x.Email.ToLower() == normalizedEmail))
                {
                    throw new InvalidOperationException($"Email '{dto.Email}' đã được sử dụng bởi tài khoản khác.");
                }

                if (await _context.IAUsers.AnyAsync(x => x.Id != id && x.CitizenId == normalizedCitizenId))
                {
                    throw new InvalidOperationException($"Số CCCD '{dto.CitizenId}' đã tồn tại trong hệ thống.");
                }

                if (await _context.IAUsers.AnyAsync(x => x.Id != id && x.PhoneNumber == normalizedPhone))
                {
                    throw new InvalidOperationException($"Số điện thoại '{dto.PhoneNumber}' đã tồn tại trong hệ thống.");
                }

                // Bảo vệ tài khoản admin tối cao không bị khóa
                if (entity.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && !dto.IsActive)
                {
                    throw new InvalidOperationException("Không thể tạm khóa tài khoản quản trị viên tối cao (admin).");
                }

                _mapper.Map(dto, entity);
                entity.Email = normalizedEmail;
                entity.PhoneNumber = normalizedPhone;
                entity.CitizenId = normalizedCitizenId;

                #region Đồng bộ lại các quan hệ Nhiều - Nhiều
                // 1. Đồng bộ Vai trò (Roles)
                if (entity.UserRoles.Any())
                {
                    _context.IAUserRoles.RemoveRange(entity.UserRoles);
                }

                var targetRoleIds = dto.RoleIds?.Distinct().ToList() ?? new List<int>();

                // Bảo vệ tài khoản admin tối cao: Luôn giữ vai trò ADMIN
                if (entity.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    var adminRole = await _context.IARoles.FirstOrDefaultAsync(r => r.Code == "ADMIN");
                    if (adminRole != null && !targetRoleIds.Contains(adminRole.Id))
                    {
                        targetRoleIds.Add(adminRole.Id);
                    }
                    entity.IsActive = true;
                }

                if (targetRoleIds.Any())
                {
                    _context.IAUserRoles.AddRange(
                        targetRoleIds.Select(rId => new IAUserRole { UserId = id, RoleId = rId }));
                }

                // 2. Đồng bộ Kho hàng (Warehouses)
                if (entity.UserWarehouses.Any())
                {
                    _context.IAUserWarehouses.RemoveRange(entity.UserWarehouses);
                }
                if (dto.WarehouseIds.Any())
                {
                    _context.IAUserWarehouses.AddRange(
                        dto.WarehouseIds.Distinct().Select(wId => new IAUserWarehouse { UserId = id, WarehouseId = wId }));
                }

                // 3. Đồng bộ Quyền ngoại lệ (Custom Permissions)
                if (entity.UserPermissions.Any())
                {
                    _context.IAUserPermissions.RemoveRange(entity.UserPermissions);
                }
                if (dto.CustomPermissions.Any())
                {
                    _context.IAUserPermissions.AddRange(
                        dto.CustomPermissions.Select(p => new IAUserPermission
                        {
                            UserId = id,
                            PermissionId = p.PermissionId,
                            IsGranted = p.IsGranted
                        }));
                }
                #endregion

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> ChangePasswordAsync(int id, IAUserChangePasswordDto dto)
        {
            var entity = await _context.IAUsers.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy tài khoản nhân viên với ID = {id}.");
            }

            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.IAUsers.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy tài khoản nhân viên với ID = {id}.");
            }

            if (entity.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không thể xóa tài khoản quản trị viên tối cao (admin).");
            }

            // DbContext sẽ tự động chuyển thành Soft Delete (IsDeleted = true) trong SaveChangesAsync
            _context.IAUsers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.IAUsers.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy tài khoản nhân viên với ID = {id}.");
            }

            if (entity.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && entity.IsActive)
            {
                throw new InvalidOperationException("Không thể tạm khóa tài khoản quản trị viên tối cao (admin).");
            }

            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();
            return entity.IsActive;
        }
        #endregion
    }
}