using Microsoft.AspNetCore.Mvc;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;

namespace SmartBusTicketing.Controllers.Api
{
    [ApiController]
    [Route("api/trips")]
    public class TripsApiController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripsApiController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTrips([FromQuery] TripSearchRequestDto request)
        {
            try
            {

                var trips = await _tripService.SearchTripsByOriginAsync(request);

                if (trips == null || !trips.Any())
                {
                    return NotFound(new { message = "Không tìm thấy chuyến xe nào xuất phát từ điểm đi này." });
                }

                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý.", error = ex.Message });
            }
        }
    }
}