namespace backend.Models
{
    /// <summary>
    /// Thực thể Kho Hàng Vật Lý (Physical Warehouse).
    /// Đơn vị quản lý lưu trữ hàng hóa, kiểm soát số dư tồn kho và phân quyền nhân viên theo kho.
    /// </summary>
    public class Warehouse : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Tên kho hàng (Ví dụ: Tổng Kho Hà Nội, Kho Nông Sản Đà Lạt)</summary>
        public required string Name { get; set; }

        /// <summary>Mã kho duy nhất (Ví dụ: WH-HN-01, WH-DL-02)</summary>
        public required string Code { get; set; }

        /// <summary>Loại kho (Kho tổng, Kho trung chuyển, Cửa hàng phân phối)</summary>
        public string? WarehouseType { get; set; }

        /// <summary>Địa chỉ vật lý và tọa độ GPS của kho hàng</summary>
        public int AddressId { get; set; }
        public virtual WarehouseAddress? Address { get; set; }

        /// <summary>Quản lý trưởng kho (IAUser)</summary>
        public int? ManagerId { get; set; }
        public virtual IAUser? Manager { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        /// <summary>Danh sách nhân viên được phân quyền truy cập làm việc tại kho này (Data-Level Authorization)</summary>
        public virtual ICollection<IAUserWarehouse> UserWarehouses { get; set; } = new List<IAUserWarehouse>();
    }
}