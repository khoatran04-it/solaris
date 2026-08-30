using backend.Models.Enums;
using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Sổ Cái Giao Dịch Tồn Kho (Immutable Inventory Transaction Log).
    /// Đóng vai trò như một hệ thống "Event Sourcing" - Ghi nhận mọi vết biến động tăng, giảm, 
    /// giữ chỗ (reserve), hoàn trả và kiểm kê kho theo thời gian thực.
    /// KỶ LUẬT THÉP: Dữ liệu là Bất biến (Immutable). Không cho phép Update hay Soft Delete.
    /// </summary>
    public class InventoryTransaction
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Phân loại
        /// <summary>Mã giao dịch phát sinh trên hệ thống (Ví dụ: TX-20260817-0001).</summary>
        public required string TransactionCode { get; set; }

        /// <summary>
        /// Loại giao dịch (Nhập kho, Xuất kho, Giữ chỗ, Hủy giữ chỗ, Điều chuyển, Kiểm kê, Bút toán đảo...).
        /// Nghiệp vụ: Dùng để phân biệt luồng cộng/trừ/đóng băng khi tính toán lại số dư (Recalculate Balance).
        /// </summary>
        public TransactionType Type { get; set; }
        #endregion

        #region Vị trí & Hàng hóa (Location & Product)
        /// <summary>Mã định danh Kho hàng nơi xảy ra biến động.</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Mã định danh Biến thể sản phẩm (SKU) có sự biến động.</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// Lô hàng (Batch) cụ thể xảy ra biến động.
        /// Đảm bảo Truy xuất nguồn gốc khép kín: Mọi thay đổi tồn kho đều trỏ chính xác về Ngày SX và Hạn SD của lô nào.
        /// </summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }
        #endregion

        #region Số liệu Biến động (Metrics)
        /// <summary>
        /// Khối lượng/Số lượng biến động. 
        /// NGHIỆP VỤ CỐT LÕI: Để sổ cái tính toán không bị sai lệch, con số này BẮT BUỘC 
        /// phải được quy đổi về Đơn vị tính cơ sở (Base UoM) trước khi lưu vào CSDL 
        /// (Ví dụ: Nhập 1 Thùng (10kg) thì lưu vào đây là Quantity = 10, UoM không lưu ở bảng này).
        /// </summary>
        public decimal Quantity { get; set; }
        #endregion

        #region Đối soát & Truy vết (Audit Trail)
        /// <summary>
        /// Mã chứng từ gốc tham chiếu sinh ra giao dịch này (Ví dụ: Mã Phiếu nhập IR-xxx, Mã Phiếu xuất OUT-xxx, Mã Order...).
        /// Rất quan trọng để bộ phận Kế toán/Kiểm toán truy vết nguyên nhân dòng tiền/hàng.
        /// </summary>
        public string? ReferenceCode { get; set; }

        /// <summary>Ghi chú nghiệp vụ chi tiết (Ví dụ: "Hệ thống tự động trừ kho khi xuất hàng Đơn #1234").</summary>
        public string? Note { get; set; }
        #endregion

        #region Hệ thống & Bảo mật (System & Security)
        /// <summary>Mã định danh nhân viên/hệ thống thực hiện hoặc phê duyệt giao dịch này.</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>
        /// Thời điểm ghi nhận giao dịch vào sổ cái (Lưu theo chuẩn UTC).
        /// Sổ cái không có UpdatedAt hay DeletedAt nhằm bảo toàn tính toàn vẹn của dữ liệu kế toán.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
}