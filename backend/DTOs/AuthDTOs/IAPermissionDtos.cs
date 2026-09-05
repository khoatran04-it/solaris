namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin Quyền hạn chi tiết cho giao diện Admin.
    /// </summary>
    public class IAPermissionReadDto
    {
        public int Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
