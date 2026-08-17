using backend.DTOs;
using backend.DTOs.SupplierDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Nhà cung cấp (Supplier).
    /// </summary>
    public interface ISupplierService
    {
        /// <summary>Lấy toàn bộ danh sách nhà cung cấp (dùng cho dropdown/combobox)</summary>
        Task<IEnumerable<SupplierReadDto>> GetAllListAsync();

        /// <summary>Lấy danh sách nhà cung cấp có phân trang và bộ lọc</summary>
        Task<PagedResult<SupplierReadDto>> GetPagedAsync(
            string? search,
            string? supplierTypesId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết nhà cung cấp theo ID kèm danh sách địa chỉ</summary>
        Task<SupplierReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới nhà cung cấp (kèm địa chỉ ban đầu nếu có)</summary>
        Task<int> CreateAsync(SupplierCreateDto dto);

        /// <summary>Cập nhật thông tin nhà cung cấp</summary>
        Task<bool> UpdateAsync(int id, SupplierUpdateDto dto);

        /// <summary>Xóa nhà cung cấp (có kiểm tra ràng buộc PO và Lô hàng)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Tạm khóa</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}