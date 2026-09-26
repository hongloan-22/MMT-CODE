using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ticket.Models;

namespace Ticket.Models
{
    public class TripSchedule
    {
        [Key]
        public int Id { get; set; }

        // Chuyến xe
        [Required]
        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        // Trạm
        [Required]
        public int StopId { get; set; }

        [ForeignKey(nameof(StopId))]
        public BusStop? BusStop { get; set; }

        // Thứ tự trạm
        [Required]
        public int StopOrder { get; set; }

        // Giờ đến dự kiến
        [Required]
        public TimeSpan ArrivalTime { get; set; }

        // Giờ rời trạm
        [Required]
        public TimeSpan DepartureTime { get; set; }

        // Trạng thái
        [StringLength(30)]
        public string Status { get; set; } = "SCHEDULED";
    }
}