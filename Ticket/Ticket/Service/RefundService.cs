using System;

namespace Ticket.Services
{
    public interface IRefundService
    {
        decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime);
        decimal CalculateRefundAmount(decimal price, TimeSpan departureTime);
        (decimal refundFromOldTicket, decimal netDifference) CalculateExchange(decimal oldPrice, DateTime departureTime, decimal newPrice);
    }

    public class RefundService : IRefundService
    {
        public (decimal refundFromOldTicket, decimal netDifference) CalculateExchange(decimal oldPrice, DateTime departureTime, decimal newPrice)
        {
            decimal refundFromOld = CalculateRefundAmount(oldPrice, departureTime);
            decimal netDifference = newPrice - refundFromOld;
            return (refundFromOld, netDifference);
        }

        public decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime)
        {
            var hourToDeparture = (departureTime - DateTime.Now).TotalHours;
            if (hourToDeparture >= 24) return ticketPrice;
            if (hourToDeparture >= 12) return ticketPrice * 0.5m;
            return 0m;
        }

        public decimal CalculateRefundAmount(decimal price, TimeSpan departureTime)
        {
            var hoursToDeparture = (DateTime.Today.Add(departureTime) - DateTime.Now).TotalHours;
            if (hoursToDeparture >= 24) return price;
            if (hoursToDeparture >= 12) return price * 0.5m;
            return 0m;
        }

        internal async Task<object?> CancelTicketAsync(object ticketId, string userId, object reason)
        {
            throw new NotImplementedException();
        }
    }
}