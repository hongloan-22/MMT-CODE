using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;
using SmartBusTicketing.DTOs;

namespace SmartBusTicketing.Services
{
    public class TripServices : ITripService
    {
        private readonly ApplicationDbContext _context;

        public TripServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TripResponseDto>> SearchTripsByOriginAsync(TripSearchRequestDto request)
        {
            var query = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.OriginStation)
                .Include(t => t.DestinationStation)
                .Where(t => t.IsActive)
                .AsQueryable();

            if (request.OriginStationId.HasValue && request.OriginStationId.Value > 0)
            {
                query = query.Where(t => t.OriginStationId == request.OriginStationId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.OriginKeyword))
            {
                string keyword = request.OriginKeyword.Trim().ToLower();
                query = query.Where(t => t.OriginStation != null && t.OriginStation.StationName.ToLower().Contains(keyword));
            }

            var trips = await query
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();

            var result = trips.Select(t => new TripResponseDto
            {
                TripId = t.TripId,
                RouteName = t.Route != null ? t.Route.RouteName : string.Empty,
                OriginStationName = t.OriginStation != null ? t.OriginStation.StationName : "Điểm đi mặc định",
                DestinationStationName = t.DestinationStation != null ? t.DestinationStation.StationName : "Điểm đến mặc định",
                DepartureTime = t.TripDate.Date + t.DepartureTime,
                Price = t.Price,
                AvailableSeats = t.TotalSeats - t.BookedSeats
            }).ToList();

            return result;
        }
    }
}