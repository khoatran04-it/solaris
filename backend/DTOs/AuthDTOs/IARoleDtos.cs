using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin Vai trò (kèm danh sách mã quyền hạn).
    /// </summary>
    public class IARoleReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Danh sách ID các quyền thuộc vai trò này</summary>
        public List<int> PermissionIds { get; set; } = new();
    }

    /// <summary>
    /// DTO tạo mới Vai trò.
    /// </summary>
    public class IARoleCreateDto
    {
        [Required(ErrorMessage = "Mã vai trò không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã vai trò tối đa 50 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên vai trò không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên vai trò tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public List<int> PermissionIds { get; set; } = new();
    }

    /// <summary>
    /// DTO cập nhật thông tin Vai trò và cấu hình lại danh sách quyền hạn.
    /// </summary>
    public class IARoleUpdateDto
    {
        [Required(ErrorMessage = "Tên vai trò không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên vai trò tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public List<int> PermissionIds { get; set; } = new();
    }
}