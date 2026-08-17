using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý và tính toán Quy tắc Quy đổi Đơn vị tính (UoM Conversion).
    /// </summary>
    public interface IUoMConversionService
    {
        /// <summary>Lấy toàn bộ danh sách quy tắc quy đổi không phân trang (hỗ trợ lọc đang hoạt động)</summary>
        Task<IEnumerable<UoMConversionReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách quy tắc quy đổi có phân trang, tìm kiếm và lọc nâng cao</summary>
        Task<PagedResult<UoMConversionReadDto>> GetPagedAsync(
            string? search,
            int? productId,
            bool? isStandard,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy thông tin chi tiết quy tắc quy đổi theo ID</summary>
        Task<UoMConversionReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới quy tắc quy đổi (thẩm định kiểm tra vòng lặp và cùng nhóm ĐVT)</summary>
        Task<int> CreateAsync(UoMConversionCreateDto dto);

        /// <summary>Cập nhật quy tắc quy đổi</summary>
        Task<bool> UpdateAsync(int id, UoMConversionUpdateDto dto);

        /// <summary>Xóa mềm quy tắc quy đổi</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Kích hoạt hoặc tạm khóa quy tắc quy đổi</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}