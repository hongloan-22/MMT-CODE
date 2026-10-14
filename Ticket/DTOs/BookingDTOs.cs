namespace SmartBusTicketing.DTOs
{
    public class HoldSeatRequestDto
    {
        public int TripId { get; set; }

        public List<string> SeatCodes { get; set; }
            = new();
    }

    public class HoldSeatResponseDto
    {
        public int BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public int TripId { get; set; }

        public string TripCode { get; set; } = string.Empty;

        public List<string> SeatCodes { get; set; }
            = new();

        public decimal TotalAmount { get; set; }

        public DateTime HeldAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public int RemainingSeconds { get; set; }

        public string Status { get; set; } = "HELD";
    }
}