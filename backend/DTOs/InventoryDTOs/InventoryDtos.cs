using System;

namespace backend.DTOs.InventoryDTOs
{
    /// <summary>
    /// DTO hiển thị Báo cáo Tồn kho theo Thời gian thực (Real-time Inventory Read DTO).
    /// Dữ liệu đã được làm phẳng và tính toán sẵn các chỉ số cảnh báo (Vòng đời, Trạng thái) 
    /// nhằm tối ưu hóa việc render DataGrid / Dashboard trên Frontend.
    /// </summary>
    public class InventoryReadDto
    {
        public int Id { get; set; }

        #region Tọa độ Kho (Location)
        public int WarehouseId { get; set; }

        /// <summary>Mã Kho hàng (Dùng để hiển thị tag/label).</summary>
        public string WarehouseCode { get; set; } = string.Empty;

        /// <summary>Tên Kho hàng.</summary>
        public string WarehouseName { get; set; } = string.Empty;
        #endregion

        #region Hàng hóa & Đơn vị tính (Product)
        public int VariantId { get; set; }

        /// <summary>Mã SKU của Sản phẩm.</summary>
        public string VariantCode { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của Biến thể (Ví dụ: Cà chua Cherry 500g).</summary>
        public string VariantName { get; set; } = string.Empty;

        /// <summary>
        /// Đơn vị tính cơ sở (Base UoM). 
        /// Kỷ luật hiển thị: Mọi con số tồn kho trong bảng này đều quy về ĐVT cơ sở (Kg, Bó, Quả) để tránh sai lệch.
        /// </summary>
        public string BaseUoMName { get; set; } = string.Empty;
        #endregion

        #region Lô hàng & Truy xuất nguồn gốc (Batch & Traceability)
        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// Tên Nhà cung cấp/Nông hộ (Giúp truy vết lô nông sản này đến từ đâu).
        /// </summary>
        public string SupplierName { get; set; } = string.Empty;

        /// <summary>
        /// Số ngày còn lại trước khi hết hạn (Tính toán realtime cho Frontend).
        /// Nghiệp vụ: Frontend có thể dựa vào số ngày này để tô màu cảnh báo Đỏ (Đã hết hạn) / Vàng (Cận date) / Xanh (An toàn).
        /// </summary>
        public int DaysToExpiry => (ExpiryDate - DateTime.UtcNow).Days;
        #endregion

        #region Phân mảng Số liệu Tồn kho (Inventory Buckets)
        /// <summary>
        /// [Hàng Xanh] - Số lượng có sẵn để bán.
        /// (Dùng để đồng bộ lên Website/App cho khách hàng đặt mua).
        /// </summary>
        public decimal QuantityAvailable { get; set; }

        /// <summary>
        /// [Hàng Vàng] - Số lượng đã bị giữ chỗ.
        /// (Đã có khách đặt mua nhưng chưa xuất kho đi, bị khóa lại không cho bán tiếp).
        /// </summary>
        public decimal QuantityReserved { get; set; }

        /// <summary>
        /// [Hàng Cam] - Số lượng đang chờ kiểm định (Quality Control).
        /// (Hàng khách bom hoàn trả về, hoặc hàng đang nghi ngờ chất lượng, chờ QC đánh giá).
        /// </summary>
        public decimal QuantityQC { get; set; }

        /// <summary>
        /// [Hàng Đỏ] - Số lượng hỏng/thối chờ tiêu hủy.
        /// (Không tính vào tồn kho khả dụng nhưng vẫn nằm trong kho vật lý chiếm không gian).
        /// </summary>
        public decimal QuantityDamaged { get; set; }

        /// <summary>
        /// Tổng tồn kho vật lý đang nằm trong tòa nhà.
        /// Bằng tổng 4 trạng thái cộng lại. Phục vụ cho đối soát khi nhân viên đi đếm kho cuối tháng.
        /// </summary>
        public decimal TotalQuantity => QuantityAvailable + QuantityReserved + QuantityQC + QuantityDamaged;
        #endregion
    }
}