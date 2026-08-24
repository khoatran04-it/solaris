namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết vai trò kèm danh sách ID quyền hạn trực thuộc.
    /// </summary>
    public class IARoleReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<int> PermissionIds { get; set; } = new();
    }

    /// <summary>
    /// DTO yêu cầu tạo mới vai trò và gán danh sách quyền ban đầu.
    /// </summary>
    public class IARoleCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public List<int> PermissionIds { get; set; } = new();
    }

    /// <summary>
    /// DTO yêu cầu cập nhật thông tin và danh sách quyền hạn của vai trò.
    /// </summary>
    public class IARoleUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public List<int> PermissionIds { get; set; } = new();
    }
}