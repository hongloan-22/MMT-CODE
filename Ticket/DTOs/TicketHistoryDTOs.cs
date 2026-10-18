
namespace SmartBusTicketing.DTOs
{
    public class TicketHistoryItemDto
    {
        public int TicketId { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string BookingCode { get; set; } = string.Empty;

        public string BookingStatus { get; set; } = string.Empty;

        public string PassengerName { get; set; } = string.Empty;

        public string TripCode { get; set; } = string.Empty;

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        public DateTime? TripDate { get; set; }

        public string DepartureTime { get; set; } = string.Empty;

        public string ArrivalTime { get; set; } = string.Empty;

        public string DepartureStop { get; set; } = string.Empty;

        public string ArrivalStop { get; set; } = string.Empty;

        public string SeatCode { get; set; } = string.Empty;

        public decimal Fare { get; set; }

        public string TicketStatus { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public string PaymentStatus { get; set; } = "NO_TRANSACTION";

        public string PaymentProvider { get; set; } = string.Empty;

        public string PaymentTransactionCode { get; set; } = string.Empty;

        public decimal? PaymentAmount { get; set; }

        public DateTime? PaidAt { get; set; }

        public string RefundStatus { get; set; } = "NO_TRANSACTION";

        public string RefundReason { get; set; } = string.Empty;

        public decimal? RefundAmount { get; set; }

        public DateTime? RefundedAt { get; set; }
    }

    public class TicketHistoryResponseDto
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public List<TicketHistoryItemDto> Items { get; set; }
            = new List<TicketHistoryItemDto>();
    }
}
