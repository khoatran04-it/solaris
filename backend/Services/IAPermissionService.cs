using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Data;
using backend.DTOs.AuthDTOs;
using backend.Enums;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Dịch vụ xử lý truy xuất và tự động đồng bộ danh mục Quyền hạn (IAM Permissions) dựa trên Enum SystemPermission.
    /// </summary>
    public class IAPermissionService : IIAPermissionService
    {
        private readonly SolarisDbContext _context;

        public IAPermissionService(SolarisDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<IAPermissionReadDto>> GetAllListAsync()
        {
            // Đồng bộ và làm sạch danh mục quyền từ Enum SystemPermission
            await DbInitializer.SeedAsync(_context);

            // Trả về danh sách quyền hạn đã sắp xếp theo số thứ tự phân hệ Module
            var permissions = await _context.IAPermissions
                .AsNoTracking()
                .ToListAsync();

            return permissions
                .OrderBy(x => ExtractModuleOrder(x.Module))
                .ThenBy(x => x.Id)
                .Select(x => new IAPermissionReadDto
                {
                    Id = x.Id,
                    Module = x.Module,
                    Code = x.Code,
                    Name = x.Name
                });
        }

        private static int ExtractModuleOrder(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return 999;
            var dotIdx = moduleName.IndexOf('.');
            if (dotIdx > 0 && int.TryParse(moduleName.Substring(0, dotIdx).Trim(), out var order))
            {
                return order;
            }
            return 999;
        }
    }
}
