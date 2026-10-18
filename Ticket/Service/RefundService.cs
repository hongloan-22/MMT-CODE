using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Service
{
    public class RefundService
    {
        private readonly ApplicationDbContext _context;

        public RefundService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> CancelTicketAsync(int ticketId, string userId, string reason)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Booking)
                    .Include(t => t.Trip)
                    .FirstOrDefaultAsync(t => t.Id == ticketId && t.Booking!.UserId == userId);

                if (ticket == null || ticket.Status != "VALID")
                    return new { Success = false, Message = "Vé không hợp lệ hoặc không thể hủy." };

                var departureAt = ticket.Trip!.TripDate.Date.Add(ticket.Trip.DepartureTime);
                if (DateTime.Now >= departureAt.AddMinutes(-30))
                    return new { Success = false, Message = "Không thể hủy vé trước giờ khởi hành dưới 30 phút." };

                // Hủy vé
                ticket.Status = "CANCELLED";
                ticket.Trip.BookedSeats = Math.Max(0, ticket.Trip.BookedSeats - 1);
                ticket.Trip.AvailableSeats = Math.Min(ticket.Trip.TotalSeats, ticket.Trip.AvailableSeats + 1);
                
                if (ticket.Booking != null)
                {
                    ticket.Booking.Status = "CANCELLED";
                }

                // Giải phóng ghế
                var seatHold = await _context.SeatHolds
                    .FirstOrDefaultAsync(x => x.TripId == ticket.TripId && x.SeatCode == ticket.SeatCode && x.Status == "CONFIRMED");
                
                if (seatHold != null) seatHold.Status = "RELEASED";

                // Tạo Refund (nếu đã thanh toán)
                var payment = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(x => x.BookingId == ticket.BookingId && x.Status == "SUCCESS");

                if (payment != null && ticket.Fare > 0)
                {
                    _context.RefundTransactions.Add(new RefundTransaction
                    {
                        TicketId = ticket.Id,
                        PaymentTransactionId = payment.Id,
                        Amount = ticket.Fare,
                        Status = "PENDING",
                        Reason = "CANCEL_TICKET",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new { Success = true, Message = "Hủy vé thành công." };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}