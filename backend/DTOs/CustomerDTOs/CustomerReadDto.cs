using backend.DTOs.CustomerAddressDTOs;

namespace backend.DTOs.CustomerDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin Khách Hàng.
    /// </summary>
    public class CustomerReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? TaxCode { get; set; }
        public string? AvatarPath { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? Gender { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Khóa ngoại Nullable
        public int? CustomerTypeId { get; set; }
        public string? CustomerTypeName { get; set; }
        public int? CustomerTierId { get; set; }
        public string? CustomerTierName { get; set; }

        // Mảng chứa các Tên & ID Nhóm khách hàng mà người này đang tham gia
        public List<string> Groups { get; set; } = new List<string>();
        public List<int> GroupIds { get; set; } = new List<int>();

        // Danh sách địa chỉ giao hàng
        public List<CustomerAddressReadDto> Addresses { get; set; } = new List<CustomerAddressReadDto>();
    }
}
