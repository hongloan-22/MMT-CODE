namespace SmartBusTicketing.DTOs
{
    // =====================================================
    // US-148: DTO BÁO CÁO DOANH THU
    // =====================================================

    // Bộ lọc báo cáo doanh thu
    public class RevenueReportFilterDto
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        // null = tất cả tuyến
        public int? RouteId { get; set; }

        // day (mặc định) / week / month
        public string GroupBy { get; set; } = "day";

        // true = kèm danh sách giao dịch gốc để đối chiếu
        public bool IncludeTransactions { get; set; }
    }

    // Một dòng tổng hợp theo kỳ (ngày / tuần / tháng)
    public class RevenueItemDto
    {
        public string Period { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? RouteId { get; set; }

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        // Σ PaymentTransaction với Status = "SUCCESS"
        public decimal Payment { get; set; }

        // Σ RefundTransaction với Status = "SUCCESS"
        public decimal Refund { get; set; }

        // Payment - Refund
        public decimal Net { get; set; }

        public int Transactions { get; set; }
    }

    // Tổng hợp theo ngày (biểu đồ + bảng chi tiết)
    public class RevenueDailyItemDto
    {
        // yyyy-MM-dd
        public string Date { get; set; } = string.Empty;

        public decimal Payment { get; set; }

        public decimal Refund { get; set; }

        // Payment - Refund
        public decimal Revenue { get; set; }

        public int Transactions { get; set; }
    }

    // Tổng hợp theo tuyến
    public class RevenueByRouteItemDto
    {
        public int RouteId { get; set; }

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        public decimal Payment { get; set; }

        public decimal Refund { get; set; }

        // Payment - Refund
        public decimal Revenue { get; set; }

        public int Transactions { get; set; }
    }

    // Tuyến dùng cho bộ lọc
    public class RevenueRouteOptionDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    // Một giao dịch gốc nằm trong kỳ báo cáo.
    // Dùng để đối chiếu số tổng hợp với dữ liệu gốc
    // (AC US-148 — mục 1.7, test case TC-13).
    public class RevenueTransactionDto
    {
        // PAYMENT / REFUND
        public string Type { get; set; } = string.Empty;

        public int TransactionId { get; set; }

        // PaymentTransaction.TransactionCode
        // (refund không có mã giao dịch -> để trống)
        public string TransactionCode { get; set; } = string.Empty;

        // Provider (payment) / Reason (refund)
        public string Description { get; set; } = string.Empty;

        public string BookingCode { get; set; } = string.Empty;

        public string TicketCode { get; set; } = string.Empty;

        public int RouteId { get; set; }

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        // Ngày ghi nhận vào kỳ (PaidAt/ProcessedAt ?? CreatedAt)
        public DateTime OccurredAt { get; set; }

        public DateTime CreatedAt { get; set; }

        // Luôn dương
        public decimal Amount { get; set; }

        // payment: +Amount / refund: -Amount
        // Tổng SignedAmount = NetRevenue của toàn báo cáo
        public decimal SignedAmount { get; set; }
    }

    public class RevenueReportDto
    {
        // ===== Bộ lọc đã áp dụng =====

        // yyyy-MM-dd
        public string From { get; set; } = string.Empty;

        // yyyy-MM-dd
        public string To { get; set; } = string.Empty;

        public int? RouteId { get; set; }

        public string RouteCode { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;

        public string GroupBy { get; set; } = "day";

        // ===== Thông tin báo cáo =====

        public string Currency { get; set; } = "VND";

        public DateTime GeneratedAt { get; set; }

        // false = không có dữ liệu (vẫn trả 0, không lỗi)
        public bool HasData { get; set; }

        public string? Message { get; set; }

        // ===== Số liệu tổng hợp =====

        // Σ PaymentTransaction SUCCESS (BR-22)
        public decimal GrossRevenue { get; set; }

        // Σ RefundTransaction SUCCESS
        public decimal RefundAmount { get; set; }

        // GrossRevenue - RefundAmount
        public decimal NetRevenue { get; set; }

        // Số giao dịch thanh toán thành công
        public int TransactionCount { get; set; }

        public int RefundCount { get; set; }

        // Số vé phát hành trong kỳ
        public int TicketCount { get; set; }

        public decimal AverageTransaction { get; set; }

        // ===== Chi tiết =====

        public List<RevenueRouteOptionDto> Routes { get; set; }
            = new();

        public List<RevenueByRouteItemDto> ByRoute { get; set; }
            = new();

        public List<RevenueDailyItemDto> Daily { get; set; }
            = new();

        public List<RevenueItemDto> Details { get; set; }
            = new();

        // Chỉ có dữ liệu khi includeTransactions = true
        public List<RevenueTransactionDto> Transactions { get; set; }
            = new();
    }
}
