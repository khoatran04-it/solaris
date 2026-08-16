namespace backend.DTOs.AuthDTOs
{
    // DTO Đọc dữ liệu Vai trò (kèm danh sách mã quyền mặc định)
    public class IARoleReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Danh sách ID các quyền mặc định của Vai trò này
        public List<int> PermissionIds { get; set; } = new();
    }

    public class IARoleCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Chọn sẵn các quyền khi tạo mới Role
        public List<int> PermissionIds { get; set; } = new();
    }

    public class IARoleUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // Để Frontend ghi đè lại danh sách quyền của Role
        public List<int> PermissionIds { get; set; } = new();
    }
}