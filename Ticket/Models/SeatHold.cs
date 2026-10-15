using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class SeatHold
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        [Required]
        [StringLength(10)]
        public string SeatCode { get; set; } = string.Empty;

        [Required]
        public DateTime HeldAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "HELD";
    }
}