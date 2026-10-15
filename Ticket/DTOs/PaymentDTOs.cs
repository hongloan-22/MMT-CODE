namespace SmartBusTicketing.DTOs
{
    public class CreatePaymentRequestDto
    {
        public int BookingId { get; set; }

        public string Provider { get; set; } = "VNPay";
    }

    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }

        public int BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string TransactionCode { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}