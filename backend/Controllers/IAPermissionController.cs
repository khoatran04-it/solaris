using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    /// <summary>
    /// API Truy xuất danh mục Quyền hạn (Permissions) trong hệ thống.
    /// </summary>
    [Route("api/ia-permissions")]
    [ApiController]
    [Authorize]
    public class IAPermissionController : ControllerBase
    {
        private readonly SolarisDbContext _context;

        public IAPermissionController(SolarisDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách quyền hạn được nhóm theo Module.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _context.IAPermissions
                .AsNoTracking()
                .OrderBy(x => x.Module)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Module,
                    x.Code,
                    x.Name
                })
                .ToListAsync();

            return Ok(permissions);
        }
    }
}