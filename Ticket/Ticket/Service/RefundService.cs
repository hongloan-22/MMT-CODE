using System;

namespace Ticket.Services
{
    public interface IRefundService
    {
        decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime);
        int CalculateRefundAmount(decimal price, TimeSpan departureTime);
    }

    public class RefundService : IRefundService
    {
        public decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime)
        {
            var timeUntilDeparture = departureTime - DateTime.Now;
            if (timeUntilDeparture.TotalHours >= 24) return ticketPrice;
            if (timeUntilDeparture.TotalHours >= 12) return ticketPrice * 0.5m;
            return 0; 
        }

        public int CalculateRefundAmount(decimal price, TimeSpan departureTime)
        {
            throw new NotImplementedException();
        }
    }
}