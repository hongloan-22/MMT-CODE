using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class BusSeat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusId { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }

        [Required]
        [StringLength(10)]
        public string SeatCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string SeatType { get; set; } = "NORMAL";

        public bool IsActive { get; set; } = true;
    }
}