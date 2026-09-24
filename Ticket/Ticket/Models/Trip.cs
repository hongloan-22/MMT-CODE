public class Trip
{
    public int TripId { get; set; }
    public int RouteId { get; set; }
    public Route? Route { get; set; }
    public int OriginStationId { get; set; }
    public Station? OriginStation { get; set; }
    public int DestinationStationId { get; set; }
    public Station? DestinationStation { get; set; }
    public DateTime DepartureTime { get; set; }
    public decimal Price { get; set; }
    public int TotalSeats { get; set; }
    public int BookedSeats { get; set; }
    public bool IsActive { get; set; }
}

// Models/Station.cs
public class Station
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
}

// Models/Route.cs
public class Route
{
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
}