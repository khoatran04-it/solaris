using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Cổng truy xuất danh mục Quyền hạn (Permissions) phục vụ cấu hình ma trận phân quyền.
    /// </summary>
    [ApiController]
    [Route("api/ia-permissions")]
    [Authorize]
    [Produces("application/json")]
    public class IAPermissionController : ControllerBase
    {
        private readonly IIAPermissionService _permissionService;

        public IAPermissionController(IIAPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách quyền hạn chi tiết trong hệ thống (đã sắp xếp theo phân hệ Module).
        /// </summary>
        /// <response code="200">Danh sách quyền hạn được nhóm theo thứ tự Module.</response>
        /// <response code="401">Chưa xác thực hoặc JWT Token không hợp lệ.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<IAPermissionReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _permissionService.GetAllListAsync();
            return Ok(result);
        }
    }
}