namespace backend.DTOs.CustomerDTOs
{
    public class CustomerUpdateDto
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
        public bool IsActive { get; set; }

        public int? CustomerTypeId { get; set; }
        public int? CustomerTierId { get; set; }

        // Danh sách nhóm mới (sẽ ghi đè danh sách cũ khi Update)
        public List<int> GroupIds { get; set; } = new List<int>();
    }
}
