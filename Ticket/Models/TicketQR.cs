using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class TicketQR
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [ForeignKey(nameof(TicketId))]
        public Ticket? Ticket { get; set; }

        [Required]
        [StringLength(200)]
        public string QRToken { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}