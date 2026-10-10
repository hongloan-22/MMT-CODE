using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        // ✅ FIX: UserId là string (khớp User.UserId GUID)
        [MaxLength(36)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Khóa ngoại liên kết với Lịch trình chuyến xe
        public int ScheduleId { get; set; }

        [ForeignKey("ScheduleId")]
        public TripSchedule Schedule { get; set; } = null!;

        // Khóa ngoại liên kết với Ghế ngồi
        public int SeatId { get; set; }

        [ForeignKey("SeatId")]
        public Seat Seat { get; set; } = null!;

        // ✅ FIX: sửa typo `decimal(18,2)]` → `decimal(18,0)`
        [Column(TypeName = "decimal(18,0)")]
        public decimal Price { get; set; }

        // Trạng thái vé (Pending, Booked, Cancelled, Completed...)
        public TicketStatus Status { get; set; } = TicketStatus.Booked;

        // Thời gian đặt vé
        public DateTime BookingDate { get; set; } = DateTime.Now;

        // Lý do hủy vé (US-77, US-78)
        public string? CancelReason { get; set; }

        // Quan hệ 1-1 với giao dịch hoàn tiền
        public RefundTransaction? RefundTransaction { get; set; }
    }

    public enum TicketStatus
    {
        Pending,
        Booked,
        Cancelled,
        Completed
    }
}