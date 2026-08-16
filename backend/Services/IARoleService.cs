using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.AuthDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class IARoleService : IIARoleService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public IARoleService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<IARoleReadDto>> GetAllListAsync()
        {
            var items = await _context.IARoles
                .Include(x => x.RolePermissions)
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<IARoleReadDto>>(items);
        }

        public async Task<PagedResult<IARoleReadDto>> GetPagedAsync(string? search, bool? isActive, int pageIndex, int pageSize)
        {
            var query = _context.IARoles
                .Include(x => x.RolePermissions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(searchLower) || x.Name.ToLower().Contains(searchLower));
            }

            if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

            var totalRecords = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
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

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy vai trò.");

            return _mapper.Map<IARoleReadDto>(entity);
        }

        public async Task<int> CreateAsync(IARoleCreateDto dto)
        {
            if (await _context.IARoles.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã Code vai trò đã tồn tại.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = _mapper.Map<IARole>(dto);
                _context.IARoles.Add(entity);
                await _context.SaveChangesAsync();

                // Thêm danh sách quyền mặc định
                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var permissions = dto.PermissionIds.Select(pId => new IARolePermission
                    {
                        RoleId = entity.Id,
                        PermissionId = pId
                    });
                    _context.IARolePermissions.AddRange(permissions);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return entity.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, IARoleUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.IARoles
                    .Include(x => x.RolePermissions)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) throw new KeyNotFoundException("Không tìm thấy vai trò.");

                _mapper.Map(dto, entity);

                // Đồng bộ Quyền: Xóa sạch dòng cũ -> Thêm dòng mới
                if (entity.RolePermissions.Any())
                {
                    _context.IARolePermissions.RemoveRange(entity.RolePermissions);
                }

                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var newPermissions = dto.PermissionIds.Select(pId => new IARolePermission
                    {
                        RoleId = id,
                        PermissionId = pId
                    });
                    _context.IARolePermissions.AddRange(newPermissions);
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
            var entity = await _context.IARoles.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy vai trò.");
            _context.IARoles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.IARoles.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy vai trò.");
            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}