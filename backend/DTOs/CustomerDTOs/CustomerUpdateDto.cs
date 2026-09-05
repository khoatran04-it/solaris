using System;
using System.Collections.Generic;

namespace backend.DTOs.CustomerDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Khách Hàng.
    /// Lưu ý: Phần cập nhật địa chỉ (Addresses) thường được tách riêng ra các API quản lý Sổ địa chỉ để dễ kiểm soát.
    /// </summary>
    public class CustomerUpdateDto
    {
        #region Thông tin Định danh & Liên hệ
        /// <summary>Mã khách hàng.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên đầy đủ của khách hàng.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên lạc chính.</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>Địa chỉ Email liên hệ.</summary>
        public string? Email { get; set; }

        /// <summary>Mật khẩu mới nếu muốn đặt lại (Reset Password) cho khách hàng.</summary>
        public string? Password { get; set; }
        #endregion

        #region Thông tin Chi tiết & Cá nhân hóa
        /// <summary>Mã số thuế (Dành cho khách hàng B2B xuất hóa đơn).</summary>
        public string? TaxCode { get; set; }

        /// <summary>Đường dẫn lưu trữ ảnh đại diện.</summary>
        public string? AvatarPath { get; set; }

        /// <summary>Ngày tháng năm sinh.</summary>
        public DateTime? Birthday { get; set; }

        /// <summary>Giới tính (true: Nam, false: Nữ, null: Khác).</summary>
        public bool? Gender { get; set; }

        /// <summary>Ghi chú nội bộ dành cho nhân viên CSKH.</summary>
        public string? Note { get; set; }
        #endregion

        #region Trạng thái & Phân loại
        /// <summary>Trạng thái hoạt động (true: Cho phép giao dịch, false: Khóa tài khoản).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>ID Phân loại khách hàng (Ví dụ: Sỉ, Lẻ, HORECA).</summary>
        public int? CustomerTypeId { get; set; }

        /// <summary>ID Hạng thành viên (Ví dụ: Đồng, Bạc, Vàng).</summary>
        public int? CustomerTierId { get; set; }
        #endregion

        #region Dữ liệu Mở rộng (Cập nhật đồng thời)
        /// <summary>Danh sách ID các nhóm khách hàng mới (Sẽ xóa liên kết nhóm cũ và ghi đè bằng danh sách này).</summary>
        public List<int> GroupIds { get; set; } = new List<int>();
        #endregion
    }
}