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
    /// Dịch vụ xử lý nghiệp vụ quản lý Vai trò (Roles) và cấu hình phân quyền (RBAC).
    /// </summary>
    public class IARoleService : IIARoleService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public IARoleService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn (Query)
        public async Task<IEnumerable<IARoleReadDto>> GetAllListAsync()
        {
            var items = await _context.IARoles
                .Include(x => x.RolePermissions)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<IARoleReadDto>>(items);
        }

        public async Task<PagedResult<IARoleReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            int pageIndex,
            int pageSize)
        {
            var query = _context.IARoles
                .Include(x => x.RolePermissions)
                .AsNoTracking()
                .AsQueryable();

            #region Bộ lọc tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(searchLower) ||
                    x.Name.ToLower().Contains(searchLower));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }
            #endregion

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<IARoleReadDto>
            {
                Items = _mapper.Map<IEnumerable<IARoleReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<IARoleReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.IARoles
                .Include(x => x.RolePermissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy vai trò với ID = {id}.");
            }

            return _mapper.Map<IARoleReadDto>(entity);
        }
        #endregion

        #region Thao tác Dữ liệu (Command)
        public async Task<int> CreateAsync(IARoleCreateDto dto)
        {
            // Kiểm tra trùng mã vai trò (Code không phân biệt hoa thường)
            var normalizedCode = dto.Code.Trim().ToUpper();
            bool isCodeExists = await _context.IARoles.AnyAsync(x => x.Code.ToUpper() == normalizedCode);
            if (isCodeExists)
            {
                throw new InvalidOperationException($"Mã vai trò '{dto.Code}' đã tồn tại trong hệ thống.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<IARole>(dto);
                entity.Code = entity.Code.Trim().ToUpper(); // Chuẩn hóa mã Code: viết hoa, không khoảng trắng thừa

                _context.IARoles.Add(entity);
                await _context.SaveChangesAsync();

                // Gán danh sách quyền hạn mặc định ban đầu
                if (dto.PermissionIds.Any())
                {
                    var permissions = dto.PermissionIds.Distinct().Select(pId => new IARolePermission
                    {
                        RoleId = entity.Id,
                        PermissionId = pId
                    });

                    _context.IARolePermissions.AddRange(permissions);
                    await _context.SaveChangesAsync();
                }

                return entity.Id;
            });
        }

        public async Task<bool> UpdateAsync(int id, IARoleUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.IARoles
                    .Include(x => x.RolePermissions)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy vai trò với ID = {id}.");
                }

                var isSuperAdminRole = entity.Code.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
                                       entity.Code.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase);

                // Bảo vệ vai trò hệ thống không bị tạm khóa
                if (isSuperAdminRole && !dto.IsActive)
                {
                    throw new InvalidOperationException("Không thể tạm khóa vai trò quản trị hệ thống mặc định.");
                }

                _mapper.Map(dto, entity);

                // Luôn giữ nguyên Code và trạng thái hoạt động cho vai trò quản trị tối cao
                if (isSuperAdminRole)
                {
                    entity.Code = "ADMIN";
                    entity.IsActive = true;
                }

                #region Đồng bộ lại danh sách Quyền hạn (RolePermissions)
                // Xóa các quyền cũ của vai trò
                if (entity.RolePermissions.Any())
                {
                    _context.IARolePermissions.RemoveRange(entity.RolePermissions);
                }

                // Với vai trò ADMIN tối cao, luôn luôn bảo đảm được cấp toàn bộ quyền hạn trong hệ thống
                IEnumerable<int> targetPermissionIds = dto.PermissionIds;
                if (isSuperAdminRole)
                {
                    var allPermIds = await _context.IAPermissions.Select(p => p.Id).ToListAsync();
                    targetPermissionIds = allPermIds.Union(dto.PermissionIds);
                }

                // Gán lại danh sách quyền mới
                if (targetPermissionIds.Any())
                {
                    var newPermissions = targetPermissionIds.Distinct().Select(pId => new IARolePermission
                    {
                        RoleId = id,
                        PermissionId = pId
                    });

                    _context.IARolePermissions.AddRange(newPermissions);
                }
                #endregion

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.IARoles.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy vai trò với ID = {id}.");
            }

            // Bảo vệ vai trò hệ thống cốt lõi không bị xóa
            if (entity.Code.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
                entity.Code.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không thể xóa vai trò quản trị hệ thống mặc định.");
            }

            // DbContext sẽ tự động chuyển thành Soft Delete (IsDeleted = true) trong SaveChangesAsync
            _context.IARoles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.IARoles.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy vai trò với ID = {id}.");
            }

            // Bảo vệ vai trò hệ thống không bị tạm khóa
            if ((entity.Code.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
                 entity.Code.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)) && entity.IsActive)
            {
                throw new InvalidOperationException("Không thể tạm khóa vai trò quản trị hệ thống mặc định.");
            }

            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();
            return entity.IsActive;
        }
        #endregion
    }
}