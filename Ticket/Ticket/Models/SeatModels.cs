using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    // Trạng thái ghế
    public enum SeatStatus
    {
        Available = 0, // Ghế trống
        Held = 1,      // Đang được giữ chỗ tạm thời
        Booked = 2     // Đã thanh toán / Đã đặt thành công
    }

    // 1. Model Xe (US-52)
    public class Bus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty; // VD: 29B-888.88

        [Required]
        [StringLength(50)]
        public string BusType { get; set; } = "Ghế ngồi"; // Ghế ngồi, Giường nằm

        public int TotalSeats { get; set; } = 40;

        public bool IsActive { get; set; } = true;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }

    // 2. Model Ghế vật lý trên xe (US-52)
    public class Seat
    {
        [Key]
        public int Id { get; set; }

        // Mã định danh hiển thị (khớp với code bạn kia: "A1", "A2", "B1"...)
        [Required]
        [StringLength(10)]
        public string SeatNumber { get; set; } = string.Empty;

        // Cho phép truy cập SeatId giống như bạn kia đang dùng
        [NotMapped]
        public string SeatId
        {
            get => SeatNumber;
            set => SeatNumber = value;
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public decimal Price { get; set; } = 0;

        public int BusId { get; set; }
        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }

        // Trạng thái mặc định của ghế vật lý
        public SeatStatus Status { get; set; } = SeatStatus.Available;
    }

    // 3. Model Phiên giữ chỗ (US-58)
    // 3. Model Phiên giữ chỗ (US-58)
    public class SeatHold
    {
        [Key]
        public string HoldId { get; set; } = Guid.NewGuid().ToString("N");

        // Giữ kiểu string để khớp 100% với code dịch vụ SeatBookingService ("TRIP01", "TRIP02"...)
        [Required]
        [StringLength(50)]
        public string TripId { get; set; } = string.Empty;

        public string? UserId { get; set; }

        // Lưu danh sách mã ghế dạng text nối chuỗi phẩy vào Database (VD: "A1,A2")
        public string SeatCodesRaw { get; set; } = string.Empty;

        // Cho phép truy cập List<string> mượt mà
        [NotMapped]
        public List<string> SeatIds
        {
            get => string.IsNullOrEmpty(SeatCodesRaw)
                ? new List<string>()
                : SeatCodesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            set => SeatCodesRaw = string.Join(",", value);
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public bool IsReleased { get; set; } = false;
        public bool IsConfirmed { get; set; } = false;

        // Tự động kiểm tra hết hạn giữ chỗ (10 phút)
        public bool IsExpired => DateTime.UtcNow > ExpiresAt && !IsConfirmed;
    }
}