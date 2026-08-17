using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Khách Hàng Trả Hàng (Customer Return / RMA).
    /// Tiếp nhận hàng hoàn từ khách, tiến hành kiểm định và phân loại hoàn lại vào tồn khả dụng (Available) hoặc hàng hỏng (Damaged).
    /// </summary>
    public class CustomerReturn : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã phiếu trả hàng (Ví dụ: RET-20260817-001)</summary>
        public required string ReturnCode { get; set; }

        /// <summary>Đơn bán hàng gốc</summary>
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        /// <summary>Khách hàng yêu cầu trả hàng</summary>
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        /// <summary>Kho tiếp nhận lại hàng hoàn</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Thủ kho / Nhân viên tiếp nhận kiểm định chất lượng</summary>
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
        public CustomerReturnStatus Status { get; set; } = CustomerReturnStatus.Pending;

        /// <summary>Tổng tiền hoàn trả lại cho khách (VND)</summary>
        public decimal RefundAmount { get; set; } = 0;

        /// <summary>Lý do khách trả hàng</summary>
        public string? Reason { get; set; }

        /// <summary>Biên bản kết quả kiểm tra chất lượng thực tế</summary>
        public string? InspectionNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<CustomerReturnDetail> Details { get; set; } = new List<CustomerReturnDetail>();
    }
}
