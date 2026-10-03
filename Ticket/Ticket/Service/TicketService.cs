using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticket.Models; // Thay đổi đường dẫn đến các class Ticket, RefundTransaction...
using Ticket.Data;   // Thay đổi đường dẫn đến AppDbContext

namespace Ticket.Services
{
    public interface ITicketService
    {
        Task<bool> CancelTicketAsync(int ticketId, int userId, string reason);
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

        public async Task<bool> CancelTicketAsync(int ticketId, int userId, string reason)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Seat)
                .Include(t => t.Schedule)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId);

            if (ticket == null || ticket.Status != Ticket.Models.TicketStatus.Booked) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = Ticket.Models.TicketStatus.Cancelled;
                ticket.CancelReason = reason;

                if (ticket.Seat != null) ticket.Seat.Status = SeatStatus.Available;

                var refundAmount = _refundPolicy.CalculateRefundAmount(ticket.Price, ticket.Schedule.DepartureTime);
                if (refundAmount > 0)
                {
                    _context.RefundTransactions.Add(new RefundTransaction
                    {
                        TicketId = ticket.Id,
                        Amount = refundAmount,
                        CreatedAt = DateTime.Now,
                        Status = RefundStatus.Pending
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