namespace SmartBusTicketing.DTOs
{
    public class TicketResponseDto
    {
        public int TicketId { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string BookingCode { get; set; } = string.Empty;

        public string TripCode { get; set; } = string.Empty;

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        public string PassengerName { get; set; } = string.Empty;

        public string DepartureStop { get; set; } = string.Empty;

        public string ArrivalStop { get; set; } = string.Empty;

        public DateTime TripDate { get; set; }

        public string DepartureTime { get; set; } = string.Empty;

        public string ArrivalTime { get; set; } = string.Empty;

        public string SeatCode { get; set; } = string.Empty;

        public decimal Fare { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public string QRToken { get; set; } = string.Empty;
    }

    public class VerifyTicketQRRequestDto
    {
        public string QRToken { get; set; } = string.Empty;
        public int TripId { get; set; }
    }
    public class ChangeTicketRequestDto
    {
        public int NewTripId { get; set; }

        public string NewSeatCode { get; set; } = string.Empty;
    }
}