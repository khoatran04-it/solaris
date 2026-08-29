using System;

namespace backend.DTOs.CustomerTypeDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Phân loại Khách hàng.
    /// Dùng để trả về cho Front-end (Dropdown list) hoặc trang Quản trị.
    /// </summary>
    public class CustomerTypeReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã phân loại khách hàng.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của phân loại.</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Mô tả chi tiết về phân loại này.</summary>
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