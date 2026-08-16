namespace backend.Models
{
    public class IAUserWarehouse
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; } 

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}