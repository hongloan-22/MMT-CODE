using System.ComponentModel.DataAnnotations;

namespace Ticket.DTOs
{
    public class CreatePaymentTransactionRequest
    {
        [Required]
        [MaxLength(50)]
        public string TransactionCode { get; set; } = string.Empty;

        [MaxLength(36)]
        public string? UserId { get; set; }

        [Range(1, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [MaxLength(30)]
        public string Provider { get; set; } = "DEMO";

        [MaxLength(100)]
        public string? OrderCode { get; set; }
    }

    /// <summary>
    /// Payload chuẩn hóa cho webhook demo.
    /// Chữ ký được truyền qua header X-Webhook-Signature.
    /// </summary>
    public class PaymentWebhookRequest
    {
        [Required]
        public string TransactionCode { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ProviderTransactionId { get; set; }

        [MaxLength(100)]
        public string? OrderCode { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }
    }
}
