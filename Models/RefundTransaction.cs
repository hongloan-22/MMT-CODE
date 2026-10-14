using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    /// <summary>
    /// US-114: Trạng thái giao dịch hoàn tiền
    /// PENDING  → user vừa gửi yêu cầu
    /// APPROVED → admin đã duyệt, chờ gọi cổng hoàn tiền
    /// SUCCESS  → hoàn tiền thành công
    /// FAILED   → gọi cổng hoàn tiền thất bại
    /// REJECTED → admin từ chối yêu cầu
    /// </summary>
    public enum RefundStatus
    {
        Pending = 0,
        Approved = 1,
        Success = 2,
        Failed = 3,
        Rejected = 4
    }

    /// <summary>
    /// US-109: Entity RefundTransaction — Yêu cầu hoàn tiền cho vé đã thanh toán
    /// US-110: Admin duyệt / từ chối
    /// US-111: Thực thi hoàn tiền qua cổng thanh toán (MoMo/VNPay)
    /// </summary>
    public class RefundTransaction
    {
        [Key]
        public int Id { get; set; }

        // =========================
        // LIÊN KẾT VÉ
        // =========================
        [Required]
        public int TicketId { get; set; }

        [ForeignKey(nameof(TicketId))]
        public Ticket? Ticket { get; set; }

        // =========================
        // SỐ TIỀN (US-111)
        // =========================
        [Column(TypeName = "decimal(18,0)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal Fee { get; set; } = 0m;

        // =========================
        // TRẠNG THÁI & LÝ DO
        // =========================
        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(100)]
        public string? GatewayTxnRef { get; set; }

        // =========================
        // THỜI GIAN
        // =========================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        // =========================
        // NGƯỜI XỬ LÝ (ADMIN)
        // =========================
        [MaxLength(36)]
        public string? ProcessedByUserId { get; set; }

        [ForeignKey(nameof(ProcessedByUserId))]
        public User? ProcessedByUser { get; set; }
    }
}