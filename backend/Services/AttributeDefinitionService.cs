using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.AttributeDefinitionDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Từ điển Thuộc tính (Attribute Definition / EAV Dictionary).
    /// Đảm bảo tính toàn vẹn dữ liệu, kiểm tra tính duy nhất (case-insensitive) và hỗ trợ giao dịch kiên cường (Execution Strategy).
    /// </summary>
    public class AttributeDefinitionService : IAttributeDefinitionService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public AttributeDefinitionService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách từ điển thuộc tính (không phân trang, dùng cho Dropdown/Select).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các thuộc tính đang ở trạng thái hoạt động.</param>
        public async Task<IEnumerable<AttributeDefinitionReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.AttributeDefinitions
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AttributeDefinitionReadDto>>(items);
        }

        /// <summary>
        /// Lấy danh sách từ điển thuộc tính có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên thuộc tính.</param>
        /// <param name="dataType">Lọc theo kiểu dữ liệu của thuộc tính (String, Number, Boolean, Date, Select).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        public async Task<PagedResult<AttributeDefinitionReadDto>> GetPagedAsync(
            string? search,
            string? dataType,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.AttributeDefinitions.AsQueryable();

            // 1. Filter: Tìm theo tên thuộc tính
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Theo kiểu dữ liệu
            if (!string.IsNullOrWhiteSpace(dataType))
            {
                var lowerDataType = dataType.Trim().ToLower();
                query = query.Where(x => x.DataType.ToLower() == lowerDataType);
            }

            // 3. Filter: Trạng thái
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // 4. Filter: Ngày tạo
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 5. Filter: Ngày cập nhật
            if (updatedAt.HasValue)
            {
                var startDate = updatedAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.UpdatedAt >= startDate && x.UpdatedAt < endDate);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<AttributeDefinitionReadDto>>(items);

            return new PagedResult<AttributeDefinitionReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một định nghĩa thuộc tính theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính trong từ điển.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy thuộc tính.</exception>
        public async Task<AttributeDefinitionReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.AttributeDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính.");

            return _mapper.Map<AttributeDefinitionReadDto>(entity);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một định nghĩa thuộc tính vào từ điển hệ thống (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="dto">Dữ liệu khởi tạo thuộc tính.</param>
        /// <returns>ID của định nghĩa thuộc tính vừa được tạo.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi tên thuộc tính đã tồn tại trong từ điển.</exception>
        public async Task<int> CreateAsync(AttributeDefinitionCreateDto dto)
        {
            var trimmedName = dto.Name.Trim();

            // Bắt lỗi trùng Tên thuộc tính (case-insensitive)
            var isDuplicate = await _context.AttributeDefinitions
                .AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower());

            if (isDuplicate)
                throw new InvalidOperationException($"Thuộc tính có tên '{trimmedName}' đã tồn tại trong từ điển hệ thống.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<AttributeDefinition>(dto);
                entity.Name = trimmedName;
                entity.DataType = dto.DataType.Trim().ToUpper();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.AttributeDefinitions.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin của một định nghĩa thuộc tính (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy thuộc tính.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi tên thuộc tính bị trùng với bản ghi khác.</exception>
        public async Task<bool> UpdateAsync(int id, AttributeDefinitionUpdateDto dto)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính cần cập nhật.");

            var trimmedName = dto.Name.Trim();

            // Bắt lỗi trùng Tên (nhưng bỏ qua chính nó)
            var isDuplicate = await _context.AttributeDefinitions
                .AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower());

            if (isDuplicate)
                throw new InvalidOperationException($"Tên thuộc tính '{trimmedName}' đã bị trùng lặp với một thuộc tính khác.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _mapper.Map(dto, entity);
                entity.Name = trimmedName;
                entity.DataType = dto.DataType.Trim().ToUpper();
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa mềm một định nghĩa thuộc tính (có kiểm tra ràng buộc mẫu danh mục và biến thể sản phẩm).
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính cần xóa.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy thuộc tính.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi thuộc tính đang được gán vào mẫu danh mục hoặc biến thể sản phẩm.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính để xóa.");

            // 1. SAFETY SHIELD: Chặn xóa nếu đang được gắn vào Mẫu Danh mục
            var isUsedInCategories = await _context.CategoryAttributes
                .AnyAsync(ca => ca.AttributeDefinitionId == id);
            if (isUsedInCategories)
                throw new InvalidOperationException("Không thể xóa thuộc tính này vì đang được gán vào mẫu thuộc tính danh mục sản phẩm.");

            // 2. SAFETY SHIELD: Chặn xóa nếu đang có Biến thể sản phẩm sử dụng
            var isUsedInVariants = await _context.ProductAttributes
                .AnyAsync(pa => pa.AttributeDefinitionId == id && !pa.IsDeleted);
            if (isUsedInVariants)
                throw new InvalidOperationException("Không thể xóa thuộc tính này vì đang được sử dụng trong các biến thể sản phẩm.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.AttributeDefinitions.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang hoạt động / Tạm khóa) của định nghĩa thuộc tính.
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính cần đổi trạng thái.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy thuộc tính.</exception>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}