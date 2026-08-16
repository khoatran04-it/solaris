namespace backend.DTOs
{
    /// <summary>
    /// Lớp generic dùng để trả về dữ liệu phân trang cho toàn bộ hệ thống
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của DTO (VD: SupplierReadDto, ProductCategoryReadDto...)</typeparam>
    public class PagedResult<T>
    {
        // Danh sách dữ liệu của trang hiện tại
        public IEnumerable<T> Items { get; set; } = new List<T>();

        // Tổng số lượng bản ghi có trong Database (sau khi đã áp dụng bộ lọc)
        public int TotalRecords { get; set; }

        // Tổng số trang (Được tính bằng: TotalRecords / PageSize)
        public int TotalPages { get; set; }

        // Trang hiện tại đang đứng
        public int CurrentPage { get; set; }

        // Số lượng bản ghi hiển thị trên 1 trang
        public int PageSize { get; set; }
    }
}