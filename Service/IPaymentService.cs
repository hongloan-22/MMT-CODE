using Ticket.Models;

namespace Ticket.Service
{
    // ============================================================
    // US-64 + US-65: Interface Service Thanh Toán
    // ============================================================

    public interface IPaymentService
    {
        /// <summary>
        /// US-64: Tạo bản ghi giao dịch trong CSDL và sinh mã tham chiếu nội bộ
        /// </summary>
        Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request);

        /// <summary>
        /// US-64: Truy vấn trạng thái giao dịch theo mã tham chiếu nội bộ
        /// </summary>
        Task<PaymentStatusResponseDto?> GetPaymentStatusAsync(string transactionRef);

        /// <summary>
        /// US-65: Xử lý callback/IPN từ VNPay sandbox, cập nhật trạng thái
        /// </summary>
        Task<bool> ProcessVNPayCallbackAsync(VNPayCallbackDto callback);

        /// <summary>
        /// US-65: Xử lý callback/IPN từ MoMo sandbox, cập nhật trạng thái
        /// </summary>
        Task<bool> ProcessMoMoCallbackAsync(MoMoCallbackDto callback);

        /// <summary>
        /// US-65: Xử lý callback/IPN từ ZaloPay sandbox, cập nhật trạng thái
        /// </summary>
        Task<bool> ProcessZaloPayCallbackAsync(ZaloPayCallbackDto callback);

        /// <summary>
        /// Hủy giao dịch khi hết thời gian giữ chỗ hoặc người dùng huỷ
        /// </summary>
        Task<bool> CancelPaymentAsync(string transactionRef, string reason = "");
    }
}
