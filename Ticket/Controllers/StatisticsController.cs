using Microsoft.AspNetCore.Mvc;
using SmartBusTicketing.Services;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly BackendAuthorizationService _authorization;

        public StatisticsController(
            ITripService tripService,
            BackendAuthorizationService authorization)
        {
            _tripService = tripService;
            _authorization = authorization;
        }

        // =====================================================
        // LẤY TỶ LỆ LẤP ĐẦY CỦA MỘT CHUYẾN XE (US-160)
        // GET: /api/Statistics/trip/{tripId}/occupancy
        // =====================================================
        [HttpGet("trip/{tripId:int}/occupancy")]
        public async Task<IActionResult> GetTripOccupancy(
            int tripId,
            [FromHeader(Name = "X-User-Id")] string? userId)
        {
            // Chỉ MANAGER hoặc ADMIN mới được xem thống kê
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER",
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền xem thống kê."
                });
            }

            try
            {
                var result =
                    await _tripService
                        .GetTripOccupancyAsync(tripId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }
    }
}