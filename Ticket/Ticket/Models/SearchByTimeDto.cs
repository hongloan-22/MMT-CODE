namespace Ticket.Models
{
    public class SearchByTimeDto
    {
        // Lọc theo mã tuyến xe (nếu chọn)
        public int? RouteId { get; set; }

        // Thời gian di chuyển tối thiểu từ trạm đầu (phút)
        public int? MinTravelTimeMinutes { get; set; }

        // Thời gian di chuyển tối đa từ trạm đầu (phút)
        public int? MaxTravelTimeMinutes { get; set; }

        // Giờ xe khởi hành tại bến đầu (Định dạng HH:mm, ví dụ: "07:30")
        public string? StartTime { get; set; }
    }
}