namespace SmartBusTicketing.DTOs
{
    public class SeatResponseDto
    {
        public int Id { get; set; }

        public string SeatCode { get; set; } = string.Empty;

        public string SeatType { get; set; } = "NORMAL";

        public string Status { get; set; } = "available";

        public bool IsActive { get; set; }
    }

    public class TripSeatMapResponseDto
    {
        public int TripId { get; set; }

        public string TripCode { get; set; } = string.Empty;

        public string BusCode { get; set; } = string.Empty;

        public string LicensePlate { get; set; } = string.Empty;

        public int SeatCount { get; set; }

        public decimal Fare { get; set; }

        public List<SeatResponseDto> Seats { get; set; }
            = new();
    }
}