using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticket.Models;
using Ticket.Data;

namespace Ticket.Services
{
    public interface ITicketService
    {
        Task<bool> CancelTicketAsync(int ticketId, string userId, string reason);
    }

    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRefundService _refundPolicy;

        public TicketService(ApplicationDbContext context, IRefundService refundPolicy)
        {
            _context = context;
            _refundPolicy = refundPolicy;
        }

        public async Task<bool> CancelTicketAsync(int ticketId, string userId, string reason)
        {
            // ✅ FIX CS1503: Include thêm Trip để lấy TripDate + DepartureTime
            var ticket = await _context.Tickets
                .Include(t => t.Seat)
                .Include(t => t.Schedule).ThenInclude(s => s.Trip)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId);

            if (ticket == null || ticket.Status != Ticket.Models.TicketStatus.Booked)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = Ticket.Models.TicketStatus.Cancelled;
                ticket.CancelReason = reason;

                if (ticket.Seat != null)
                    ticket.Seat.Status = SeatStatus.Available;

                // ✅ FIX CS1503: Ghép TripDate + DepartureTime thành DateTime để gọi đúng overload
                var refundAmount = 0m;
                if (ticket.Schedule?.Trip != null)
                {
                    var departureDateTime = ticket.Schedule.Trip.TripDate.Date
                                            + ticket.Schedule.Trip.DepartureTime;

                    refundAmount = _refundPolicy.CalculateRefundAmount(ticket.Price, departureDateTime);
                }

                if (refundAmount > 0)
                {
                    _context.RefundTransactions.Add(new RefundTransaction
                    {
                        TicketId = ticket.Id,
                        Amount = refundAmount,
                        CreatedAt = DateTime.UtcNow,
                        Status = RefundStatus.Pending,
                        Reason = reason
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}