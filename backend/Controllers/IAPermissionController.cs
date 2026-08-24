using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    /// <summary>
    /// API Truy xuất danh mục Quyền hạn (Permissions) phục vụ cấu hình ma trận phân quyền.
    /// </summary>
    [ApiController]
    [Route("api/ia-permissions")]
    [Authorize]
    [Produces("application/json")]
    public class IAPermissionController : ControllerBase
    {
        private readonly SolarisDbContext _context;

        public IAPermissionController(SolarisDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách quyền hạn chi tiết trong hệ thống (đã sắp xếp theo phân hệ Module).
        /// </summary>
        /// <response code="200">Danh sách quyền hạn được nhóm theo thứ tự Module.</response>
        /// <response code="401">Chưa xác thực hoặc JWT Token không hợp lệ.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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