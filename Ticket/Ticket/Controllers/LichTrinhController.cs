using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Models;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LichTrinhController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LichTrinhController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLichTrinh()
        {
            try
            {
                var list = await _context.TripSchedules
                    .AsNoTracking()
                    .Include(x => x.BusStop)
                    .Include(x => x.Trip)
                        .ThenInclude(t => t!.Route)
                    .OrderBy(x => x.TripId)
                    .ThenBy(x => x.StopOrder)
                    .ToListAsync();

                var result = list.Select(s => new
                {
                    id = s.Id,
                    tripId = s.TripId,
                    stopId = s.StopId,
                    stopOrder = s.StopOrder,
                    arrivalTime = s.ArrivalTime.ToString(@"hh\:mm"),
                    departureTime = s.DepartureTime.ToString(@"hh\:mm"),
                    status = string.IsNullOrEmpty(s.Status) ? "SCHEDULED" : s.Status,
                    busStop = s.BusStop != null ? new
                    {
                        id = s.BusStop.Id,
                        name = s.BusStop.Name
                    } : null,
                    trip = s.Trip != null ? new
                    {
                        id = s.Trip.Id,
                        tripCode = s.Trip.TripCode ?? $"TRIP-{s.Trip.Id}",
                        routeId = s.Trip.RouteId,
                        tripDate = s.Trip.TripDate.ToString("yyyy-MM-dd"),
                        departureTime = s.Trip.DepartureTime.ToString(@"hh\:mm"),
                        route = s.Trip.Route != null ? new
                        {
                            id = s.Trip.RouteId,
                            routeName = s.Trip.Route.RouteName
                        } : null
                    } : null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetTrips()
        {
            try
            {
                var rawList = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Route)
                    .ToListAsync();

                var list = rawList
                    .OrderBy(t => t.TripDate)
                    .ThenBy(t => t.DepartureTime)
                    .ToList();

                var result = list.Select(t => new
                {
                    id = t.Id,
                    tripCode = t.TripCode ?? $"TRIP-{t.Id}",
                    routeId = t.RouteId,
                    tripDate = t.TripDate.ToString("yyyy-MM-dd"),
                    departureTime = t.DepartureTime.ToString(@"hh\:mm"),
                    route = t.Route != null ? new
                    {
                        id = t.RouteId,
                        routeName = t.Route.RouteName
                    } : null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("quick-create")]
        public async Task<IActionResult> QuickCreateSchedule([FromBody] QuickScheduleRequest req)
        {
            if (req.RouteId <= 0)
                return BadRequest(new { message = "Vui lòng chọn tuyến xe hợp lệ." });

            var route = await _context.BusRoutes.FirstOrDefaultAsync(r => r.Id == req.RouteId);
            if (route == null)
                return NotFound(new { message = "Tuyến xe không tồn tại." });

            if (!TimeSpan.TryParse(req.DepartureTime, out var startTime))
            {
                startTime = new TimeSpan(8, 0, 0);
            }

            // Cập nhật số km của tuyến nếu người dùng chỉ định
            if (req.TotalDistanceKm > 0)
            {
                route.TotalDistanceKm = req.TotalDistanceKm;
            }

            var newTrip = new Trip
            {
                RouteId = req.RouteId,
                TripCode = string.IsNullOrWhiteSpace(req.TripCode) ? $"TRIP-{DateTime.Now:HHmmss}" : req.TripCode.Trim(),
                TripDate = req.TripDate == default ? DateTime.Today : req.TripDate,
                DepartureTime = startTime
            };

            _context.Trips.Add(newTrip);
            await _context.SaveChangesAsync();

            var stops = await _context.RouteStops
                .Where(rs => rs.RouteId == req.RouteId)
                .OrderBy(rs => rs.StopOrder)
                .ToListAsync();

            if (stops.Any())
            {
                var curTime = startTime;
                var list = new List<TripSchedule>();

                for (int i = 0; i < stops.Count; i++)
                {
                    var stop = stops[i];
                    var arrTime = curTime;
                    var depTime = (i == stops.Count - 1) ? arrTime : arrTime.Add(TimeSpan.FromMinutes(5));

                    list.Add(new TripSchedule
                    {
                        TripId = newTrip.Id,
                        StopId = stop.StopId,
                        StopOrder = stop.StopOrder > 0 ? stop.StopOrder : (i + 1),
                        ArrivalTime = arrTime,
                        DepartureTime = depTime,
                        Status = "SCHEDULED"
                    });

                    // Tính thời gian dựa trên km thực tế của trạm tiếp theo nếu có
                    int travelMinutes = 20;
                    if (i + 1 < stops.Count)
                    {
                        double segmentKm = stops[i + 1].DistanceFromStartKm - stop.DistanceFromStartKm;
                        if (segmentKm > 0) travelMinutes = (int)Math.Max(5, segmentKm * 2);
                    }

                    curTime = depTime.Add(TimeSpan.FromMinutes(travelMinutes));
                }

                _context.TripSchedules.AddRange(list);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã lập lịch trình thành công!", tripId = newTrip.Id });
        }

        [HttpDelete("trip/{tripId}")]
        public async Task<IActionResult> DeleteTrip(int tripId)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null) return NotFound(new { message = "Không tìm thấy chuyến xe" });

            var schedules = await _context.TripSchedules.Where(s => s.TripId == tripId).ToListAsync();
            _context.TripSchedules.RemoveRange(schedules);
            _context.Trips.Remove(trip);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa chuyến xe và lịch trình thành công" });
        }
    }

    public class QuickScheduleRequest
    {
        public int RouteId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public string DepartureTime { get; set; } = "08:00";
        public double TotalDistanceKm { get; set; }
    }
}