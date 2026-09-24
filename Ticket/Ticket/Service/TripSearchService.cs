using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Data;
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
                .Where(t => t.IsActive && t.DepartureTime >= DateTime.Now)
                .AsQueryable();

            if (request.OriginStationId.HasValue && request.OriginStationId.Value > 0)
            {
                query = query.Where(t => t.OriginStationId == request.OriginStationId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.OriginKeyword))
            {
                string keyword = request.OriginKeyword.Trim().ToLower();
                
                query = query.Where(t => t.OriginStation.StationName.ToLower().Contains(keyword));
            }

            var result = await query
                .OrderBy(t => t.DepartureTime) 
                .Select(t => new TripResponseDto
                {
                    TripId = t.TripId,
                    RouteName = t.Route.RouteName,
                    OriginStationName = t.OriginStation.StationName,
                    DestinationStationName = t.DestinationStation.StationName,
                    DepartureTime = t.DepartureTime,
                    Price = t.Price,
                    AvailableSeats = t.TotalSeats - t.BookedSeats 
                })
                .ToListAsync();

            return result;
        }
    }
}