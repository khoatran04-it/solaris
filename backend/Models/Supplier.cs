namespace backend.Models
{
    public class Supplier : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }

        public string? LogoPath { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public string? TaxCode { get; set; }
        public string? Website { get; set; }
        public string? SocialLink { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY --- 
        public int? SupplierTypeId { get; set; }
        public virtual SupplierType? SupplierType { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection <SupplierAddress> Addresses { get; set; } = new List <SupplierAddress> ();
        public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}