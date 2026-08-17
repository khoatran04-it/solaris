using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.InventoryDTOs
{
    // DTO dùng cho Read (Get List, Get Detail)
    public class WarehouseReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Thông tin Trưởng kho
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; } // Map từ IAUser.FullName

        // Thông tin Địa chỉ (Gộp chung vào đây cho FE dễ đọc)
        public int AddressId { get; set; }
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty; // Cột tính toán
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // DTO phụ để chứa Address khi tạo/sửa
    public class WarehouseAddressPayload
    {
        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống.")]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố tối đa 100 ký tự.")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện không được để trống.")]
        [StringLength(100, ErrorMessage = "Quận/Huyện tối đa 100 ký tự.")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã không được để trống.")]
        [StringLength(100, ErrorMessage = "Phường/Xã tối đa 100 ký tự.")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống.")]
        [StringLength(255, ErrorMessage = "Địa chỉ chi tiết tối đa 255 ký tự.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ (Latitude) phải nằm trong khoảng [-90, 90].")]
        public double Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ (Longitude) phải nằm trong khoảng [-180, 180].")]
        public double Longitude { get; set; }
    }

    // DTO dùng khi Create
    public class WarehouseCreateDto
    {
        [Required(ErrorMessage = "Mã kho không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã kho tối đa 50 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kho hàng không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên kho tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Loại kho tối đa 50 ký tự.")]
        public string? WarehouseType { get; set; }

        public int? ManagerId { get; set; }
        public bool IsActive { get; set; } = true;

        // Bắt buộc phải có thông tin địa chỉ khi tạo Kho
        [Required(ErrorMessage = "Thông tin địa chỉ kho không được để trống.")]
        public required WarehouseAddressPayload Address { get; set; }
    }

    // DTO dùng khi Update
    public class WarehouseUpdateDto
    {
        [Required(ErrorMessage = "Tên kho hàng không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên kho tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Loại kho tối đa 50 ký tự.")]
        public string? WarehouseType { get; set; }

        public int? ManagerId { get; set; }
        public bool IsActive { get; set; }

        // Có thể cập nhật lại địa chỉ
        [Required(ErrorMessage = "Thông tin địa chỉ kho không được để trống.")]
        public required WarehouseAddressPayload Address { get; set; }
    }
}