using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ProductCategoryService : IProductCategoryService
    {
        // ==========================================
        // SECTION: FIELDS & CONSTRUCTOR
        // ==========================================
        #region

        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductCategoryService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #endregion

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================

        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách loại sản phẩm (không phân trang)
        /// </summary>
        //IEnumerable<T>: hợp đồng cho một chuỗi có thể duyệt (foreach).
        public async Task<IEnumerable<ProductCategoryReadDto>> GetAllListAsync() 
        { 
             var types =  await _context.ProductCategories
                .AsNoTracking()//tối ưu khi chỉ đọc, EF không giữ state để theo dõi thay đổi.
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(); //trả về Task<List<ProductCategory>>

            return _mapper.Map<IEnumerable<ProductCategoryReadDto>>(types);
        }

        /// <summary>
        /// Lấy danh sách phân loại sản phẩm có phân trang và tìm kiếm
        /// </summary>
        /// 
        public async Task<PagedResult<ProductCategoryReadDto>> GetPagedAsync(
            string? search,
            string? names,
            string? categoryGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ProductCategories
                .Include(x => x.CategoryGroup)// .Include() để móc dữ liệu từ bảng cha (CategoryGroup)
                .AsQueryable();// query chỉ đang là biểu thức truy vấn

            // 1. Filter: Tìm kiếm theo mã hoặc tên
            if (!string.IsNullOrWhiteSpace(search)){//Nếu mà chuỗi search không rỗng
                var lowerSearch = search.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch)||
                    x.Name.ToLower().Contains(lowerSearch)
                );
            }

            // 2. Filter: Tìm kiếm theo danh sách tên cụ thể
            if (!string.IsNullOrWhiteSpace(names))
            {
                var nameList = names.Split(',').Select(n => n.Trim().ToLower()).ToList();
                query = query.Where(x => nameList.Contains(x.Name.ToLower()));
            }

            // 3. Lọc Đa luồng: Nhóm Danh Mục Sản Phẩm (CategoryGroup)
            if (!string.IsNullOrWhiteSpace(categoryGroupId))
            {
                var groupIdList = categoryGroupId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (groupIdList.Any())
                {
                    query = query.Where(x => x.CategoryGroupId.HasValue && groupIdList.Contains(x.CategoryGroupId.Value));
                }
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

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

            // .CountAsync(): Tính tổng số bản ghi thỏa mãn bộ lọc
            var totalRecords = await query.CountAsync();

            //Thực hiện phân trang và tối ưu hóa truy vấn
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking() //Báo chỉ đọc, không giữ state
                .ToListAsync(); //Task<List<ProductCategory>>

            var dtos = _mapper.Map<IEnumerable<ProductCategoryReadDto>>(items);

            return new PagedResult<ProductCategoryReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết phân loại theo ID
        /// </summary>
        public async Task<ProductCategoryReadDto?> GetByIdAsync(int id)
        {
            var type = await _context.ProductCategories
                .Include(x => x.CategoryGroup)//Móc thêm dữ liệu bảng cha, tải kèm navigation properties
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);//lấy phần tử đầu hoặc null

            if (type == null) return null;

            return _mapper.Map<ProductCategoryReadDto> (type);
        }
        #endregion
        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới loại sản phẩm
        /// </summary>
        public async Task<int> CreateAsync(ProductCategoryCreateDto dto)
        {
            //Kiểm tra trùng mã code
            if (await _context.ProductCategories.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã phân loại đã tồn tại");

            var entity = _mapper.Map<ProductCategory>(dto);

            _context.ProductCategories.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin loại sản phẩm
        /// </summary>
        public async Task<bool> UpdateAsync(int id, ProductCategoryUpdateDto dto)
        {
            var entity = await _context.ProductCategories.FindAsync(id);//Tìm entity theo khóa chính
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy loại sản phẩm cần sửa");

            //Kiểm tra trùng mã code với các bản ghi khác
            if (await _context.ProductCategories.AnyAsync(x => x.Id != id && x.Code == dto.Code))
                throw new Exception("Cập nhật thất bại: Mã phân loại này đã bị trùng lặp với một dòng dữ liệu khác.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa loại sản phẩm
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại để xóa");

            // Thao tác Remove sẽ được Global Interceptor
            // chuyển thành Update DeletedAt nếu dùng Soft Delete
            _context.ProductCategories.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
