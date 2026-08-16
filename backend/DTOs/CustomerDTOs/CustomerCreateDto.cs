using backend.DTOs.CustomerAddressDTOs;

namespace backend.DTOs.CustomerDTOs
{
    public class CustomerCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? TaxCode { get; set; }
        public string? AvatarPath { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? Gender { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;

        public int? CustomerTypeId { get; set; }
        public int? CustomerTierId { get; set; }

        // Danh sách nhóm muốn gán ngay khi tạo mới
        public List<int> GroupIds { get; set; } = new List<int>();
    }
}
