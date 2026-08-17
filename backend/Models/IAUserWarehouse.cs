namespace backend.Models
{
    /// <summary>
    /// Bảng liên kết trung gian Phân quyền dữ liệu cấp độ Kho (Data-level Authorization).
    /// Xác định nhân viên nào được phép xem, nhập, xuất và kiểm kê tại Kho nào.
    /// </summary>
    public class IAUserWarehouse
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; } 

        /// <summary>Thời điểm gán quyền quản lý kho</summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}