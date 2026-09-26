using System.ComponentModel.DataAnnotations;

namespace Ticket.Models
{
    public class BusStop
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên trạm không được để trống")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty; // Ví dụ: Bến xe Mỹ Đình

        [StringLength(255)]
        public string Address { get; set; } = string.Empty; // Địa chỉ cụ thể

        // Tọa độ GPS phục vụ định vị hoặc tích hợp bản đồ Google Maps sau này
        public double? Latitude { get; set; }  // Vĩ độ
        public double? Longitude { get; set; } // Kinh độ

        // Trạng thái trạm (true: đang hoạt động, false: tạm đóng cửa)
        public bool IsActive { get; set; } = true;

        // Quan hệ 1 - N: Một trạm có thể xuất hiện trong nhiều bản ghi Tuyến-Trạm
        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    
    }
}