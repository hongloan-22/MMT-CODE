namespace Ticket.Models
{
    public class SearchByDestinationDto
    {
        // Id trạm đến (nếu chọn trực tiếp từ danh sách)
        public int? DestinationStopId { get; set; }

        // Tên hoặc từ khóa trạm đến (nếu gõ ô tìm kiếm)
        public string? DestinationName { get; set; }

        // Lọc theo tuyến cụ thể (nếu có)
        public int? RouteId { get; set; }
    }
}