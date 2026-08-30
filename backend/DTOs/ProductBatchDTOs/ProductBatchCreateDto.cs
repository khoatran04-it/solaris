using System;

namespace backend.DTOs.ProductBatchDTOs
{
    /// <summary>
    /// DTO yêu cầu Tạo mới Lô Hàng Nông Sản (Product Batch).
    /// Nghiệp vụ: Lô hàng thường được tạo ra TRƯỚC hoặc TRONG quá trình lập Phiếu Nhập Kho (GRN).
    /// Đóng vai trò sống còn trong việc kiểm soát chất lượng, truy xuất nguồn gốc (Traceability) và định tuyến xuất kho.
    /// </summary>
    public class ProductBatchCreateDto
    {
        #region Thông tin Định danh
        /// <summary>
        /// Mã lô hàng duy nhất (Ví dụ: BATCH-20260817-CACHUA-01). 
        /// Nghiệp vụ: Có thể do User tự nhập theo mã in trên bao bì của NCC, hoặc nếu để trống, Service sẽ tự động sinh mã theo quy tắc [NămThángNgày]-[Mã SKU]-[STT].
        /// </summary>
        public string BatchCode { get; set; } = string.Empty;
        #endregion

        #region Thời hạn & Vòng đời (Shelf-life)
        /// <summary>Ngày sản xuất / Ngày thu hoạch, sơ chế, đóng gói thực tế từ Nông trại hoặc Nhà máy.</summary>
        public DateTime ManufactureDate { get; set; }

        /// <summary>
        /// Hạn sử dụng của lô hàng.
        /// Nghiệp vụ cốt lõi: Dữ liệu bắt buộc để hệ thống tính toán vòng đời và kích hoạt thuật toán ưu tiên xuất kho FEFO (First Expired, First Out - Hết hạn trước, xuất trước).
        /// </summary>
        public DateTime ExpiryDate { get; set; }
        #endregion

        #region Liên kết & Nguồn gốc (Traceability)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) sẽ được lưu trữ trong lô này.</summary>
        public int VariantId { get; set; }

        /// <summary>
        /// Mã định danh của Nhà cung cấp / Nông trại / Hợp tác xã.
        /// Nghiệp vụ: Chốt chặn bắt buộc để truy vết nguồn gốc ngược (Backward Traceability) khi phát sinh sự cố về an toàn vệ sinh thực phẩm (Vd: Dư lượng thuốc trừ sâu).
        /// </summary>
        public int SupplierId { get; set; }
        #endregion

        #region Trạng thái
        /// <summary>Trạng thái kinh doanh (Mặc định là true: Sẵn sàng đưa vào lưu thông sau khi nhập kho).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}