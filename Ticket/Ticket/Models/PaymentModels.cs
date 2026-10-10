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
        Pending = 0,    // Dang cho thanh toan
        Success = 1,    // Thanh toan thanh cong
        Failed = 2,     // Thanh toan that bai
        Cancelled = 3,  // Da huy (het han giu cho hoac nguoi dung huy)
        Unknown = 4     // Ket qua khong xac dinh, cho doi soat
    }

    /// <summary>
    /// Enum phương thức thanh toán
    /// </summary>
    public enum PaymentMethod
    {
        VNPay = 1,
        MoMo = 2,
        ZaloPay = 3,
        BankTransfer = 4,
        Cash = 5        // Thanh toan tien mat (thanh cong ngay)
    }

    /// <summary>
    /// Bảng Payment - Lưu thông tin giao dịch thanh toán
    /// </summary>
    [Table("payment_transactions")]
    public class PaymentTransaction
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
        [Column("transaction_code")]
        public string TransactionCode { get; set; } = string.Empty;

        // ------ MÃ THAM CHIẾU TỪ CỔNG THANH TOÁN ------
        /// <summary>
        /// Mã giao dịch do cổng thanh toán bên ngoài cấp (VNPay/MoMo/ZaloPay trả về)
        /// Dùng để đối soát và tra cứu tại cổng thanh toán
        /// </summary>
        [StringLength(100)]
        [Column("order_code")]
        public string? OrderCode { get; set; }

        /// <summary>
        /// Mã đơn hàng gửi đến cổng thanh toán (app_trans_id cho ZaloPay, orderId cho MoMo, vnp_TxnRef cho VNPay)
        /// </summary>
        [StringLength(100)]
        [Column("provider_transaction_id")]
        public string? ProviderTransactionId { get; set; }

        // ------ THÔNG TIN LIÊN KẾT ------
        /// <summary>
        /// Cong thanh toan su dung (VNPay/MoMo/ZaloPay/BankTransfer/Cash).
        /// </summary>
        [Required]
        [StringLength(30)]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        // ------ THONG TIN LIEN KET ------
        /// <summary>
        /// Ma phien giu cho (SeatHold.HoldId).
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("hold_id")]
        public string HoldId { get; set; } = string.Empty;

        /// <summary>
        /// Mã chuyến xe (TripCode, VD: TRIP01)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("trip_code")]
        public string TripCode { get; set; } = string.Empty;

        /// <summary>
        /// Danh sách mã ghế đã đặt (lưu dưới dạng JSON, VD: "A1,A2,B3")
        /// </summary>
        [Required]
        [StringLength(500)]
        [Column("seat_ids")]
        public string SeatIds { get; set; } = string.Empty;

        /// <summary>
        /// Mã người dùng thực hiện giao dịch
        /// </summary>
        [StringLength(50)]
        [Column("user_id")]
        public string? UserId { get; set; }

        // ------ THÔNG TIN THANH TOÁN ------
        /// <summary>
        /// Phương thức thanh toán
        /// </summary>
        [Required]
        [Column("method")]
        public PaymentMethod Method { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(18,0)")]
        public decimal Amount { get; set; }

        [Required]
        [Column("status")]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // ------ THONG TIN THOI GIAN ------
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        // ------ THONG TIN PHAN HOI TU CONG ------
        [StringLength(20)]
        [Column("gateway_response_code")]
        public string? GatewayResponseCode { get; set; }

        /// <summary>
        /// Mô tả phản hồi từ cổng thanh toán
        /// </summary>
        [StringLength(500)]
        [Column("gateway_response_message")]
        public string? GatewayResponseMessage { get; set; }

        /// <summary>
        /// Chữ ký xác thực (checksum) nhận từ cổng thanh toán để xác minh tính toàn vẹn
        /// </summary>
        [StringLength(256)]
        [Column("gateway_signature")]
        public string? GatewaySignature { get; set; }

        /// <summary>
        /// URL redirect sau khi thanh toán (cổng trả về người dùng)
        /// </summary>
        [StringLength(500)]
        [Column("return_url")]
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// URL IPN/webhook nhận kết quả từ cổng thanh toán (server-to-server)
        /// </summary>
        [StringLength(500)]
        [Column("ipn_url")]
        public string? IpnUrl { get; set; }

        // ------ THÔNG TIN BỔ SUNG ------
        /// <summary>
        /// IP của người dùng khi thực hiện giao dịch (dùng để xác thực VNPay)
        /// </summary>
        [StringLength(45)]
        [Column("user_ip_address")]
        public string? UserIpAddress { get; set; }

        /// <summary>
        /// Ghi chú nội bộ (nếu có)
        /// </summary>
        [StringLength(500)]
        [Column("note")]
        public string? Note { get; set; }

        [Column("callback_count")]
        public int CallbackCount { get; set; } = 0;

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
                               || Status == PaymentStatus.Cancelled;
    }
}
