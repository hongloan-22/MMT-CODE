using System.ComponentModel.DataAnnotations;
using Ticket.Models;

namespace Ticket.Models
{
    public class BusRoute
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên tuyến không được để trống")]
        [StringLength(150)]
        public string RouteName { get; set; } = string.Empty; // Ví dụ: Tuyến 01: Bến xe Gia Lâm - Bến xe Yên Nghĩa

        public double TotalDistanceKm { get; set; } // Tổng chiều dài tuyến (km)

        public TimeSpan EstimatedDuration { get; set; } // Thời gian chạy dự kiến (vd: 01:30:00)

        public bool IsActive { get; set; } = true;

        // Quan hệ 1 - N: Một tuyến bao gồm danh sách nhiều trạm dừng theo thứ tự
        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public ICollection<Trip> Trips { get; set; }
        = new List<Trip>();
    }
}