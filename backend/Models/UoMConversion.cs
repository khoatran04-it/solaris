namespace backend.Models
{
    public class UoMConversion : ISoftDelete
    {
        public int Id { get; set; }
        public decimal ConversionFactor { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY ---
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int? FromUoMId { get; set; }
        public virtual UoM? FromUoM { get; set; }

        public int? ToUoMId { get; set; }
        public virtual UoM? ToUoM { get; set; }

        // Logic ảo: Chỉ tồn tại trong C#, EF Core sẽ lờ đi
        public bool IsStandard => ProductId == null;
    }
}