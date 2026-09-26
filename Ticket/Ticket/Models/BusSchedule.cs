using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Ticket.Models
{
    public class BusSchedule
    {
        [Key]
        public int Id { get; set; }

        public int BusRouteId { get; set; }

        public DateTime DepartureTime { get; set; }

        public DateTime ArrivalTime { get; set; }

        public int AvailableSeats { get; set; } = 40;

        public bool IsActive { get; set; } = true;

        [ForeignKey("BusRouteId")]
        public virtual BusRoute? BusRoute { get; set; }
    }
}
