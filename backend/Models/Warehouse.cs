namespace backend.Models
{
    public class Warehouse : ISoftDelete
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? WarehouseType { get; set; }

        public int AddressId { get; set; }
        public virtual WarehouseAddress? Address { get; set; }

        public int? ManagerId { get; set; }
        public virtual IAUser? Manager { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // 🔥 Navigation chọc ngược để lấy danh sách nhân viên của kho này
        public virtual ICollection<IAUserWarehouse> UserWarehouses { get; set; } = new List<IAUserWarehouse>();
    }
}