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
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // DTO dùng khi Create
    public class WarehouseCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }
        public int? ManagerId { get; set; }
        public bool IsActive { get; set; } = true;

        // Bắt buộc phải có thông tin địa chỉ khi tạo Kho
        public required WarehouseAddressPayload Address { get; set; }
    }

    // DTO dùng khi Update
    public class WarehouseUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }
        public int? ManagerId { get; set; }
        public bool IsActive { get; set; }

        // Có thể cập nhật lại địa chỉ
        public required WarehouseAddressPayload Address { get; set; }
    }
}