using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        [StringLength(30)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}