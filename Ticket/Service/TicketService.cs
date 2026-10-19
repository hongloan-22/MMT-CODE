using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Service
{
    public class TicketService
    {
        private readonly ApplicationDbContext _context;

        public TicketService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> CancelTicketAsync(int ticketId, string userId, string reason)
        {
            return Task.FromResult(true);
        }
    }
}