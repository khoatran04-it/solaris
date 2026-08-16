using backend.DTOs.SupplierAddressDTOs;

namespace backend.DTOs.SupplierDTOs
{
    public class SupplierCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? Website { get; set; }
        public string? SocialLink { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;

        // --- FOREIGN KEY ---
        public int? SupplierTypeId { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public List<SupplierAddressCreateDto>? Addresses { get; set; }
    }
}