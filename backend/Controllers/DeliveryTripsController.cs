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
    /// API Quản lý & Điều phối Chuyến xe Giao hàng / Vận chuyển Liên kho (Delivery Trips & Dispatch).
    /// </summary>
    [ApiController]
    [Route("api/delivery-trips")]
    [Authorize]
    [Produces("application/json")]
    public class DeliveryTripsController : ControllerBase
    {
        private readonly IDeliveryTripService _deliveryTripService;

        public DeliveryTripsController(IDeliveryTripService deliveryTripService)
        {
            _deliveryTripService = deliveryTripService;
        }

        /// <summary>
        /// Lấy danh sách các chuyến xe giao hàng / chuyển kho.
        /// </summary>
        /// <param name="status">Lọc theo trạng thái chuyến: Preparing, InTransit, Completed, Cancelled.</param>
        /// <param name="tripType">Lọc theo loại: B2C_Delivery hoặc B2B_Transfer.</param>
        [HttpGet]
        [ProducesResponseType(typeof(List<DeliveryTripReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] string? tripType = null)
        {
            var result = await _deliveryTripService.GetAllTripsAsync(status, tripType);
            return Ok(result);
        }

        /// <summary>
        /// Lấy số liệu KPI tổng hợp cho Bảng điều khiển Vận tải (Transportation & Dispatch Dashboard).
        /// </summary>
        [HttpGet("dashboard-stats")]
        [ProducesResponseType(typeof(TransportationDashboardStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _deliveryTripService.GetTransportationDashboardStatsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một chuyến xe kèm danh sách đơn hàng và lộ trình giao.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var trip = await _deliveryTripService.GetTripByIdAsync(id);
            if (trip == null)
            {
                return NotFound(new { message = $"Không tìm thấy chuyến xe với ID #{id}" });
            }
            return Ok(trip);
        }

        /// <summary>
        /// Khởi tạo một chuyến xe mới (Gom nhiều đơn B2C hoặc tạo chuyến B2B liên kho).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] DeliveryTripCreateDto dto)
        {
            try
            {
                var trip = await _deliveryTripService.CreateTripAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Bắt đầu chuyến xe (Xe rời kho xuất bến -> Trạng thái InTransit).
        /// </summary>
        [HttpPost("{id:int}/start")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartTrip(int id)
        {
            try
            {
                var trip = await _deliveryTripService.StartTripAsync(id);
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu một điểm dừng / đơn hàng trong chuyến xe đã giao thành công.
        /// </summary>
        [HttpPost("{id:int}/orders/{orderId:int}/deliver")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkOrderDelivered(int id, int orderId, [FromBody] MarkOrderDeliveredRequest? request)
        {
            try
            {
                var trip = await _deliveryTripService.MarkTripOrderDeliveredAsync(id, orderId, request?.Note);
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu một điểm dừng / đơn hàng trong chuyến xe giao thất bại hoặc khách từ chối nhận.
        /// </summary>
        [HttpPost("{id:int}/orders/{orderId:int}/failed")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkOrderFailed(int id, int orderId, [FromBody] MarkOrderFailedRequest? request)
        {
            try
            {
                var trip = await _deliveryTripService.MarkTripOrderFailedAsync(id, orderId, request?.Reason ?? "Khách từ chối nhận hàng");
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hoàn tất toàn bộ chuyến xe (Tất cả hàng đã giao xong, xe quay về trạng thái sẵn sàng).
        /// </summary>
        [HttpPost("{id:int}/complete")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteTrip(int id)
        {
            try
            {
                var trip = await _deliveryTripService.CompleteTripAsync(id);
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy bỏ chuyến xe (Giải phóng phương tiện và hoàn trả trạng thái cho các đơn hàng/phiếu trả liên kết).
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelTrip(int id, [FromBody] CancelTripRequest request)
        {
            try
            {
                var trip = await _deliveryTripService.CancelTripAsync(id, request.Reason);
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu tài xế đã thu hồi thành công hàng trả RMA tại nhà khách.
        /// </summary>
        [HttpPost("{id:int}/returns/{returnId:int}/pickup")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkReturnPickedUp(int id, int returnId, [FromBody] MarkReturnPickedUpRequest? request)
        {
            try
            {
                var trip = await _deliveryTripService.MarkTripReturnPickedUpAsync(id, returnId, request?.Note);
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu tài xế thu hồi thất bại hàng trả RMA (khách vắng mặt, không liên lạc được, từ chối trả).
        /// </summary>
        [HttpPost("{id:int}/returns/{returnId:int}/failed")]
        [ProducesResponseType(typeof(DeliveryTripReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkReturnFailed(int id, int returnId, [FromBody] MarkReturnFailedRequest request)
        {
            try
            {
                var result = await _deliveryTripService.MarkTripReturnFailedAsync(id, returnId, request.Reason ?? "Thu hồi không thành công");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class MarkOrderDeliveredRequest
    {
        public string? Note { get; set; }
    }

    public class MarkOrderFailedRequest
    {
        public string Reason { get; set; } = "Khách từ chối nhận hàng";
    }

    public class CancelTripRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class MarkReturnPickedUpRequest
    {
        public string? Note { get; set; }
    }

    public class MarkReturnFailedRequest
    {
        public string Reason { get; set; } = "Khách vắng mặt / không thể thu hồi";
    }
}
