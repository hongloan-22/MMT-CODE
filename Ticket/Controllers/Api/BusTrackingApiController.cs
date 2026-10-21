using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers.Api;

[ApiController]
[Route("api/tracking")]
public class BusTrackingApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    public BusTrackingApiController(ApplicationDbContext db, IConfiguration configuration)
    { _db = db; _configuration = configuration; }

    public sealed record GpsUpdate(int TripId, double Latitude, double Longitude, double? SpeedKmh);

    // Bảo vệ API gửi GPS bằng khóa bí mật cấu hình phía máy chủ.
    // Không đặt khóa trong JavaScript hoặc mã nguồn công khai.
    [HttpPost("position")]
    public async Task<IActionResult> UpdatePosition([FromBody] GpsUpdate input)
    {
        var configuredKey = _configuration["Tracking:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            return StatusCode(503, new { message = "Chưa cấu hình Tracking:ApiKey trên máy chủ." });
        if (!Request.Headers.TryGetValue("X-Tracking-Key", out var suppliedKey) ||
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(suppliedKey.ToString()),
                System.Text.Encoding.UTF8.GetBytes(configuredKey)))
            return Unauthorized(new { message = "Khóa gửi GPS không hợp lệ." });

        if (input.TripId <= 0 || input.Latitude is < -90 or > 90 || input.Longitude is < -180 or > 180 ||
            (input.SpeedKmh.HasValue && (input.SpeedKmh < 0 || input.SpeedKmh > 250)))
            return BadRequest(new { message = "Dữ liệu GPS không hợp lệ." });

        var trip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == input.TripId && t.IsActive);
        if (trip == null) return NotFound(new { message = "Không tìm thấy chuyến xe đang hoạt động." });

        var now = DateTime.UtcNow;
        var latest = await _db.BusGpsPositions.Where(x => x.TripId == input.TripId)
            .OrderByDescending(x => x.RecordedAtUtc).FirstOrDefaultAsync();
        if (latest == null)
        {
            latest = new BusGpsPosition { TripId = input.TripId };
            _db.BusGpsPositions.Add(latest);
        }
        latest.Latitude = input.Latitude;
        latest.Longitude = input.Longitude;
        latest.SpeedKmh = input.SpeedKmh;
        latest.RecordedAtUtc = now;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, tripId = input.TripId, latitude = latest.Latitude,
            longitude = latest.Longitude, speedKmh = latest.SpeedKmh, recordedAtUtc = now });
    }

    [HttpGet("trips/{tripId:int}/position")]
    public async Task<IActionResult> GetPosition(int tripId)
    {
        var result = await _db.BusGpsPositions.AsNoTracking().Where(x => x.TripId == tripId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new { x.TripId, x.Latitude, x.Longitude, x.SpeedKmh, x.RecordedAtUtc })
            .FirstOrDefaultAsync();
        return result == null ? NotFound(new { message = "Chưa có dữ liệu GPS cho chuyến này." }) : Ok(result);
    }

    [HttpGet("trips/{tripId:int}/route")]
    public async Task<IActionResult> GetTripRoute(int tripId)
    {
        var trip = await _db.Trips.AsNoTracking()
            .Include(t => t.Schedules).ThenInclude(s => s.BusStop)
            .FirstOrDefaultAsync(t => t.Id == tripId && t.IsActive);
        if (trip == null) return NotFound(new { message = "Không tìm thấy chuyến xe." });
        var stops = trip.Schedules.Where(s => s.BusStop != null &&
                s.BusStop.Latitude.HasValue && s.BusStop.Longitude.HasValue)
            .OrderBy(s => s.StopOrder)
            .Select(s => new { stopId = s.StopId, stopOrder = s.StopOrder,
                name = s.BusStop!.Name, address = s.BusStop.Address,
                latitude = s.BusStop.Latitude, longitude = s.BusStop.Longitude,
                arrivalTime = s.ArrivalTime.ToString(), departureTime = s.DepartureTime.ToString() })
            .ToList();
        return Ok(new { tripId = trip.Id, tripCode = trip.TripCode, stops });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBusPositions()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var recentPositions = await _db.BusGpsPositions.AsNoTracking()
            .Where(p => p.RecordedAtUtc >= cutoff)
            .OrderByDescending(p => p.RecordedAtUtc)
            .ToListAsync();
        var latest = recentPositions.GroupBy(p => p.TripId).Select(g => g.First()).ToList();
        var tripIds = latest.Select(p => p.TripId).ToList();
        var trips = await _db.Trips.AsNoTracking()
            .Where(t => t.IsActive && tripIds.Contains(t.Id))
            .Select(t => new { t.Id, t.TripCode, t.RouteId }).ToListAsync();
        var tripLookup = trips.ToDictionary(t => t.Id);
        var result = latest.Where(p => tripLookup.ContainsKey(p.TripId)).Select(p => new {
            p.TripId, tripLookup[p.TripId].TripCode, tripLookup[p.TripId].RouteId,
            p.Latitude, p.Longitude, p.SpeedKmh, p.RecordedAtUtc
        }).ToList();
        return Ok(result);
    }
}
