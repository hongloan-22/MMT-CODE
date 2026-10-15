using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class RecurringSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RouteId { get; set; }

        [ForeignKey(nameof(RouteId))]
        public BusRoute? Route { get; set; }

        [Required]
        public int BusId { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }

        // DAILY hoặc WEEKLY
        [Required]
        [StringLength(20)]
        public string ScheduleType { get; set; } = "DAILY";

        // DAILY: có thể để trống
        // WEEKLY: lưu "1,2,3,4,5"
        // .NET DayOfWeek: Sunday = 0, Monday = 1, ... Saturday = 6
        [StringLength(50)]
        public string? DaysOfWeek { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public TimeSpan DepartureTime { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Trip> Trips { get; set; }
            = new List<Trip>();
    }
}