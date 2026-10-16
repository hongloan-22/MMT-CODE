namespace SmartBusTicketing.DTOs
{
    // =====================================================
    // US-159: BÁO CÁO KIỂM TRA TÍNH HỢP LỆ DỮ LIỆU
    // =====================================================
    public class DataIntegrityReportDto
    {
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        public int TotalTripsChecked { get; set; }

        public int TotalIssuesFound { get; set; }

        public DataIntegritySummaryDto Summary { get; set; } = new();

        public List<DataIntegrityIssueDto> Issues { get; set; } = new();
    }

    // =====================================================
    // TÓM TẮT SỐ LƯỢNG VẤN ĐỀ THEO LOẠI
    // =====================================================
    public class DataIntegritySummaryDto
    {
        // Trip.TotalSeats != Bus.SeatCount
        public int SeatCountMismatch { get; set; }

        // Trip.BookedSeats != COUNT(Ticket VALID/USED)
        public int BookedSeatsMismatch { get; set; }

        // Trip.AvailableSeats != TotalSeats - BookedSeats - HeldSeats
        public int AvailableSeatsMismatch { get; set; }

        // SeatHold Status=HELD nhưng ExpiresAt < now
        public int ExpiredSeatHolds { get; set; }
    }

    // =====================================================
    // MỘT VẤN ĐỀ CỤ THỂ ĐƯỢC PHÁT HIỆN
    // =====================================================
    public class DataIntegrityIssueDto
    {
        // SEAT_COUNT_MISMATCH | BOOKED_SEATS_MISMATCH
        // AVAILABLE_SEATS_MISMATCH | EXPIRED_SEAT_HOLDS
        public string IssueType { get; set; } = string.Empty;

        // HIGH | MEDIUM | LOW
        public string Severity { get; set; } = "LOW";

        // Thông tin chuyến (nếu liên quan đến Trip)
        public int? TripId { get; set; }
        public string? TripCode { get; set; }

        // Thông tin ghế (nếu liên quan đến SeatHold)
        public int? SeatHoldId { get; set; }
        public string? SeatCode { get; set; }

        // Mô tả chi tiết
        public string Description { get; set; } = string.Empty;

        // Giá trị kỳ vọng vs thực tế (nếu có)
        public string? ExpectedValue { get; set; }
        public string? ActualValue { get; set; }
    }
}