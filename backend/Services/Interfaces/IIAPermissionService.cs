using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý và truy xuất danh mục Quyền hạn (IAM Permissions).
    /// </summary>
    public interface IIAPermissionService
    {
        /// <summary>
        /// Lấy toàn bộ danh mục quyền hạn trong hệ thống, tự động đồng bộ từ Enum SystemPermission vào Database nếu chưa tồn tại.
        /// </summary>
        Task<IEnumerable<IAPermissionReadDto>> GetAllListAsync();
    }
}
