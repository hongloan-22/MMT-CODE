using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Trip
    {
        

        [Key]
        public int Id { get; set; }

        [NotMapped]
        public int TripId
        {
            get => Id;
            set => Id = value;
        }

        [Required]
        public int RouteId { get; set; }
        public int? BusId { get; set; }
        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }
        public int? RecurringScheduleId { get; set; }

        [ForeignKey(nameof(RecurringScheduleId))]
        public RecurringSchedule? RecurringSchedule { get; set; }
        [ForeignKey(nameof(RouteId))]
        public BusRoute? Route { get; set; }

        [Required]
        [StringLength(50)]
        public string TripCode { get; set; } = string.Empty;

        // Ngày chạy cụ thể
        public DateTime TripDate { get; set; }


        [Required]
        public TimeSpan DepartureTime { get; set; }

        

        // Giờ đến dự kiến
        public TimeSpan? ArrivalTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";

        // Giữ lại để không phá phần giao diện đang có.
        // Booking chưa thuộc Sprint 1.
        public int TotalSeats { get; set; } = 40;

        public int AvailableSeats { get; set; } = 40;

        public int BookedSeats { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public string? Note { get; set; }

        public ICollection<TripSchedule> Schedules { get; set; }
            = new List<TripSchedule>();

    }
}