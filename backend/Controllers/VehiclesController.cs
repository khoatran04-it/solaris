using backend.DTOs.VehicleDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Đội Xe Giao Hàng & Phương Tiện Chuỗi Lạnh (Delivery Vehicles & Cold Fleet).
    /// Hỗ trợ Xe máy thùng lạnh (Motorbike) cho B2C và Xe tải lạnh chuyên dụng (RefrigeratedTruck) cho B2B.
    /// </summary>
    [ApiController]
    [Route("api/vehicles")]
    [Authorize]
    [Produces("application/json")]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ phương tiện giao hàng trong đội xe.
        /// </summary>
        /// <param name="vehicleType">Lọc theo loại xe: Motorbike hoặc RefrigeratedTruck.</param>
        /// <param name="status">Lọc theo trạng thái: Available, OnTrip, Maintenance.</param>
        [HttpGet]
        [ProducesResponseType(typeof(List<DeliveryVehicleReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? vehicleType = null, [FromQuery] string? status = null)
        {
            var result = await _vehicleService.GetAllAsync(vehicleType, status);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết phương tiện theo ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DeliveryVehicleReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound(new { message = $"Không tìm thấy phương tiện với ID #{id}" });
            }
            return Ok(vehicle);
        }

        /// <summary>
        /// Đăng ký phương tiện mới vào đội xe nội bộ.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DeliveryVehicleReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] DeliveryVehicleCreateDto dto)
        {
            try
            {
                var vehicle = await _vehicleService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin phương tiện, tài xế phụ trách hoặc trạng thái bảo dưỡng.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(DeliveryVehicleReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] DeliveryVehicleUpdateDto dto)
        {
            try
            {
                var vehicle = await _vehicleService.UpdateAsync(id, dto);
                if (vehicle == null)
                {
                    return NotFound(new { message = $"Không tìm thấy phương tiện với ID #{id}" });
                }
                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa mềm phương tiện khỏi đội xe.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _vehicleService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy phương tiện với ID #{id}" });
            }
            return Ok(new { message = "Xóa phương tiện thành công" });
        }
    }
}
