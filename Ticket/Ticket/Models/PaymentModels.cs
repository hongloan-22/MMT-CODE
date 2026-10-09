using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    // ============================================================
    // SPRINT 3 - US06: CSDL Giao Dich Thanh Toan
    // Thuc the PaymentTransaction luu:
    //   - Trang thai thanh toan (PaymentStatus)
    //   - Ma tham chieu noi bo (TransactionCode)
    //   - Ma tham chieu tu cong thanh toan (ProviderTransactionId / OrderCode)
    //   - Lien ket voi phien giu cho (SeatHold) va nguoi dung
    // ============================================================

    /// <summary>
    /// Trang thai giao dich thanh toan (theo tai lieu nghiep vu Sprint 3).
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
    /// Phuong thuc thanh toan.
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
    /// Bang PaymentTransaction - luu thong tin giao dich thanh toan.
    /// </summary>
    [Table("payment_transactions")]
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        // ------ MA THAM CHIEU NOI BO ------
        /// <summary>
        /// Ma giao dich noi bo duy nhat. Dinh dang: SBGD-YYYYMMDD-NNNNN
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("transaction_code")]
        public string TransactionCode { get; set; } = string.Empty;

        // ------ MA THAM CHIEU TU CONG THANH TOAN ------
        /// <summary>
        /// Ma don hang gui den cong thanh toan (khong dau gach ngang).
        /// </summary>
        [StringLength(100)]
        [Column("order_code")]
        public string? OrderCode { get; set; }

        /// <summary>
        /// Ma giao dich do cong thanh toan ben ngoai cap.
        /// </summary>
        [StringLength(100)]
        [Column("provider_transaction_id")]
        public string? ProviderTransactionId { get; set; }

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
        /// Ma chuyen xe (VD: TRIP01).
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("trip_code")]
        public string TripCode { get; set; } = string.Empty;

        /// <summary>
        /// Danh sach ma ghe da dat (VD: "A1,A2,B3").
        /// </summary>
        [Required]
        [StringLength(500)]
        [Column("seat_ids")]
        public string SeatIds { get; set; } = string.Empty;

        /// <summary>
        /// Ma nguoi dung thuc hien giao dich.
        /// </summary>
        [StringLength(50)]
        [Column("user_id")]
        public string? UserId { get; set; }

        // ------ THONG TIN THANH TOAN ------
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

        [StringLength(500)]
        [Column("gateway_response_message")]
        public string? GatewayResponseMessage { get; set; }

        [StringLength(256)]
        [Column("gateway_signature")]
        public string? GatewaySignature { get; set; }

        [StringLength(500)]
        [Column("return_url")]
        public string? ReturnUrl { get; set; }

        [StringLength(500)]
        [Column("ipn_url")]
        public string? IpnUrl { get; set; }

        // ------ THONG TIN BO SUNG ------
        [StringLength(45)]
        [Column("user_ip_address")]
        public string? UserIpAddress { get; set; }

        [StringLength(500)]
        [Column("note")]
        public string? Note { get; set; }

        [Column("callback_count")]
        public int CallbackCount { get; set; } = 0;

        // ------ COMPUTED PROPERTIES ------
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiresAt && Status == PaymentStatus.Pending;

        [NotMapped]
        public bool IsFinished => Status == PaymentStatus.Success
                               || Status == PaymentStatus.Failed
                               || Status == PaymentStatus.Cancelled;
    }
}
