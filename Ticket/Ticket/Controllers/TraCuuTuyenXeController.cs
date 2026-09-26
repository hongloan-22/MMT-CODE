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
        // Lấy tất cả tuyến xe
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
        // GET:
        // api/TraCuuTuyenXe/{id}
        //
        // Tra cứu một tuyến theo ID
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
                return NotFound(new
                {
                    message = "Không tìm thấy tuyến xe"
                });
            }

            return Ok(route);
        }


        // =====================================================
        // GET:
        // api/TraCuuTuyenXe/tim-kiem?keyword=Tuyến 01
        //
        // Tìm kiếm theo tên tuyến
        // =====================================================
        [HttpGet("tim-kiem")]
        public async Task<IActionResult> SearchRoute(
            [FromQuery] string? keyword)
        {
            var query = _context.BusRoutes
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.RouteName.Contains(keyword));
            }

            var routes = await query
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
        // GET:
        // api/TraCuuTuyenXe/theo-diem
        //
        // Tìm tuyến theo điểm đi và điểm đến
        //
        // Ví dụ:
        // ?diemDi=Ben xe My Dinh&diemDen=Yen Nghia
        // =====================================================
        [HttpGet("theo-diem")]
        public async Task<IActionResult> SearchByStops(
            [FromQuery] string diemDi,
            [FromQuery] string diemDen)
        {
            if (string.IsNullOrWhiteSpace(diemDi) ||
                string.IsNullOrWhiteSpace(diemDen))
            {
                return BadRequest(new
                {
                    message = "Vui lòng nhập điểm đi và điểm đến"
                });
            }

            diemDi = diemDi.Trim();
            diemDen = diemDen.Trim();

            var routes = await _context.BusRoutes
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .Where(route =>
                    route.IsActive &&

                    route.RouteStops.Any(rs =>
                        rs.BusStop!.Name.Contains(diemDi)) &&

                    route.RouteStops.Any(rs =>
                        rs.BusStop!.Name.Contains(diemDen))
                )
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
                .ToListAsync();

            return Ok(routes);
        }


        // =====================================================
        // GET:
        // api/TraCuuTuyenXe/dang-hoat-dong
        //
        // Lấy các tuyến đang hoạt động
        // =====================================================
        [HttpGet("dang-hoat-dong")]
        public async Task<IActionResult> GetActiveRoutes()
        {
            var routes = await _context.BusRoutes
                .Where(x => x.IsActive)
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .OrderBy(x => x.RouteName)
                .Select(x => new
                {
                    x.Id,
                    x.RouteName,
                    x.TotalDistanceKm,
                    x.EstimatedDuration,

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
    }
}