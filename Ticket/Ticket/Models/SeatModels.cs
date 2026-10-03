namespace Ticket.Models
{
    // Trạng thái ghế
    public enum SeatStatus
    {
        Available = 0, // Ghế trống
        Held = 1,      // Đang được giữ chỗ tạm thời
        Booked = 2     // Đã thanh toán / Đã đặt thành công
    }

    // Thông tin từng ghế trên sơ đồ
    public class Seat
    {
        public string SeatId { get; set; } = string.Empty; // VD: "A1", "A2"
        public string SeatNumber { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
        public decimal Price { get; set; }
        public SeatStatus Status { get; set; }
    }

    // Thông tin lượt giữ chỗ tạm thời
    public class SeatHold
    {
        public string HoldId { get; set; } = Guid.NewGuid().ToString("N");
        public string TripId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<string> SeatIds { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsReleased { get; set; } = false;
        public bool IsConfirmed { get; set; } = false;

        // Tự động kiểm tra hết hạn giữ chỗ (ví dụ quá 10 phút)
        public bool IsExpired => DateTime.UtcNow > ExpiresAt && !IsConfirmed;
    }
}