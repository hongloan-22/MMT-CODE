using Ticket.Models;

namespace Ticket.Models
{
    // ============================================================
    // US-65: DTOs cho API Thanh Toán (MoMo, VNPay, ZaloPay)
    // ============================================================

    // ---- REQUEST DTOs ----

    /// <summary>
    /// Request khởi tạo giao dịch thanh toán
    /// </summary>
    public class CreatePaymentRequestDto
    {
        /// <summary>Mã giữ chỗ (từ API SeatBooking/hold)</summary>
        public string HoldId { get; set; } = string.Empty;

        /// <summary>Mã chuyến (VD: TRIP01)</summary>
        public string TripCode { get; set; } = string.Empty;

        /// <summary>Danh sách ghế (VD: ["A1","A2"])</summary>
        public List<string> SeatIds { get; set; } = new();

        /// <summary>Mã người dùng</summary>
        public string? UserId { get; set; }

        /// <summary>Phương thức: VNPay | MoMo | ZaloPay | Bank</summary>
        public string Method { get; set; } = "VNPay";

        /// <summary>Số tiền thanh toán (VND)</summary>
        public decimal Amount { get; set; }

        /// <summary>IP client (cần cho VNPay)</summary>
        public string? ClientIp { get; set; }
    }

    /// <summary>
    /// Response khi khởi tạo giao dịch thanh toán
    /// </summary>
    public class CreatePaymentResponseDto
    {
        public bool Success { get; set; }

        /// <summary>Ma tham chieu noi bo (TransactionCode)</summary>
        public string TransactionCode { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        /// <summary>Trang thai hien tai (PENDING/SUCCESS/FAILED/CANCELLED/UNKNOWN)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>URL chuyen huong den cong thanh toan sandbox (null voi Cash)</summary>
        public string? PaymentUrl { get; set; }

        /// <summary>Thời gian hết hạn giao dịch</summary>
        public DateTime ExpiresAt { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response trạng thái giao dịch
    /// </summary>
    public class PaymentStatusResponseDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? HoldId { get; set; }
        public string? GatewayTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ---- VNPay IPN / Return DTOs ----

    /// <summary>
    /// Dữ liệu VNPay trả về qua ReturnUrl và IPN (sandbox)
    /// Tham số chính theo tài liệu VNPay
    /// </summary>
    public class VNPayCallbackDto
    {
        public string? vnp_TxnRef { get; set; }
        public string? vnp_ResponseCode { get; set; }
        public string? vnp_TransactionNo { get; set; }
        public string? vnp_Amount { get; set; }
        public string? vnp_BankCode { get; set; }
        public string? vnp_PayDate { get; set; }
        public string? vnp_SecureHash { get; set; }
        public string? vnp_TransactionStatus { get; set; }
        public string? vnp_OrderInfo { get; set; }
    }

    // ---- MoMo IPN / Return DTOs ----

    /// <summary>
    /// Dữ liệu MoMo trả về (sandbox)
    /// Theo tài liệu MoMo API v2
    /// </summary>
    public class MoMoCallbackDto
    {
        public string? partnerCode { get; set; }
        public string? orderId { get; set; }
        public string? requestId { get; set; }
        public long amount { get; set; }
        public string? orderInfo { get; set; }
        public string? orderType { get; set; }
        public long transId { get; set; }
        public int resultCode { get; set; }
        public string? message { get; set; }
        public string? payType { get; set; }
        public long responseTime { get; set; }
        public string? extraData { get; set; }
        public string? signature { get; set; }
    }

    // ---- ZaloPay IPN / Return DTOs ----

    /// <summary>
    /// Dữ liệu ZaloPay trả về (sandbox)
    /// Theo tài liệu ZaloPay API
    /// </summary>
    public class ZaloPayCallbackDto
    {
        public string? data { get; set; }
        public string? mac { get; set; }
        public int type { get; set; }
    }

    /// <summary>
    /// Dữ liệu bên trong trường "data" của ZaloPay callback
    /// </summary>
    public class ZaloPayCallbackData
    {
        public string? app_id { get; set; }
        public string? app_trans_id { get; set; }
        public string? app_user { get; set; }
        public long amount { get; set; }
        public long app_time { get; set; }
        public string? embed_data { get; set; }
        public string? item { get; set; }
        public long zp_trans_id { get; set; }
        public long server_time { get; set; }
        public int channel { get; set; }
        public string? merchant_user_id { get; set; }
        public long user_fee_amount { get; set; }
        public long discount_amount { get; set; }
    }

    /// <summary>
    /// DTO cho request huy giao dich.
    /// </summary>
    public class CancelPaymentDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
