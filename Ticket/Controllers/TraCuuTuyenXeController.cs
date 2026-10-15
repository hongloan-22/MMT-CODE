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
                    "API tra cứu chính sử dụng /api/TraCuuTuyenXe/theo-diem và /api/TraCuuTuyenXe/danh-sach"
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

        // =====================================================
        // US-165: DANH SÁCH CHUYẾN ĐÃ LÊN LỊCH (PHÂN TRANG)
        //
        // Ví dụ:
        // /api/TraCuuTuyenXe/danh-sach
        // ?diemDi=Bến xe Gia Lâm
        // &diemDen=Bến xe Yên Nghĩa
        // &ngay=2026-10-20
        // &gioMin=06:00
        // &giaMin=50000
        // &giaMax=200000
        // &sortBy=price
        // &sortOrder=asc
        // &page=1
        // &pageSize=20
        // =====================================================
        [HttpGet("danh-sach")]
        public async Task<IActionResult> GetTripList(
            [FromQuery] string? diemDi,
            [FromQuery] string? diemDen,
            [FromQuery] DateTime? ngay,
            [FromQuery] TimeSpan? gioMin,
            [FromQuery] decimal? giaMin,
            [FromQuery] decimal? giaMax,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Validate phân trang
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            // Validate giá
            if (giaMin.HasValue && giaMin.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Giá tối thiểu không được âm."
                });
            }

            if (giaMax.HasValue && giaMax.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Giá tối đa không được âm."
                });
            }

            if (giaMin.HasValue && giaMax.HasValue &&
                giaMin.Value > giaMax.Value)
            {
                return BadRequest(new
                {
                    message = "Giá tối thiểu không được lớn hơn giá tối đa."
                });
            }

            // Validate ngày
            if (ngay.HasValue && ngay.Value.Date < DateTime.Today)
            {
                return BadRequest(new
                {
                    message = "Ngày đi không được ở trong quá khứ."
                });
            }

            var request = new TripListRequestDto
            {
                OriginKeyword = diemDi?.Trim(),
                DestinationKeyword = diemDen?.Trim(),
                DepartureDate = ngay?.Date,
                MinDepartureTime = gioMin,
                MinFare = giaMin,
                MaxFare = giaMax,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _tripService.GetTripListAsync(request);

            return Ok(result);
        }
    }
}