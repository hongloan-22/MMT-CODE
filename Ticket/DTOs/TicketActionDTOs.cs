namespace Ticket.DTOs
{
    public class CancelTicketDto
    {
        public int TicketId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ExchangeTicketDto
    {
        public int OldTicketId { get; set; }
        public int NewTripId { get; set; }
        public string NewSeatNumber { get; set; } = string.Empty;
    }

    public class TicketActionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public decimal AdditionalPaymentAmount { get; set; }
        public string? PaymentUrl { get; set; }
        public string? NewTicketCode { get; set; }
    }
}