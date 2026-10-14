using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace TuyenXe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TraCuuTuyenXeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TraCuuTuyenXeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: api/TraCuuTuyenXe
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllRoutes()
        {
            var routes = await _context.BusRoutes
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .OrderBy(x => x.RouteName)
                .Select(x => new
                {
                    x.Id,
                    x.RouteName,
                    x.TotalDistanceKm,
                    x.EstimatedDuration,
                    x.IsActive,
                    DiemDung = x.RouteStops
                        .OrderBy(rs => rs.StopOrder)
                        .Select(rs => new
                        {
                            rs.StopOrder,
                            rs.BusStop!.Id,
                            rs.BusStop.Name,
                            rs.BusStop.Address
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(routes);
        }

        // =====================================================
        // GET: api/TraCuuTuyenXe/{id}
        // =====================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRouteById(int id)
        {
            var route = await _context.BusRoutes
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.RouteName,
                    x.TotalDistanceKm,
                    x.EstimatedDuration,
                    x.IsActive,
                    DiemDung = x.RouteStops
                        .OrderBy(rs => rs.StopOrder)
                        .Select(rs => new
                        {
                            rs.StopOrder,
                            rs.BusStop!.Id,
                            rs.BusStop.Name,
                            rs.BusStop.Address,
                            rs.DistanceFromStartKm,
                            rs.TravelTimeFromStartMinutes
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (route == null)
            {
                return NotFound(new { message = "Không tìm thấy tuyến xe" });
            }

            return Ok(route);
        }

        // =====================================================
        // GET: api/TraCuuTuyenXe/theo-diem
        // US-09, US-31, US-32, US-33: Kiểm tra thứ tự trạm hợp lệ
        // =====================================================
        [HttpGet("theo-diem")]
        public async Task<IActionResult> SearchByStops(
            [FromQuery] string diemDi,
            [FromQuery] string diemDen)
        {
            if (string.IsNullOrWhiteSpace(diemDi) || string.IsNullOrWhiteSpace(diemDen))
            {
                return BadRequest(new { message = "Vui lòng nhập điểm đi và điểm đến" });
            }

            diemDi = diemDi.Trim().ToLower();
            diemDen = diemDen.Trim().ToLower();

            var allActiveRoutes = await _context.BusRoutes
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .Where(r => r.IsActive)
                .ToListAsync();

            // Nghiệp vụ Sprint 1: Tuyến phải có cả 2 trạm và StopOrder(Điểm đi) < StopOrder(Điểm đến)
            var matchedRoutes = allActiveRoutes.Where(r =>
            {
                var startStop = r.RouteStops
                    .FirstOrDefault(rs => rs.BusStop != null && rs.BusStop.Name.ToLower().Contains(diemDi));
                var endStop = r.RouteStops
                    .FirstOrDefault(rs => rs.BusStop != null && rs.BusStop.Name.ToLower().Contains(diemDen));

                return startStop != null && endStop != null && startStop.StopOrder < endStop.StopOrder;
            })
            .Select(route => new
            {
                route.Id,
                route.RouteName,
                route.TotalDistanceKm,
                route.EstimatedDuration,
                DiemDung = route.RouteStops
                    .OrderBy(rs => rs.StopOrder)
                    .Select(rs => new
                    {
                        rs.StopOrder,
                        rs.BusStop!.Id,
                        rs.BusStop.Name,
                        rs.BusStop.Address
                    })
                    .ToList()
            })
            .ToList();

            return Ok(matchedRoutes);
        }
    }
}