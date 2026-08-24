namespace backend.Models
{
    /// <summary>
    /// Thực thể phân quyền dữ liệu cấp độ Kho hàng (Data-level Authorization).
    /// Xác định phạm vi kho mà người dùng được phép truy cập và thao tác nghiệp vụ.
    /// Khóa chính phức hợp (Composite Key): (UserId, WarehouseId).
    /// </summary>
    public class IAUserWarehouse
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Thời điểm gán quyền truy cập kho cho người dùng (UTC).</summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}