using System;

namespace Ticket.DTOs
{
    public class ScheduleSearchRequestDto
    {
        public int? RouteId { get; set; }
        public DateTime? DepartureDate { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
    }

    public class ScheduleResponseDto
    {
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal TicketPrice { get; set; }
        public int AvailableSeats { get; set; }
        public string BusLicensePlate { get; set; } = string.Empty;
    }
}