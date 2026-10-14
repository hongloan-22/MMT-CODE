namespace Ticket.Models
{
        // DTO trả về sơ đồ ghế của chuyến
        public class SeatMapResponseDto
        {
            public string TripId { get; set; } = string.Empty;
            public int TotalSeats { get; set; }
            public int AvailableSeats { get; set; }
            public List<Seat> Seats { get; set; } = new();
        }

        // DTO gửi yêu cầu giữ chỗ
        public class CreateHoldRequestDto
        {
            public string TripId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public List<string> SeatIds { get; set; } = new();
            public int HoldDurationMinutes { get; set; } = 10; // Mặc định giữ chỗ 10 phút
        }

        // DTO phản hồi kết quả giữ chỗ
        public class HoldResponseDto
        {
            public string HoldId { get; set; } = string.Empty;
            public string TripId { get; set; } = string.Empty;
            public List<string> SeatIds { get; set; } = new();
            public DateTime ExpiresAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
 }