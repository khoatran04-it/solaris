using backend.DTOs.SupplierAddressDTOs;

namespace backend.DTOs.SupplierDTOs
{
    public class SupplierReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public string? TaxCode { get; set; }
        public string? Website {  get; set; }
        public string? SocialLink { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // --- FOREIGN KEY ---
        public int SupplierTypeId { get; set; }
        public string? SupplierTypeName { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public List<SupplierAddressReadDto> Addresses { get; set; } = new();
    }
}
