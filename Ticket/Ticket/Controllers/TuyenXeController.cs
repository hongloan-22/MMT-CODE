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
            var danhSach = await _context.BusRoutes
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
            var tuyen = await _context.BusRoutes
                .AsNoTracking()
                .Include(x => x.RouteStops.OrderBy(rs => rs.StopOrder))
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

        // POST: api/TuyenXe
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

            _context.BusRoutes.Add(route);
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

        // POST: api/TuyenXe/them-tram-vao-tuyen
        [HttpPost("them-tram-vao-tuyen")]
        public async Task<IActionResult> ThemTramVaoTuyen([FromBody] AddStopDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.StopName))
                return BadRequest(new { message = "Tên trạm không được rỗng" });

            if (dto.DistanceKm < 0)
                return BadRequest(new { message = "Khoảng cách không được là số âm." });

            var route = await _context.BusRoutes.FirstOrDefaultAsync(r => r.Id == dto.RouteId);
            if (route == null) return NotFound(new { message = "Tuyến xe không tồn tại." });

            var currentStops = await _context.RouteStops
                .Where(x => x.RouteId == dto.RouteId)
                .OrderBy(x => x.StopOrder)
                .ToListAsync();

            int targetOrder = dto.StopOrder;
            if (targetOrder <= 0 || targetOrder > currentStops.Count + 1)
            {
                targetOrder = currentStops.Count + 1;
            }

            var stop = await _context.BusStops.FirstOrDefaultAsync(s => s.Name.ToLower() == dto.StopName.Trim().ToLower());
            if (stop == null)
            {
                stop = new BusStop { Name = dto.StopName.Trim(), Address = $"{dto.StopName.Trim()}, Việt Nam" };
                _context.BusStops.Add(stop);
                await _context.SaveChangesAsync();
            }

            if (currentStops.Any(x => x.StopId == stop.Id))
            {
                return BadRequest(new { message = "Trạm này đã tồn tại trong tuyến!" });
            }

            foreach (var s in currentStops.Where(x => x.StopOrder >= targetOrder))
            {
                s.StopOrder += 1;
                _context.Update(s);
            }

            var rs = new RouteStop
            {
                RouteId = dto.RouteId,
                StopId = stop.Id,
                StopOrder = targetOrder,
                DistanceFromStartKm = dto.DistanceKm,
                TravelTimeFromStartMinutes = (int)(dto.DistanceKm * 1.5)
            };

            _context.RouteStops.Add(rs);
            await _context.SaveChangesAsync();

            // Lấy lại danh sách đầy đủ sau khi thêm để tính đúng cự ly lớn nhất
            var finalStops = await _context.RouteStops
                .Where(x => x.RouteId == dto.RouteId)
                .OrderBy(x => x.StopOrder)
                .ToListAsync();

            for (int i = 0; i < finalStops.Count; i++)
            {
                finalStops[i].StopOrder = i + 1;
                _context.Update(finalStops[i]);
            }

            route.TotalDistanceKm = finalStops.Any() ? finalStops.Max(s => s.DistanceFromStartKm) : 0;
            _context.Update(route);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm trạm thành công!", stopId = stop.Id, assignedOrder = targetOrder });
        }

        // DELETE: api/TuyenXe/xoa-tram-khoi-tuyen
        [HttpDelete("xoa-tram-khoi-tuyen")]
        public async Task<IActionResult> XoaTramKhoiTuyen([FromQuery] int routeId, [FromQuery] int stopId)
        {
            var rs = await _context.RouteStops.FirstOrDefaultAsync(x => x.RouteId == routeId && x.StopId == stopId);
            if (rs == null) return NotFound(new { message = "Không tìm thấy trạm trong tuyến" });

            int deletedOrder = rs.StopOrder;

            // 1. Xóa bản ghi trạm khỏi RouteStops
            _context.RouteStops.Remove(rs);
            await _context.SaveChangesAsync();

            // 2. Lấy danh sách các trạm còn lại theo thứ tự
            var remainingStops = await _context.RouteStops
                .Where(x => x.RouteId == routeId)
                .OrderBy(x => x.StopOrder)
                .ToListAsync();

            var route = await _context.BusRoutes.FirstOrDefaultAsync(r => r.Id == routeId);

            if (remainingStops.Any())
            {
                // Đánh lại StopOrder liên tục: 1, 2, 3...
                for (int i = 0; i < remainingStops.Count; i++)
                {
                    remainingStops[i].StopOrder = i + 1;
                }

                // NẾU XÓA TRẠM ĐẦU TIÊN (deletedOrder == 1):
                // Trạm kế tiếp (giờ là remainingStops[0]) trở thành mốc 0 km.
                // Trừ lùi toàn bộ cự ly của các trạm sau theo khoảng cách của trạm này.
                if (deletedOrder == 1)
                {
                    double offset = remainingStops[0].DistanceFromStartKm;
                    for (int i = 0; i < remainingStops.Count; i++)
                    {
                        remainingStops[i].DistanceFromStartKm = Math.Round(Math.Max(0, remainingStops[i].DistanceFromStartKm - offset), 2);
                        _context.Update(remainingStops[i]); // Ép EF theo dõi cập nhật
                    }
                }
                else
                {
                    // Nếu xóa trạm giữa hoặc cuối, vẫn đánh dấu update thứ tự
                    for (int i = 0; i < remainingStops.Count; i++)
                    {
                        _context.Update(remainingStops[i]);
                    }
                }

                if (route != null)
                {
                    // Lấy cự ly trạm cuối cùng làm TotalDistanceKm mới
                    route.TotalDistanceKm = remainingStops.Max(s => s.DistanceFromStartKm);
                    _context.Update(route); // Ép EF cập nhật BusRoute
                }
            }
            else
            {
                // Nếu xóa hết sạch trạm trong tuyến
                if (route != null)
                {
                    route.TotalDistanceKm = 0;
                    _context.Update(route);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã xóa trạm khỏi tuyến và tự động cập nhật lại toàn bộ cự ly!",
                newTotalDistanceKm = route?.TotalDistanceKm
            });
        }

        // PUT: api/TuyenXe/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BusRoute route)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var tuyen = await _context.BusRoutes.FirstOrDefaultAsync(x => x.Id == id);
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
            var tuyen = await _context.BusRoutes
                .Include(x => x.RouteStops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
                return NotFound(new { message = "Không tìm thấy tuyến xe cần xóa." });

            try
            {
                // 1. Dọn dẹp trong BusSchedules nếu có liên kết RouteId
                try
                {
                    var busSchedules = await _context.BusSchedules
                        .Where(bs => EF.Property<int>(bs, "RouteId") == id)
                        .ToListAsync();
                    if (busSchedules.Any())
                    {
                        _context.BusSchedules.RemoveRange(busSchedules);
                    }
                }
                catch { }

                // 2. Tìm và xóa toàn bộ Trips cùng TripSchedules của tuyến
                var tripIds = await _context.Trips
                    .Where(t => t.RouteId == id)
                    .Select(t => t.Id)
                    .ToListAsync();

                if (tripIds.Any())
                {
                    var schedules = await _context.TripSchedules
                        .Where(s => tripIds.Contains(s.TripId))
                        .ToListAsync();
                    if (schedules.Any())
                    {
                        _context.TripSchedules.RemoveRange(schedules);
                    }

                    var trips = await _context.Trips
                        .Where(t => tripIds.Contains(t.Id))
                        .ToListAsync();
                    if (trips.Any())
                    {
                        _context.Trips.RemoveRange(trips);
                    }
                }

                // 3. Xóa các trạm dừng trong tuyến
                if (tuyen.RouteStops != null && tuyen.RouteStops.Any())
                {
                    _context.RouteStops.RemoveRange(tuyen.RouteStops);
                }

                // 4. Xóa tuyến xe
                _context.BusRoutes.Remove(tuyen);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa tuyến xe thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa tuyến: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }
    }
}