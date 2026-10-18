using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Service
{
    public class ExchangeService
    {
        private readonly ApplicationDbContext _context;

        public ExchangeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExchangeTicketAsync(int ticketId, int newTripId, string newSeatCode, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var oldTicket = await _context.Tickets
                    .Include(t => t.Booking)
                    .Include(t => t.Trip)
                    .FirstOrDefaultAsync(t => t.Id == ticketId && t.Booking!.UserId == userId);

                if (oldTicket == null || oldTicket.Status != "VALID")
                    return false;

                var oldDepartureAt = oldTicket.Trip!.TripDate.Date.Add(oldTicket.Trip.DepartureTime);
                if (DateTime.Now >= oldDepartureAt.AddHours(-4))
                    return false;

                var newTrip = await _context.Trips.Include(t => t.Route).FirstOrDefaultAsync(t => t.Id == newTripId);
                if (newTrip == null || newTrip.Status != "ACTIVE" || newTrip.AvailableSeats <= 0)
                    return false;

                // Kiểm tra ghế trống
                var isSeatTaken = await _context.SeatHolds.AnyAsync(x => x.TripId == newTripId && x.SeatCode == newSeatCode && x.Status == "CONFIRMED");
                if (isSeatTaken) return false;

                // Hoàn ghế cũ
                oldTicket.Trip.BookedSeats = Math.Max(0, oldTicket.Trip.BookedSeats - 1);
                oldTicket.Trip.AvailableSeats = Math.Min(oldTicket.Trip.TotalSeats, oldTicket.Trip.AvailableSeats + 1);

                var oldHold = await _context.SeatHolds.FirstOrDefaultAsync(x => x.TripId == oldTicket.TripId && x.SeatCode == oldTicket.SeatCode && x.Status == "CONFIRMED");
                if (oldHold != null) oldHold.Status = "RELEASED";

                // Cập nhật vé mới
                oldTicket.TripId = newTripId;
                oldTicket.RouteId = newTrip.RouteId;
                oldTicket.SeatCode = newSeatCode;
                oldTicket.Fare = newTrip.Route!.Fare;

                // Tạo ghế mới
                _context.SeatHolds.Add(new SeatHold
                {
                    BookingId = oldTicket.BookingId,
                    TripId = newTripId,
                    SeatCode = newSeatCode,
                    Status = "CONFIRMED",
                    HeldAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1)
                });

                newTrip.BookedSeats++;
                newTrip.AvailableSeats = Math.Max(0, newTrip.TotalSeats - newTrip.BookedSeats);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}