using Microsoft.AspNetCore.Mvc;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;

namespace TuyenXe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TraCuuTuyenXeController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TraCuuTuyenXeController(
            ITripService tripService)
        {
            _tripService = tripService;
        }

        // =====================================================
        // GET: api/TraCuuTuyenXe
        // =====================================================
        [HttpGet]
        public IActionResult GetAllRoutes()
        {
            return Ok(new
            {
                message =
                    "API tra cứu chính sử dụng /api/TraCuuTuyenXe/theo-diem"
            });
        }

        // =====================================================
        // GET: api/TraCuuTuyenXe/theo-diem
        //
        // Ví dụ:
        // /api/TraCuuTuyenXe/theo-diem
        // ?diemDi=Bến xe Gia Lâm
        // &diemDen=Bến xe Yên Nghĩa
        // &ngay=2026-10-02
        // &gio=08:00
        //
        // US-09
        // =====================================================
        [HttpGet("theo-diem")]
        public async Task<IActionResult> SearchByStops(
            [FromQuery] string diemDi,
            [FromQuery] string diemDen,
            [FromQuery] DateTime? ngay,
            [FromQuery] TimeSpan? gio)
        {
            if (string.IsNullOrWhiteSpace(diemDi) ||
                string.IsNullOrWhiteSpace(diemDen))
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập điểm đi và điểm đến."
                });
            }

            if (!ngay.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng chọn ngày đi."
                });
            }

            if (ngay.Value.Date < DateTime.Today)
            {
                return BadRequest(new
                {
                    message =
                        "Ngày đi không được ở trong quá khứ."
                });
            }

            var request =
                new TripSearchRequestDto
                {
                    OriginKeyword =
                        diemDi.Trim(),

                    DestinationKeyword =
                        diemDen.Trim(),

                    DepartureDate =
                        ngay.Value.Date,

                    DepartureTime =
                        gio
                };

            var result =
                await _tripService
                    .SearchTripsByOriginAsync(
                        request);

            return Ok(result);
        }
    }
}