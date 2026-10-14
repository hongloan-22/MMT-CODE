namespace SmartBusTicketing.DTOs
{
    public class TripSearchRequestDto
    {
        public int? OriginStationId { get; set; } 
        
        public string? OriginKeyword { get; set; } 
    }
}

namespace SmartBusTicketing.DTOs
{
    public class TripResponseDto
    {
        public int TripId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string OriginStationName { get; set; } = string.Empty;
        public string DestinationStationName { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
    }
}