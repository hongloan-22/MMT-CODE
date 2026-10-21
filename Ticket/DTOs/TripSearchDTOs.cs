namespace SmartBusTicketing.DTOs
{
    public class TripSearchRequestDto
    {
        public string? OriginKeyword { get; set; }

        public string? DestinationKeyword { get; set; }

        public DateTime? DepartureDate { get; set; }

        public TimeSpan? DepartureTime { get; set; }
    }

    public class TripResponseDto
    {
        public int TripId { get; set; }

        public int RouteId { get; set; }

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        public string OriginStopName { get; set; } = string.Empty;

        public string DestinationStopName { get; set; } = string.Empty;

        public DateTime DepartureDateTime { get; set; }

        public DateTime? ArrivalDateTime { get; set; }

        public decimal Fare { get; set; }

        public double DistanceKm { get; set; }

        public int DurationMinutes { get; set; }

        public int AvailableSeats { get; set; }

        public bool IsActive { get; set; }
    }
}