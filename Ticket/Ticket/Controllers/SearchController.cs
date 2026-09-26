using Microsoft.AspNetCore.Mvc;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        // Constructor rỗng để chạy Mock Data không cần Database
        public SearchController()
        {
        }

        /// <summary>
        /// API US-32: Tra cứu tuyến xe và trạm dừng dựa trên điểm đến
        /// </summary>
        [HttpGet("by-destination")]
        public async Task<IActionResult> SearchByDestination([FromQuery] SearchByDestinationDto request)
        {
            // ==========================================
            // PHẦN 1: MOCK DATA (Chạy test API thông luồng ngay)
            // ==========================================
            var mockResponse = new[]
            {
                new
                {
                    RouteId = request.RouteId ?? 1,
                    RouteCode = "R01",
                    RouteName = "Tuyến 01: Bến Thành - Chợ Lớn",
                    DestinationStop = new
                    {
                        StopId = request.DestinationStopId ?? 2,
                        StopName = string.IsNullOrEmpty(request.DestinationName)
                            ? "Trạm Chợ Lớn"
                            : request.DestinationName,
                        Address = "Quận 5, TP.HCM",
                        StopOrder = 12,
                        DistanceFromStartKm = 8.5,
                        TravelTimeFromStartMinutes = 45
                    }
                }
            };

            return Ok(new
            {
                Success = true,
                TotalResults = mockResponse.Length,
                Data = mockResponse
            });

            // ==========================================
            // PHẦN 2: CODE DATABASE THẬT (Mở comment khi DB sẵn sàng)
            // ==========================================
            /*
            var query = _context.Set<RouteStop>()
                .Include(rs => rs.BusStop)
                .Include(rs => rs.Route)
                .AsQueryable();

            if (request.RouteId.HasValue)
            {
                query = query.Where(rs => rs.RouteId == request.RouteId.Value);
            }

            if (request.DestinationStopId.HasValue)
            {
                query = query.Where(rs => rs.StopId == request.DestinationStopId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.DestinationName))
            {
                query = query.Where(rs => rs.BusStop.Name.Contains(request.DestinationName));
            }

            var routeStops = await query
                .OrderBy(rs => rs.RouteId)
                .ThenBy(rs => rs.StopOrder)
                .ToListAsync();

            var response = routeStops.Select(rs => new
            {
                rs.RouteId,
                RouteCode = rs.Route?.RouteCode,
                RouteName = rs.Route?.RouteName,
                DestinationStop = new
                {
                    StopId = rs.StopId,
                    StopName = rs.BusStop?.Name,
                    Address = rs.BusStop?.Address,
                    rs.StopOrder,
                    rs.DistanceFromStartKm,
                    rs.TravelTimeFromStartMinutes
                }
            });

            return Ok(new
            {
                Success = true,
                TotalResults = response.Count(),
                Data = response
            });
            */
        }
    }
}