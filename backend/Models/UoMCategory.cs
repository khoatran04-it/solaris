namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhóm Đơn vị tính (Ví dụ: Nhóm Khối lượng, Nhóm Dung tích, Nhóm Quy cách đóng gói).
    /// Mỗi nhóm sở hữu một Đơn vị tính cơ sở (Base UoM) làm mốc quy chiếu toán học cho toàn bộ tỷ lệ quy đổi trong nhóm.
    /// </summary>
    public class UoMCategory : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã nhóm đơn vị tính duy nhất (Ví dụ: WEIGHT, VOLUME, PACKAGING).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị nhóm đơn vị tính (Ví dụ: Khối lượng, Thể tích, Đóng gói).</summary>
        public required string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Khóa ngoại & Đơn vị tính cơ sở (Base UoM)
        /// <summary>
        /// ID Đơn vị tính gốc/chuẩn của nhóm (Ví dụ: Kg trong nhóm Khối lượng, Lít trong nhóm Thể tích).
        /// Cho phép null khi vừa khởi tạo nhóm trước khi gán Base UoM.
        /// </summary>
        public int? BaseUoMId { get; set; }
        public virtual UoM? BaseUoM { get; set; }
        #endregion

        #region Danh sách liên kết (Navigation Properties)
        /// <summary>Danh sách tất cả các đơn vị tính thuộc nhóm này.</summary>
        public virtual ICollection<UoM> UoMs { get; set; } = new List<UoM>();
        #endregion
    }
}