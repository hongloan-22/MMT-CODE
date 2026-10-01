using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Models;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TuyenXeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TuyenXeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TuyenXe
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var danhSach = await _context.Set<BusRoute>()
                .AsNoTracking()
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .OrderBy(x => x.Id)
                .ToListAsync();

            return Ok(danhSach);
        }

        // GET: api/TuyenXe/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tuyen = await _context.Set<BusRoute>()
                .AsNoTracking()
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new { message = "Không tìm thấy tuyến xe" });
            }

            return Ok(tuyen);
        }

        public class CreateRouteDto
        {
            public string RouteName { get; set; } = string.Empty;
            public double TotalDistanceKm { get; set; }
            public TimeSpan EstimatedDuration { get; set; }
            public bool IsActive { get; set; } = true;
            public string? StartStation { get; set; }
            public string? EndStation { get; set; }
        }

        // POST: api/TuyenXe (Tạo tuyến và tự động gắn trạm đầu, trạm cuối)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRouteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RouteName))
                return BadRequest(new { message = "Tên tuyến không được để trống" });

            if (dto.TotalDistanceKm < 0)
                return BadRequest(new { message = "Tổng chiều dài tuyến không được âm" });

            if (dto.EstimatedDuration <= TimeSpan.Zero)
                dto.EstimatedDuration = TimeSpan.FromMinutes(90);

            var route = new BusRoute
            {
                RouteName = dto.RouteName.Trim(),
                TotalDistanceKm = dto.TotalDistanceKm,
                EstimatedDuration = dto.EstimatedDuration,
                IsActive = dto.IsActive,
                RouteStops = new List<RouteStop>()
            };

            _context.Set<BusRoute>().Add(route);
            await _context.SaveChangesAsync();

            async Task<BusStop> GetOrCreateStop(string name)
            {
                var stop = await _context.BusStops.FirstOrDefaultAsync(s => s.Name.ToLower() == name.Trim().ToLower());
                if (stop == null)
                {
                    stop = new BusStop { Name = name.Trim(), Address = $"{name.Trim()}, Việt Nam" };
                    _context.BusStops.Add(stop);
                    await _context.SaveChangesAsync();
                }
                return stop;
            }

            if (!string.IsNullOrWhiteSpace(dto.StartStation))
            {
                var sStop = await GetOrCreateStop(dto.StartStation);
                _context.RouteStops.Add(new RouteStop
                {
                    RouteId = route.Id,
                    StopId = sStop.Id,
                    StopOrder = 1,
                    DistanceFromStartKm = 0,
                    TravelTimeFromStartMinutes = 0
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.EndStation) && dto.EndStation.Trim().ToLower() != dto.StartStation?.Trim().ToLower())
            {
                var eStop = await GetOrCreateStop(dto.EndStation);
                _context.RouteStops.Add(new RouteStop
                {
                    RouteId = route.Id,
                    StopId = eStop.Id,
                    StopOrder = 2,
                    DistanceFromStartKm = dto.TotalDistanceKm,
                    TravelTimeFromStartMinutes = (int)dto.EstimatedDuration.TotalMinutes
                });
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = route.Id }, route);
        }

        public class AddStopDto
        {
            public int RouteId { get; set; }
            public string StopName { get; set; } = string.Empty;
            public int StopOrder { get; set; }
            public double DistanceKm { get; set; }
        }

        // POST: api/TuyenXe/them-tram-vao-tuyen (Tự động dời các trạm sau lên +1 nếu chèn vào giữa)
        [HttpPost("them-tram-vao-tuyen")]
        public async Task<IActionResult> ThemTramVaoTuyen([FromBody] AddStopDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.StopName))
                return BadRequest(new { message = "Tên trạm không được rỗng" });

            // 1. Tìm hoặc tạo trạm trong BusStops
            var stop = await _context.BusStops.FirstOrDefaultAsync(s => s.Name.ToLower() == dto.StopName.Trim().ToLower());
            if (stop == null)
            {
                stop = new BusStop { Name = dto.StopName.Trim(), Address = $"{dto.StopName.Trim()}, Việt Nam" };
                _context.BusStops.Add(stop);
                await _context.SaveChangesAsync();
            }

            // 2. Tự động dịch chuyển các trạm có StopOrder >= vị trí cần chèn lên +1
            var existingStops = await _context.RouteStops
                .Where(x => x.RouteId == dto.RouteId && x.StopOrder >= dto.StopOrder)
                .ToListAsync();

            foreach (var s in existingStops)
            {
                s.StopOrder += 1;
            }

            // 3. Thêm trạm mới vào đúng vị trí StopOrder yêu cầu
            var rs = new RouteStop
            {
                RouteId = dto.RouteId,
                StopId = stop.Id,
                StopOrder = dto.StopOrder,
                DistanceFromStartKm = dto.DistanceKm,
                TravelTimeFromStartMinutes = (int)(dto.DistanceKm * 1.5)
            };

            _context.RouteStops.Add(rs);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm trạm thành công", stopId = stop.Id });
        }

        // DELETE: api/TuyenXe/xoa-tram-khoi-tuyen?routeId=1&stopId=2 (Tự động dồn các trạm phía sau xuống -1)
        [HttpDelete("xoa-tram-khoi-tuyen")]
        public async Task<IActionResult> XoaTramKhoiTuyen([FromQuery] int routeId, [FromQuery] int stopId)
        {
            var rs = await _context.RouteStops.FirstOrDefaultAsync(x => x.RouteId == routeId && x.StopId == stopId);
            if (rs == null) return NotFound(new { message = "Không tìm thấy trạm trong tuyến" });

            int deletedOrder = rs.StopOrder;
            _context.RouteStops.Remove(rs);

            // Dồn thứ tự các trạm phía sau xuống -1
            var trailingStops = await _context.RouteStops
                .Where(x => x.RouteId == routeId && x.StopOrder > deletedOrder)
                .ToListAsync();

            foreach (var s in trailingStops)
            {
                s.StopOrder -= 1;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa trạm khỏi tuyến" });
        }

        // PUT: api/TuyenXe/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BusRoute route)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var tuyen = await _context.Set<BusRoute>().FirstOrDefaultAsync(x => x.Id == id);
            if (tuyen == null)
                return NotFound(new { message = "Không tìm thấy tuyến xe" });

            tuyen.RouteName = route.RouteName.Trim();
            tuyen.TotalDistanceKm = route.TotalDistanceKm;
            tuyen.EstimatedDuration = route.EstimatedDuration;
            tuyen.IsActive = route.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật tuyến xe thành công", data = tuyen });
        }

        // DELETE: api/TuyenXe/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tuyen = await _context.Set<BusRoute>()
                .Include(x => x.RouteStops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
                return NotFound(new { message = "Không tìm thấy tuyến xe" });

            if (tuyen.RouteStops.Any())
            {
                _context.RouteStops.RemoveRange(tuyen.RouteStops);
            }

            _context.Set<BusRoute>().Remove(tuyen);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa tuyến xe thành công" });
        }
    }
}