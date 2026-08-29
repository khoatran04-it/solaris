using System;

namespace backend.DTOs.CustomerGroupDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Nhóm Khách Hàng (Marketing Tag).
    /// Dùng để trả về danh sách các thẻ để Admin gán cho Khách hàng.
    /// </summary>
    public class CustomerGroupReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã nhóm khách hàng.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của nhóm khách hàng.</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Mô tả chi tiết về nhóm khách hàng.</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động.</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}