using System;
using System.Collections.Generic;
using backend.DTOs.CustomerAddressDTOs;

namespace backend.DTOs.CustomerDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Khách Hàng.
    /// Chứa đầy đủ các thông tin hiển thị (Enriched data) từ Type, Tier, Groups và Addresses.
    /// </summary>
    public class CustomerReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Liên hệ
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        #endregion

        #region Thông tin Chi tiết & Cá nhân hóa
        public string? TaxCode { get; set; }
        public string? AvatarPath { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? Gender { get; set; }
        public string? Note { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        public bool IsActive { get; set; }

        /// <summary>Đã thiết lập tài khoản & mật khẩu đăng nhập trực tuyến (Web/App) hay chưa.</summary>
        public bool HasOnlineAccount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Dữ liệu Liên kết Mở rộng (UI Render)
        /// <summary>ID Phân loại khách hàng.</summary>
        public int? CustomerTypeId { get; set; }

        /// <summary>Tên hiển thị Phân loại khách hàng.</summary>
        public string? CustomerTypeName { get; set; }

        /// <summary>ID Hạng thành viên.</summary>
        public int? CustomerTierId { get; set; }

        /// <summary>Tên hiển thị Hạng thành viên.</summary>
        public string? CustomerTierName { get; set; }

        /// <summary>Danh sách Tên các nhóm (Marketing Tags) mà khách hàng này đang tham gia.</summary>
        public List<string> Groups { get; set; } = new List<string>();

        /// <summary>Danh sách ID các nhóm (Dùng để hiển thị selected-items trong Multi-select UI).</summary>
        public List<int> GroupIds { get; set; } = new List<int>();

        /// <summary>Danh sách Sổ địa chỉ giao hàng của khách hàng.</summary>
        public List<CustomerAddressReadDto> Addresses { get; set; } = new List<CustomerAddressReadDto>();
        #endregion

        #region Thống kê Mua Hàng & Tiến độ Hạng Thành viên (Loyalty Progress)
        /// <summary>Tổng số tiền đã thanh toán từ các đơn hàng hoàn tất.</summary>
        public decimal TotalSpent { get; set; } = 0;

        /// <summary>Tổng số đơn hàng đã đặt.</summary>
        public int TotalOrders { get; set; } = 0;

        /// <summary>Phần trăm chiết khấu hiện tại của hạng.</summary>
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>Tên hạng thành viên tiếp theo (ví dụ: Bạc, Vàng, Kim Cương). Null nếu đã ở hạng cao nhất.</summary>
        public string? NextTierName { get; set; }

        /// <summary>Mức chi tiêu tối thiểu của hạng tiếp theo.</summary>
        public decimal? NextTierMinSpending { get; set; }

        /// <summary>Số tiền cần mua thêm để thăng cấp lên hạng tiếp theo.</summary>
        public decimal AmountToNextTier { get; set; } = 0;

        /// <summary>Phần trăm tiến độ tích lũy để lên hạng tiếp theo (0 - 100%).</summary>
        public decimal TierProgressPercent { get; set; } = 0;
        #endregion
    }
}