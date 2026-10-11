using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(36)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int ScheduleId { get; set; }

        [ForeignKey(nameof(ScheduleId))]
        public TripSchedule? Schedule { get; set; }

        public int SeatId { get; set; }

        [ForeignKey(nameof(SeatId))]
        public Seat? Seat { get; set; }

        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal Price { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Booked;
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public string SeatNumber { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string TicketCode { get; set; } = string.Empty;
        public string? CancelReason { get; set; }

        public RefundTransaction? RefundTransaction { get; set; }
    }

    public enum TicketStatus
    {
        Pending,
        Booked,
        Cancelled,
        Completed,
        Paid
    }
}