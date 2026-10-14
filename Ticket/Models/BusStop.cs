using System.ComponentModel.DataAnnotations;

namespace Ticket.Models
{
    public class BusStop
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string StopCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        // true = hoạt động
        // false = ngừng hoạt động
        public bool IsActive { get; set; } = true;

        public ICollection<RouteStop> RouteStops { get; set; }
            = new List<RouteStop>();
    }
}