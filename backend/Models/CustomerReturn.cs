using backend.Models.Enums;

namespace backend.Models
{
    public class CustomerReturn : ISoftDelete
    {
        public int Id { get; set; }

        public required string ReturnCode { get; set; }

        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        // Kho tiếp nhận lại hàng đổi trả
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        // Nhân viên tiếp nhận kiểm định
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
        public CustomerReturnStatus Status { get; set; } = CustomerReturnStatus.Pending;

        public decimal RefundAmount { get; set; } = 0;
        public string? Reason { get; set; }
        public string? InspectionNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public virtual ICollection<CustomerReturnDetail> Details { get; set; } = new List<CustomerReturnDetail>();
    }
}
