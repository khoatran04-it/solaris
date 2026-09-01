using System;
using System.Collections.Generic;
using backend.DTOs.CustomerAddressDTOs;

namespace backend.DTOs.CustomerDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Khách Hàng (B2C & B2B).
    /// Hỗ trợ tạo luôn danh sách địa chỉ giao hàng và gán nhóm (Marketing Tags) ngay trong lúc tạo mới.
    /// </summary>
    public class CustomerCreateDto
    {
        #region Thông tin Định danh & Liên hệ
        /// <summary>Mã khách hàng tự sinh hoặc nhập tay (Ví dụ: CUST-2026-001).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên đầy đủ của khách hàng cá nhân hoặc Tên doanh nghiệp.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên lạc chính.</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>Địa chỉ Email liên hệ.</summary>
        public string? Email { get; set; }

        /// <summary>Mật khẩu khởi tạo tài khoản trực tuyến (Nếu quản trị viên muốn cấp quyền đăng nhập Storefront ngay).</summary>
        public string? Password { get; set; }
        #endregion

        #region Thông tin Chi tiết & Cá nhân hóa
        /// <summary>Mã số thuế (Dành cho khách hàng B2B xuất hóa đơn).</summary>
        public string? TaxCode { get; set; }

        /// <summary>Đường dẫn lưu trữ ảnh đại diện (Avatar).</summary>
        public string? AvatarPath { get; set; }

        /// <summary>Ngày tháng năm sinh (Phục vụ CSKH, chúc mừng sinh nhật).</summary>
        public DateTime? Birthday { get; set; }

        /// <summary>Giới tính (true: Nam, false: Nữ, null: Khác).</summary>
        public bool? Gender { get; set; }

        /// <summary>Ghi chú nội bộ dành riêng cho nhân viên chăm sóc khách hàng.</summary>
        public string? Note { get; set; }
        #endregion

        #region Trạng thái & Phân loại
        /// <summary>Trạng thái hoạt động (true: Cho phép giao dịch, false: Khóa tài khoản).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>ID Phân loại khách hàng (Ví dụ: Sỉ, Lẻ, HORECA).</summary>
        public int? CustomerTypeId { get; set; }

        /// <summary>ID Hạng thành viên (Ví dụ: Đồng, Bạc, Vàng) để tự động áp dụng % chiết khấu.</summary>
        public int? CustomerTierId { get; set; }
        #endregion

        #region Dữ liệu Mở rộng (Khởi tạo đồng thời)
        /// <summary>Danh sách ID các nhóm (Marketing Tags) muốn gán ngay khi tạo mới khách hàng.</summary>
        public List<int> GroupIds { get; set; } = new List<int>();

        /// <summary>Danh sách sổ địa chỉ gửi kèm khi tạo mới (Nếu có).</summary>
        public List<CustomerAddressCreateDto>? Addresses { get; set; }
        #endregion
    }
}