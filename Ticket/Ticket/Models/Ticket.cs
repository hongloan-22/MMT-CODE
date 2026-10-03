using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với Người dùng (User)
        public int UserId { get; set; }

        // Khóa ngoại liên kết với Lịch trình chuyến xe
        public int ScheduleId { get; set; }
        
        [ForeignKey("ScheduleId")]
        public TripSchedule Schedule { get; set; } = null!;

        // Khóa ngoại liên kết với Ghế ngồi
        public int SeatId { get; set; }
        
        [ForeignKey("SeatId")]
        public Seat Seat { get; set; } = null!;

        // Giá vé tại thời điểm đặt
        [Column(TypeName = "decimal(18,2)]")]
        public decimal Price { get; set; }

        // Trạng thái vé (Pending, Booked, Cancelled, Completed...)
        public TicketStatus Status { get; set; } = TicketStatus.Booked;

        // Thời gian đặt vé
        public DateTime BookingDate { get; set; } = DateTime.Now;

        // Lý do hủy vé (phục vụ cho US-77, US-78)
        public string? CancelReason { get; set; }

        // Quan hệ 1-1 hoặc 1-n với giao dịch hoàn tiền (nếu có yêu cầu hủy)
        public RefundTransaction? RefundTransaction { get; set; }
    }

    // Định nghĩa Enum trạng thái vé nếu chưa có
    public enum TicketStatus
    {
        Pending,   // Đang chờ thanh toán
        Booked,    // Đã đặt thành công
        Cancelled, // Đã hủy
        Completed  // Đã hoàn thành chuyến đi
    }
}