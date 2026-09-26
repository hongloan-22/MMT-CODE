using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ticket.Models;

namespace Ticket.Models
{
    public class Trip
    {
        [Key]
        public int Id { get; set; }

        // Tuyến xe
        [Required]
        public int RouteId { get; set; }

        [ForeignKey(nameof(RouteId))]
        public BusRoute? Route { get; set; }

        // Mã chuyến
        [Required]
        [StringLength(50)]
        public string TripCode { get; set; } = string.Empty;

        // Ngày chạy
        [Required]
        public DateTime TripDate { get; set; }

        // Giờ xuất phát
        [Required]
        public TimeSpan DepartureTime { get; set; }

        // Giờ đến dự kiến
        public TimeSpan? ArrivalTime { get; set; }

        // Trạng thái chuyến
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "SCHEDULED";

        // Số ghế
        public int TotalSeats { get; set; }

        // Số ghế còn lại
        public int AvailableSeats { get; set; }

        // Ghi chú
        [StringLength(500)]
        public string? Note { get; set; }

        // Lịch trình các trạm
        public ICollection<TripSchedule> Schedules { get; set; }
            = new List<TripSchedule>();
    }
}