using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(36)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingCode { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "HELD";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<SeatHold> SeatHolds { get; set; }
            = new List<SeatHold>();
    }
}