namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhà Cung Cấp (Supplier / Vendor).
    /// Đối tác cung ứng nông sản, vật tư và nguyên vật liệu cho hệ thống kho Solaris.
    /// </summary>
    public class Supplier : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã nhà cung cấp (Ví dụ: SUP-DALAT-001)</summary>
        public required string Code { get; set; }

        /// <summary>Tên đơn vị / Tên công ty cung cấp</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn logo hoặc ảnh đại diện</summary>
        public string? LogoPath { get; set; }

        /// <summary>Số điện thoại liên hệ chính</summary>
        public required string Phone { get; set; }

        /// <summary>Địa chỉ Email liên hệ chính</summary>
        public required string Email { get; set; }

        /// <summary>Mã số thuế doanh nghiệp</summary>
        public string? TaxCode { get; set; }

        /// <summary>Website chính thức</summary>
        public string? Website { get; set; }

        /// <summary>Liên kết mạng xã hội / Fanpage</summary>
        public string? SocialLink { get; set; }

        /// <summary>Số tài khoản ngân hàng</summary>
        public string? BankAccount { get; set; }

        /// <summary>Tên ngân hàng và chi nhánh</summary>
        public string? BankName { get; set; }

        /// <summary>Ghi chú đặc thù về nhà cung cấp</summary>
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY --- 
        /// <summary>ID Loại nhà cung cấp (Trang trại, Đại lý, Công ty nhập khẩu...)</summary>
        public int? SupplierTypeId { get; set; }
        public virtual SupplierType? SupplierType { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<SupplierAddress> Addresses { get; set; } = new List<SupplierAddress>();
        public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}