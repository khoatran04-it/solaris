using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Khách Hàng Trả Hàng (Customer Return / RMA - Return Merchandise Authorization).
    /// Quy trình cốt lõi: Tiếp nhận yêu cầu trả hàng -> Nhận hàng vật lý tại kho -> Kiểm định chất lượng (QC) 
    /// -> Quyết định hoàn tiền (Refund) -> Phân loại vào Tồn kho Khả dụng (Available) hoặc Hàng hỏng (Damaged).
    /// </summary>
    public class CustomerReturn : ISoftDelete
    {
        public int Id { get; set; }

        #region Định danh & Trạng thái (Identity & Status)
        /// <summary>Mã phiếu trả hàng (Ví dụ: RET-20260817-001).</summary>
        public required string ReturnCode { get; set; }

        /// <summary>
        /// Trạng thái quy trình trả hàng (Pending, Inspecting, Completed, Rejected).
        /// NGHIỆP VỤ KHO: Chỉ khi trạng thái chuyển sang Completed, hệ thống mới gọi vào 
        /// IInventoryService.ReceiveCustomerReturnAsync() để chính thức cộng lại số dư kho.
        /// </summary>
        public CustomerReturnStatus Status { get; set; } = CustomerReturnStatus.Pending;
        #endregion

        #region Đối soát Chứng từ (Reference & Ownership)
        /// <summary>
        /// Đơn bán hàng gốc bị hoàn trả. 
        /// Bắt buộc phải có để đối chiếu xem khách có mua đúng món hàng này với giá này không.
        /// </summary>
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        /// <summary>Khách hàng thực hiện yêu cầu hoàn trả.</summary>
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }
        #endregion

        #region Địa điểm & Kiểm định (Location & QC)
        /// <summary>
        /// Kho tiếp nhận lại hàng hoàn. 
        /// (Có thể khác với Kho xuất đi ban đầu nếu công ty có chính sách trả hàng tại kho trung tâm).
        /// </summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Thủ kho / Nhân viên QC chịu trách nhiệm kiểm định chất lượng lô hàng hoàn này.</summary>
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        /// <summary>
        /// Biên bản kết quả kiểm tra chất lượng thực tế.
        /// KẾT LUẬN QC: Ghi chú rõ tình trạng hàng (Ví dụ: "Hộp móp nhẹ nhưng ruột nguyên vẹn" 
        /// hoặc "Trái cây đã bị dập nát, bốc mùi"). Quyết định trực tiếp đến việc hàng được bán lại hay đem hủy.
        /// </summary>
        public string? InspectionNotes { get; set; }
        #endregion

        #region Tài chính & Lý do (Finance & Justification)
        /// <summary>Ngày khách hàng chính thức gửi yêu cầu hoặc ngày hàng về tới kho.</summary>
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Lý do khách trả hàng (Hàng lỗi, Boom hàng, Giao sai món, Không còn nhu cầu...).
        /// Dữ liệu sống còn để đội ngũ Phân tích dữ liệu (Data Analytics) tối ưu chất lượng dịch vụ.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Tổng số tiền quyết định hoàn trả lại cho khách (VND).
        /// NGHIỆP VỤ KẾ TOÁN: Có thể nhỏ hơn giá trị gốc của đơn hàng nếu công ty thu phí hoàn hàng (Restocking fee) 
        /// hoặc khách làm hỏng bao bì.
        /// </summary>
        public decimal RefundAmount { get; set; } = 0;
        #endregion

        #region Thời gian & Soft Delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết
        /// <summary>Danh sách chi tiết các mặt hàng bị trả lại, kèm theo kết luận QC cho từng món.</summary>
        public virtual ICollection<CustomerReturnDetail> Details { get; set; } = new List<CustomerReturnDetail>();
        #endregion
    }
}