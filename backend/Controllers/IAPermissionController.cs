using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/ia-permissions")]
    [ApiController]
    // [Authorize] // Bật lên sau khi test xong
    public class IAPermissionController : ControllerBase
    {
        private readonly SolarisDbContext _context;

        public IAPermissionController(SolarisDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Lấy toàn bộ danh sách 18 quyền sếp vừa Seed trong Database
            var permissions = await _context.IAPermissions
                .AsNoTracking()
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