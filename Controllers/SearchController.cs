using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API US-33: Tra cứu trạm dừng và tuyến xe dựa trên thời gian di chuyển
        /// </summary>
        [HttpGet("by-time")]
        public async Task<IActionResult> SearchByTime([FromQuery] SearchByTimeDto request)
        {
            // 1. Chỉ query RouteStops và BusStop để tránh phụ thuộc vào bảng Route cũ
            var query = _context.RouteStops
                .Include(rs => rs.BusStop)
                .AsQueryable();

            if (request.RouteId.HasValue)
            {
                query = query.Where(rs => rs.RouteId == request.RouteId.Value);
            }

            if (request.MinTravelTimeMinutes.HasValue)
            {
                query = query.Where(rs => rs.TravelTimeFromStartMinutes >= request.MinTravelTimeMinutes.Value);
            }

            if (request.MaxTravelTimeMinutes.HasValue)
            {
                query = query.Where(rs => rs.TravelTimeFromStartMinutes <= request.MaxTravelTimeMinutes.Value);
            }

            var routeStops = await query
                .OrderBy(rs => rs.RouteId)
                .ThenBy(rs => rs.StopOrder)
                .ToListAsync();

            TimeSpan baseStartTime = TimeSpan.Zero;
            bool hasValidStartTime = !string.IsNullOrEmpty(request.StartTime)
                                     && TimeSpan.TryParse(request.StartTime, out baseStartTime);

            // 2. Map trực tiếp với danh sách BusRoutes chuẩn của hệ thống
            var routeIds = routeStops.Select(rs => rs.RouteId).Distinct().ToList();
            var busRoutes = await _context.BusRoutes
                .Where(r => routeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RouteName);

            var response = routeStops.Select(rs => new
            {
                rs.Id,
                rs.RouteId,
                RouteName = busRoutes.ContainsKey(rs.RouteId) ? busRoutes[rs.RouteId] : $"Tuyến {rs.RouteId}",
                rs.StopId,
                BusStopInfo = rs.BusStop,
                rs.StopOrder,
                rs.DistanceFromStartKm,
                rs.TravelTimeFromStartMinutes,
                EstimatedArrivalTime = hasValidStartTime
                    ? baseStartTime.Add(TimeSpan.FromMinutes(rs.TravelTimeFromStartMinutes)).ToString(@"hh\:mm")
                    : null
            });

            return Ok(new
            {
                Success = true,
                TotalResults = response.Count(),
                Data = response
            });
        }
    }
}