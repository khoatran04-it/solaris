using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Nhóm Đơn vị tính (UoM Category).
    /// </summary>
    public interface IUoMCategoryService
    {
        /// <summary>Lấy toàn bộ danh sách nhóm ĐVT không phân trang (hỗ trợ lọc chỉ nhóm đang hoạt động)</summary>
        Task<IEnumerable<UoMCategoryReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách nhóm ĐVT có phân trang và tìm kiếm</summary>
        Task<PagedResult<UoMCategoryReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>Lấy thông tin chi tiết nhóm ĐVT theo ID</summary>
        Task<UoMCategoryReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới nhóm ĐVT</summary>
        Task<int> CreateAsync(UoMCategoryCreateDto dto);

        /// <summary>Cập nhật thông tin nhóm ĐVT</summary>
        Task<bool> UpdateAsync(int id, UoMCategoryUpdateDto dto);

        /// <summary>Xóa mềm nhóm ĐVT (có kiểm tra ràng buộc đơn vị tính con)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Kích hoạt hoặc tạm khóa nhóm ĐVT</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}
