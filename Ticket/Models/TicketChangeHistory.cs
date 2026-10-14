using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class TicketChangeHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [ForeignKey(nameof(TicketId))]
        public Ticket? Ticket { get; set; }

        [Required]
        public int OldTripId { get; set; }

        [Required]
        public int NewTripId { get; set; }

        [Required]
        [StringLength(10)]
        public string OldSeatCode { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string NewSeatCode { get; set; } = string.Empty;
        public decimal OldFare { get; set; }
        public decimal NewFare { get; set; }
        public decimal Difference { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "COMPLETED";

        [StringLength(250)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}