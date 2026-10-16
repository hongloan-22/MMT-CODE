namespace SmartBusTicketing.DTOs
{
    // =====================================================
    // REQUEST: Tham số để lấy danh sách chuyến (US-165 + US-166)
    // =====================================================
    public class TripListRequestDto
    {
        // Lọc theo điểm đi (tùy chọn)
        public string? OriginKeyword { get; set; }

        // Lọc theo điểm đến (tùy chọn)
        public string? DestinationKeyword { get; set; }

        // Lọc theo ngày (tùy chọn)
        public DateTime? DepartureDate { get; set; }

        // Lọc theo giờ khởi hành tối thiểu (tùy chọn)
        public TimeSpan? MinDepartureTime { get; set; }

        // Lọc theo khoảng giá (tùy chọn)
        public decimal? MinFare { get; set; }
        public decimal? MaxFare { get; set; }

        // Sắp xếp: "time" (mặc định), "price", "duration"
        public string? SortBy { get; set; } = "time";

        // Sắp xếp tăng/giảm: "asc" (mặc định), "desc"
        public string? SortOrder { get; set; } = "asc";

        // Phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // ✨ US-166: Chỉ hiển thị chuyến còn chỗ
        public bool? OnlyAvailable { get; set; }
    }

    // =====================================================
    // RESPONSE: Thông tin 1 chuyến trong danh sách
    // =====================================================
    public class TripListItemDto
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;

        // Tuyến
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;

        // Điểm đi / đến
        public string OriginStopName { get; set; } = string.Empty;
        public string DestinationStopName { get; set; } = string.Empty;

        // Thời gian
        public DateTime DepartureDateTime { get; set; }
        public DateTime? ArrivalDateTime { get; set; }
        public int DurationMinutes { get; set; }

        // Khoảng cách & giá
        public double DistanceKm { get; set; }
        public decimal Fare { get; set; }

        // Ghế trống & xe
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
        public string BusCode { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        // ✨ US-166: Trạng thái chuyến
        // ACTIVE | DEPARTING_SOON (dưới 30 phút) | SOLD_OUT (hết chỗ)
        public string Status { get; set; } = "ACTIVE";

        // ✨ US-166: Số phút còn lại đến giờ khởi hành
        public int MinutesUntilDeparture { get; set; }

        // ✨ US-166: Số trạm dừng giữa (không tính điểm đi/đến)
        public int StopCount { get; set; }

        // ✨ US-166: Địa chỉ điểm đi / điểm đến
        public string OriginAddress { get; set; } = string.Empty;
        public string DestinationAddress { get; set; } = string.Empty;
    }

    // =====================================================
    // RESPONSE: Kết quả có phân trang
    // =====================================================
    public class TripListResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<TripListItemDto> Items { get; set; } = new();
    }
}