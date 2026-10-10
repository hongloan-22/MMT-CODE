using Ticket.Models;

namespace Ticket.Service
{
    // ============================================================
    // SPRINT 3 - US06: Interface Service Thanh Toan
    // ============================================================

    public interface IPaymentService
    {
        /// <summary>
        /// Tao ban ghi PaymentTransaction trong CSDL va sinh ma tham chieu noi bo.
        /// </summary>
        Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request);

        /// <summary>
        /// Truy van trang thai giao dich theo ma tham chieu noi bo.
        /// </summary>
        Task<PaymentStatusResponseDto?> GetPaymentStatusAsync(string transactionCode);

        /// <summary>
        /// Xu ly callback/IPN tu VNPay sandbox, cap nhat trang thai.
        /// </summary>
        Task<bool> ProcessVNPayCallbackAsync(VNPayCallbackDto callback);

        /// <summary>
        /// Xu ly callback/IPN tu MoMo sandbox, cap nhat trang thai.
        /// </summary>
        Task<bool> ProcessMoMoCallbackAsync(MoMoCallbackDto callback);

        /// <summary>
        /// Xu ly callback/IPN tu ZaloPay sandbox, cap nhat trang thai.
        /// </summary>
        Task<bool> ProcessZaloPayCallbackAsync(ZaloPayCallbackDto callback);

        /// <summary>
        /// Huy giao dich khi het thoi gian giu cho hoac nguoi dung huy.
        /// </summary>
        Task<bool> CancelPaymentAsync(string transactionCode, string reason = "");
    }
}
