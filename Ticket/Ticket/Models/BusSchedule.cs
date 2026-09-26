using Smart_Bus_Ticketing_System.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class BusSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusRouteId { get; set; }

        [ForeignKey("BusRouteId")]
        public BusRoute? BusRoute { get; set; }

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        public decimal TicketPrice { get; set; }

        public int AvailableSeats { get; set; } = 40;

        public string BusLicensePlate { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}