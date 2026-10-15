using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class RefundTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [ForeignKey(nameof(TicketId))]
        public Ticket? Ticket { get; set; }

        public int? PaymentTransactionId { get; set; }

        [ForeignKey(nameof(PaymentTransactionId))]
        public PaymentTransaction? PaymentTransaction { get; set; }
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        [Required]
        [StringLength(30)]
        public string Reason { get; set; } = "CANCEL_TICKET";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}