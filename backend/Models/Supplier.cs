namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhà Cung Cấp (Supplier / Vendor).
    /// Đối tác cung ứng nông sản, vật tư và nguyên vật liệu cho hệ thống kho.
    /// </summary>
    public class Supplier : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin định danh
        /// <summary>Mã nhà cung cấp (Ví dụ: SUP-DALAT-001).</summary>
        public required string Code { get; set; }

        /// <summary>Tên đơn vị / Tên công ty cung cấp.</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn logo hoặc ảnh đại diện của nhà cung cấp.</summary>
        public string? LogoPath { get; set; }
        #endregion

        #region Thông tin liên hệ
        /// <summary>Số điện thoại liên hệ chính.</summary>
        public required string Phone { get; set; }

        /// <summary>Địa chỉ Email liên hệ chính.</summary>
        public required string Email { get; set; }

        /// <summary>Website chính thức của doanh nghiệp.</summary>
        public string? Website { get; set; }

        /// <summary>Liên kết mạng xã hội hoặc Fanpage.</summary>
        public string? SocialLink { get; set; }
        #endregion

        #region Thông tin Thuế & Thanh toán
        /// <summary>Mã số thuế doanh nghiệp.</summary>
        public string? TaxCode { get; set; }

        /// <summary>Số tài khoản ngân hàng giao dịch.</summary>
        public string? BankAccount { get; set; }

        /// <summary>Tên ngân hàng và chi nhánh.</summary>
        public string? BankName { get; set; }
        #endregion

        #region Phân loại & Ghi chú
        /// <summary>Mã định danh Loại nhà cung cấp (Trang trại, Đại lý, Công ty nhập khẩu...).</summary>
        public int? SupplierTypeId { get; set; }

        /// <summary>Ghi chú đặc thù, đánh giá hoặc lưu ý nội bộ về nhà cung cấp này.</summary>
        public string? Note { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hợp tác, false: Ngừng giao dịch).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Thực thể Phân loại Nhà cung cấp.</summary>
        public virtual SupplierType? SupplierType { get; set; }

        /// <summary>Danh sách các địa chỉ lấy/giao hàng của nhà cung cấp.</summary>
        public virtual ICollection<SupplierAddress> Addresses { get; set; } = new List<SupplierAddress>();

        /// <summary>Danh sách các sản phẩm (mặt hàng) mà nhà cung cấp này có khả năng cung ứng.</summary>
        public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

        /// <summary>Danh sách các lô hàng (Batches) đã nhập từ nhà cung cấp này.</summary>
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();

        /// <summary>Danh sách các Đơn đặt hàng mua (Purchase Orders) phát sinh với nhà cung cấp này.</summary>
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        #endregion
    }
}