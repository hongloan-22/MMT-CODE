using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticket.Models; 
using Ticket.Data;   

namespace Ticket.Services
{
    public interface ITicketService
    {
        // MỚI
        Task<bool> CancelTicketAsync(int ticketId, string userId, string reason);
        Task<bool> ExchangeTicketAsync(int oldTicketId, int newScheduleId, string newSeatId, int userId);
    }

    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRefundService _refundService;

        public TicketService(ApplicationDbContext context, IRefundService refundService)
        {
            _context = context;
            _refundService = refundService;
        }

        // ✅ MỚI
        public async Task<bool> CancelTicketAsync(int ticketId, string userId, string reason)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Seat)
                .Include(t => t.Schedule)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId);

            if (ticket == null || ticket.Status != Ticket.Models.TicketStatus.Booked) return false;
            if (ticket.Schedule == null)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = Ticket.Models.TicketStatus.Cancelled;
                ticket.CancelReason = reason;

                if (ticket.Seat != null) ticket.Seat.Status = SeatStatus.Available;

                var refundAmount = _refundService.CalculateRefundAmount(ticket.Price, ticket.Schedule.DepartureTime);
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

        public Task<bool> ExchangeTicketAsync(int oldTicketId, int newScheduleId, string newSeatId, int userId)
        {
            throw new NotImplementedException();
        }
    }
}