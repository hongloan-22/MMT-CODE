using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SeatController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET:
        // /api/Seat/trip/{tripId}
        // =====================================================

        [HttpGet("trip/{tripId:int}")]
        public async Task<IActionResult>
            GetSeatsByTrip(int tripId)
        {
            var trip =
                await _context.Trips
                    .AsNoTracking()
                    .Include(x => x.Bus)
                        .ThenInclude(x =>
                            x!.Seats)
                    .Include(x => x.Route)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                tripId &&
                            x.IsActive);

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy chuyến xe."
                });
            }

            if (trip.Bus == null)
            {
                return BadRequest(new
                {
                    message =
                        "Chuyến xe chưa được gắn xe."
                });
            }

            var now =
                DateTime.UtcNow;

            // =====================================================
            // GHẾ ĐANG GIỮ
            // =====================================================

            var heldSeatCodes =
                await _context.SeatHolds
                    .AsNoTracking()
                    .Where(x =>
                        x.TripId ==
                            tripId &&
                        x.Status == "HELD" &&
                        x.ExpiresAt >
                            now)
                    .Select(x =>
                        x.SeatCode)
                    .Distinct()
                    .ToListAsync();

            var heldSet =
                heldSeatCodes
                    .ToHashSet();

            // =====================================================
            // GHẾ ĐÃ XÁC NHẬN
            //
            // Hiện tại Payment chưa làm,
            // nhưng chuẩn bị sẵn cho bước sau.
            // =====================================================

            var confirmedSeatCodes =
                await _context.Bookings
                    .AsNoTracking()
                    .Where(x =>
                        x.TripId ==
                            tripId &&
                        x.Status == "CONFIRMED")
                    .SelectMany(x =>
                        x.SeatHolds)
                    .Where(x =>
                        x.Status == "CONFIRMED")
                    .Select(x =>
                        x.SeatCode)
                    .Distinct()
                    .ToListAsync();

            var confirmedSet =
                confirmedSeatCodes
                    .ToHashSet();

            var seats =
                trip.Bus.Seats
                    .Where(x =>
                        x.IsActive)
                    .OrderBy(x =>
                        x.SeatCode)
                    .Select(x =>
                        new
                        {
                            id =
                                x.Id,

                            seatCode =
                                x.SeatCode,

                            seatType =
                                x.SeatType,

                            isActive =
                                x.IsActive,

                            status =
                                confirmedSet.Contains(
                                    x.SeatCode)
                                    ? "booked"
                                    : heldSet.Contains(
                                        x.SeatCode)
                                        ? "held"
                                        : "available"
                        })
                    .ToList();

            return Ok(new
            {
                tripId =
                    trip.Id,

                tripCode =
                    trip.TripCode,

                tripDate =
                    trip.TripDate
                        .ToString("yyyy-MM-dd"),

                departureTime =
                    trip.DepartureTime
                        .ToString(@"hh\:mm"),

                arrivalTime =
                    trip.ArrivalTime.HasValue
                        ? trip.ArrivalTime.Value
                            .ToString(@"hh\:mm")
                        : null,

                routeCode =
                    trip.Route?.RouteCode ?? "",

                routeName =
                    trip.Route?.RouteName ?? "",

                startPoint =
                    trip.Route?.StartPoint ?? "",

                endPoint =
                    trip.Route?.EndPoint ?? "",

                busCode =
                    trip.Bus.BusCode,

                licensePlate =
                    trip.Bus.LicensePlate,

                seatCount =
                    trip.Bus.SeatCount,

                fare =
                    trip.Route?.Fare ?? 0,

                availableSeats =
                    seats.Count(x =>
                        x.status == "available"),

                seats
            });
        }
    }
}