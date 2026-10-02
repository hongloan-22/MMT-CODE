using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    /// <summary>
    /// Lưu trạng thái giao dịch thanh toán và kết quả callback/webhook từ cổng thanh toán.
    /// </summary>
    public class PaymentTransaction
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionCode { get; set; } = string.Empty;

        [MaxLength(36)]
        public string? UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [Required]
        [MaxLength(30)]
        public string Provider { get; set; } = "DEMO";

        [MaxLength(100)]
        public string? ProviderTransactionId { get; set; }

        [MaxLength(100)]
        public string? OrderCode { get; set; }

        // PENDING, SUCCESS, FAILED, CANCELLED
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public int CallbackCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastCallbackAt { get; set; }

        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Lưu payload callback gần nhất để đối soát/debug.
        /// Không nên dùng làm dữ liệu nghiệp vụ chính.
        /// </summary>
        public string? LastCallbackPayload { get; set; }
    }
}
