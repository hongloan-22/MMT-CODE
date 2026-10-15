using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class BusRoute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string RouteCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string RouteName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string StartPoint { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string EndPoint { get; set; } = string.Empty;
        public decimal Fare { get; set; }

        public double TotalDistanceKm { get; set; }

        public TimeSpan EstimatedDuration { get; set; }

        // true = hoạt động
        // false = ngừng hoạt động
        public bool IsActive { get; set; } = true;

        public ICollection<RouteStop> RouteStops { get; set; }
            = new List<RouteStop>();

        public ICollection<Trip> Trips { get; set; }
            = new List<Trip>();
    }
}