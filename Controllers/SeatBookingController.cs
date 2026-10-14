using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ticket.Models;
using Ticket.Service;

namespace Ticket.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatBookingController : ControllerBase
    {
        private readonly ISeatBookingService _seatBookingService;

        public SeatBookingController(ISeatBookingService seatBookingService)
        {
            _seatBookingService = seatBookingService;
        }

        /// <summary>
        /// API 1: Lấy sơ đồ và trạng thái ghế theo chuyến
        /// GET: /api/SeatBooking/seat-map/TRIP01
        /// </summary>
        [HttpGet("seat-map/{tripId}")]
        public IActionResult GetSeatMap(string tripId)
        {
            var result = _seatBookingService.GetSeatMap(tripId);

            // 🛑 Xử lý báo lỗi 404 Not Found nếu điền sai mã TRIP hoặc không tìm thấy dữ liệu
            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = $"Mã chuyến '{tripId}' không hợp lệ hoặc không tồn tại! Định dạng bắt buộc phải là TRIP... (Ví dụ: TRIP01)."
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// API 2: Tạo giữ chỗ tạm thời (Mặc định 10 phút)
        /// POST: /api/SeatBooking/hold
        /// </summary>
        [HttpPost("hold")]
        public IActionResult CreateHold([FromBody] CreateHoldRequestDto request)
        {
            if (request == null || request.SeatIds == null || !request.SeatIds.Any())
            {
                return BadRequest("Danh sách ghế không được để trống!");
            }

            var result = _seatBookingService.CreateHold(request);
            if (result.Status == "FAILED")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// API 3: Kiểm tra trạng thái mã giữ chỗ
        /// GET: /api/SeatBooking/hold/HOLD_ID_HERE
        /// </summary>
        [HttpGet("hold/{holdId}")]
        public IActionResult CheckHoldStatus(string holdId)
        {
            var result = _seatBookingService.CheckHoldStatus(holdId);

            if (result.Status == "NOT_FOUND")
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// API 4: Giải phóng (Hủy) giữ chỗ
        /// POST: /api/SeatBooking/release/HOLD_ID_HERE
        /// </summary>
        [HttpPost("release/{holdId}")]
        public IActionResult ReleaseHold(string holdId)
        {
            var success = _seatBookingService.ReleaseHold(holdId, out string message);
            if (!success)
            {
                return BadRequest(new { Success = false, Message = message });
            }

            return Ok(new { Success = true, Message = message });
        }
    }
}