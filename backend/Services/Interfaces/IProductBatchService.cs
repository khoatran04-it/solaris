using backend.DTOs;
using backend.DTOs.ProductBatchDTOs;

namespace backend.Services.Interfaces
{
    public interface IProductBatchService
    {
        Task<IEnumerable<ProductBatchReadDto>> GetAllListAsync();

        Task<PagedResult<ProductBatchReadDto>> GetPagedAsync(
            string? search, // Tìm theo mã Lô hàng (BatchCode)
            int? variantId, // Lọc Lô hàng theo Biến thể sản phẩm
            int? supplierId, // Lọc Lô hàng theo Nhà cung cấp
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        Task<ProductBatchReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductBatchCreateDto dto);
        Task<bool> UpdateAsync(int id, ProductBatchUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}