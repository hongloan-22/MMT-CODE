using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Bus_Ticketing_System.Models
{
    public class RouteStop
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới bảng Tuyến
        [Required]
        public int RouteId { get; set; }
        [ForeignKey("RouteId")]
        public BusRoute? Route { get; set; }

        // Khóa ngoại liên kết tới bảng Trạm
        [Required]
        public int StopId { get; set; }
        [ForeignKey("StopId")]
        public BusStop? BusStop { get; set; }

        // THUỘC TÍNH QUAN TRỌNG: Thứ tự trạm trên tuyến (1, 2, 3...)
        [Required]
        public int StopOrder { get; set; }

        // Khoảng cách từ trạm xuất phát đầu tiên đến trạm này (km)
        public double DistanceFromStartKm { get; set; }

        // Thời gian xe dự kiến chạy từ trạm đầu tiên tới trạm này (phút)
        public int TravelTimeFromStartMinutes { get; set; }
    }
}