using backend.DTOs;
using backend.DTOs.UoMDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Đơn vị tính (UoM).
    /// </summary>
    public interface IUoMService
    {
        /// <summary>Lấy toàn bộ danh sách ĐVT không phân trang (hỗ trợ lọc chỉ ĐVT đang hoạt động)</summary>
        Task<IEnumerable<UoMReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách ĐVT có phân trang, tìm kiếm và lọc theo Nhóm</summary>
        Task<PagedResult<UoMReadDto>> GetPagedAsync(
            string? search,
            int? categoryId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy thông tin chi tiết ĐVT theo ID</summary>
        Task<UoMReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới ĐVT</summary>
        Task<int> CreateAsync(UoMCreateDto dto);

        /// <summary>Cập nhật thông tin ĐVT</summary>
        Task<bool> UpdateAsync(int id, UoMUpdateDto dto);

        /// <summary>Xóa mềm ĐVT (có kiểm tra ràng buộc BaseUoM, Product, Conversions, v.v.)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Kích hoạt hoặc tạm khóa ĐVT</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}