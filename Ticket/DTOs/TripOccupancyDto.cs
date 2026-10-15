namespace SmartBusTicketing.DTOs
{
    public class TripOccupancyDto
    {
        public int TripId { get; set; }

        public string TripCode { get; set; } = string.Empty;

        // Tổng số ghế của chuyến
        public int TotalSeats { get; set; }

        // Số ghế đã được đặt (vé VALID hoặc USED)
        public int BookedSeats { get; set; }

        // Số ghế đang được giữ tạm (chưa thanh toán)
        public int HeldSeats { get; set; }

        // Số ghế còn trống có thể bán
        public int AvailableSeats { get; set; }

        // Tỷ lệ lấp đầy tính theo % (đã làm tròn 2 chữ số)
        public double OccupancyRate { get; set; }
    }
}