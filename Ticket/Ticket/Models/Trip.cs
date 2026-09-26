using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Trip
    {
        [Key]
        public int Id { get; set; }

        public int RouteId { get; set; }

        [ForeignKey(nameof(RouteId))]
        public BusRoute? Route { get; set; }

        [Required]
        [StringLength(50)]
        public string TripCode { get; set; } = string.Empty;

        public DateTime TripDate { get; set; }

        public TimeSpan DepartureTime { get; set; }

        public TimeSpan? ArrivalTime { get; set; }

        public string Status { get; set; } = "SCHEDULED";

        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public string? Note { get; set; }


        // Các lịch trình của chuyến
        public ICollection<TripSchedule> Schedules { get; set; }
            = new List<TripSchedule>();
    }
}