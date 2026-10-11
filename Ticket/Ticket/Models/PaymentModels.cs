using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    // ============================================================
    // US-64: CSDL Giao Dịch Thanh Toán
    // Thiết kế bảng Payment lưu trữ:
    //   - Trạng thái thanh toán (PaymentStatus)
    //   - Mã tham chiếu nội bộ (TransactionRef)
    //   - Mã tham chiếu từ cổng thanh toán bên ngoài (GatewayRef)
    //   - Thông tin liên kết với SeatHold và User
    // ============================================================

    /// <summary>
    /// Enum trạng thái giao dịch thanh toán
    /// </summary>
    public enum PaymentStatus
    {
        Pending = 0,    // Đang chờ xử lý (vừa tạo yêu cầu, chưa thanh toán)
        Processing = 1, // Đang xử lý tại cổng thanh toán
        Success = 2,    // Thanh toán thành công
        Failed = 3,     // Thanh toán thất bại
        Cancelled = 4,  // Bị huỷ (do hết thời gian giữ chỗ hoặc người dùng huỷ)
        Refunded = 5    // Đã hoàn tiền
    }

    /// <summary>
    /// Enum phương thức thanh toán
    /// </summary>
    public enum PaymentMethod
    {
        VNPay = 1,
        MoMo = 2,
        ZaloPay = 3,
        BankTransfer = 4
    }

    /// <summary>
    /// Bảng Payment - Lưu thông tin giao dịch thanh toán
    /// </summary>
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        // ------ MÃ THAM CHIẾU NỘI BỘ ------
        /// <summary>
        /// Mã giao dịch nội bộ duy nhất của hệ thống SmartBus
        /// Định dạng: SBGD-YYYYMMDD-XXXXX (VD: SBGD-20261003-00001)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TransactionRef { get; set; } = string.Empty;

        // ------ MÃ THAM CHIẾU TỪ CỔNG THANH TOÁN ------
        /// <summary>
        /// Mã giao dịch do cổng thanh toán bên ngoài cấp (VNPay/MoMo/ZaloPay trả về)
        /// Dùng để đối soát và tra cứu tại cổng thanh toán
        /// </summary>
        [StringLength(100)]
        public string? GatewayTransactionId { get; set; }

        /// <summary>
        /// Mã đơn hàng gửi đến cổng thanh toán (app_trans_id cho ZaloPay, orderId cho MoMo, vnp_TxnRef cho VNPay)
        /// </summary>
        [StringLength(100)]
        public string? GatewayOrderId { get; set; }

        // ------ THÔNG TIN LIÊN KẾT ------
        /// <summary>
        /// Mã giữ chỗ tạm thời (HoldId từ SeatHold)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string HoldId { get; set; } = string.Empty;

        /// <summary>
        /// Mã chuyến xe (TripCode, VD: TRIP01)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TripCode { get; set; } = string.Empty;

        /// <summary>
        /// Danh sách mã ghế đã đặt (lưu dưới dạng JSON, VD: "A1,A2,B3")
        /// </summary>
        [Required]
        public string SeatIds { get; set; } = string.Empty;

        /// <summary>
        /// Mã người dùng thực hiện giao dịch
        /// </summary>
        [StringLength(50)]
        public string? UserId { get; set; }

        // ------ THÔNG TIN THANH TOÁN ------
        /// <summary>
        /// Phương thức thanh toán
        /// </summary>
        [Required]
        public PaymentMethod Method { get; set; }

        /// <summary>
        /// Số tiền cần thanh toán (VND)
        /// </summary>
        [Column(TypeName = "decimal(18,0)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Trạng thái giao dịch hiện tại
        /// </summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // ------ THÔNG TIN THỜI GIAN ------
        /// <summary>
        /// Thời điểm tạo giao dịch
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời điểm giao dịch hết hạn (thường là 10-15 phút sau khi tạo)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Thời điểm hoàn tất giao dịch (thành công hoặc thất bại)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        // ------ THÔNG TIN PHẢN HỒI TỪ CỔNG ------
        /// <summary>
        /// Mã phản hồi từ cổng thanh toán (VD: "00" = thành công cho VNPay)
        /// </summary>
        [StringLength(20)]
        public string? GatewayResponseCode { get; set; }

        /// <summary>
        /// Mô tả phản hồi từ cổng thanh toán
        /// </summary>
        [StringLength(500)]
        public string? GatewayResponseMessage { get; set; }

        /// <summary>
        /// Chữ ký xác thực (checksum) nhận từ cổng thanh toán để xác minh tính toàn vẹn
        /// </summary>
        [StringLength(256)]
        public string? GatewaySignature { get; set; }

        /// <summary>
        /// URL redirect sau khi thanh toán (cổng trả về người dùng)
        /// </summary>
        [StringLength(500)]
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// URL IPN/webhook nhận kết quả từ cổng thanh toán (server-to-server)
        /// </summary>
        [StringLength(500)]
        public string? IpnUrl { get; set; }

        // ------ THÔNG TIN BỔ SUNG ------
        /// <summary>
        /// IP của người dùng khi thực hiện giao dịch (dùng để xác thực VNPay)
        /// </summary>
        [StringLength(45)]
        public string? UserIpAddress { get; set; }

        /// <summary>
        /// Ghi chú nội bộ (nếu có)
        /// </summary>
        [StringLength(500)]
        public string? Note { get; set; }

        // ------ COMPUTED PROPERTIES ------
        /// <summary>
        /// Kiểm tra giao dịch đã hết hạn chưa
        /// </summary>
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiresAt && Status == PaymentStatus.Pending;

        /// <summary>
        /// Kiểm tra giao dịch đã hoàn tất (thành công hoặc thất bại)
        /// </summary>
        [NotMapped]
        public bool IsFinished => Status == PaymentStatus.Success
                               || Status == PaymentStatus.Failed
                               || Status == PaymentStatus.Cancelled
                               || Status == PaymentStatus.Refunded;
    }
}
