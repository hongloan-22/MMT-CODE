using Microsoft.AspNetCore.Mvc;
using Ticket.Models;
using Ticket.Service;

namespace Ticket.Controllers.Api
{
    // ============================================================
    // US-64 + US-65: API Controller Thanh Toán
    //
    // Endpoints:
    //   POST /api/payment/create          - Tạo giao dịch (US-64 + US-65)
    //   GET  /api/payment/{transRef}      - Truy vấn trạng thái (US-64)
    //   GET  /api/payment/vnpay/return    - Nhận callback VNPay (US-65)
    //   POST /api/payment/vnpay/ipn       - Nhận IPN VNPay (US-65)
    //   POST /api/payment/momo/return     - Nhận callback MoMo (US-65)
    //   POST /api/payment/momo/ipn        - Nhận IPN MoMo (US-65)
    //   POST /api/payment/zalopay/callback- Nhận callback ZaloPay (US-65)
    //   POST /api/payment/cancel          - Huỷ giao dịch
    // ============================================================

    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// US-64 + US-65: Tạo giao dịch thanh toán và lấy URL chuyển hướng cổng thanh toán
        /// POST /api/payment/create
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request)
        {
            if (request == null)
                return BadRequest(new { Success = false, Message = "Dữ liệu không hợp lệ." });

            if (string.IsNullOrWhiteSpace(request.HoldId))
                return BadRequest(new { Success = false, Message = "HoldId (mã giữ chỗ) không được để trống." });

            if (request.Amount <= 0)
                return BadRequest(new { Success = false, Message = "Số tiền thanh toán phải lớn hơn 0." });

            // Gắn IP client để VNPay xác thực
            request.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _paymentService.CreatePaymentAsync(request);

            if (!result.Success)
                return BadRequest(result);

            _logger.LogInformation("[API US-64] Tạo giao dịch {TransRef} thành công | Method={Method}",
                result.TransactionRef, result.Method);

            return Ok(result);
        }

        /// <summary>
        /// US-64: Tra cứu trạng thái giao dịch theo mã tham chiếu nội bộ
        /// GET /api/payment/{transactionRef}
        /// </summary>
        [HttpGet("{transactionCode}")]
        public async Task<IActionResult> GetPaymentStatus(string transactionCode)
        {
            var result = await _paymentService.GetPaymentStatusAsync(transactionCode);

            if (result == null)
                return NotFound(new { Success = false, Message = $"Không tìm thấy giao dịch '{transactionCode}'." });

            return Ok(result);
        }

        // ============================================================
        // US-65: VNPay Callback & IPN
        // ============================================================

        /// <summary>
        /// US-65: Nhận kết quả từ VNPay qua ReturnUrl (GET - trình duyệt redirect về)
        /// GET /api/payment/vnpay/return
        /// </summary>
        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VNPayReturn([FromQuery] VNPayCallbackDto callback)
        {
            _logger.LogInformation("[VNPay Return] vnp_TxnRef={TxnRef} | ResponseCode={Code}",
                callback.vnp_TxnRef, callback.vnp_ResponseCode);

            bool success = await _paymentService.ProcessVNPayCallbackAsync(callback);

            // Redirect người dùng về trang kết quả thanh toán
            string status = (success && callback.vnp_ResponseCode == "00") ? "success" : "failed";
            string redirectUrl = $"/ket-qua-thanh-toan/index.html?status={status}&method=VNPay&gateway_ref={callback.vnp_TransactionNo}";
            return Redirect(redirectUrl);
        }

        /// <summary>
        /// US-65: Nhận IPN từ VNPay (POST - server-to-server)
        /// POST /api/payment/vnpay/ipn
        /// </summary>
        [HttpPost("vnpay/ipn")]
        public async Task<IActionResult> VNPayIPN([FromQuery] VNPayCallbackDto callback)
        {
            _logger.LogInformation("[VNPay IPN] vnp_TxnRef={TxnRef} | ResponseCode={Code}",
                callback.vnp_TxnRef, callback.vnp_ResponseCode);

            bool success = await _paymentService.ProcessVNPayCallbackAsync(callback);

            // VNPay yêu cầu trả về JSON xác nhận đã nhận IPN
            if (success)
                return Ok(new { RspCode = "00", Message = "Confirm Success" });

            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }

        // ============================================================
        // US-65: MoMo Callback & IPN
        // ============================================================

        /// <summary>
        /// US-65: Nhận kết quả từ MoMo (POST - redirect + IPN)
        /// POST /api/payment/momo/return
        /// </summary>
        [HttpPost("momo/return")]
        public async Task<IActionResult> MoMoReturn([FromBody] MoMoCallbackDto callback)
        {
            _logger.LogInformation("[MoMo Return] orderId={OrderId} | resultCode={Code}",
                callback.orderId, callback.resultCode);

            bool success = await _paymentService.ProcessMoMoCallbackAsync(callback);
            string status = (success && callback.resultCode == 0) ? "success" : "failed";
            string redirectUrl = $"/ket-qua-thanh-toan/index.html?status={status}&method=MoMo&gateway_ref={callback.transId}";
            return Redirect(redirectUrl);
        }

        /// <summary>
        /// US-65: Nhận IPN từ MoMo (server-to-server)
        /// POST /api/payment/momo/ipn
        /// </summary>
        [HttpPost("momo/ipn")]
        public async Task<IActionResult> MoMoIPN([FromBody] MoMoCallbackDto callback)
        {
            _logger.LogInformation("[MoMo IPN] orderId={OrderId} | resultCode={Code}",
                callback.orderId, callback.resultCode);

            bool success = await _paymentService.ProcessMoMoCallbackAsync(callback);
            return Ok(new { success });
        }

        // ============================================================
        // US-65: ZaloPay Callback
        // ============================================================

        /// <summary>
        /// US-65: Nhận callback từ ZaloPay (POST - server-to-server)
        /// POST /api/payment/zalopay/callback
        /// </summary>
        [HttpPost("zalopay/callback")]
        public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackDto callback)
        {
            _logger.LogInformation("[ZaloPay Callback] type={Type}", callback.type);

            bool success = await _paymentService.ProcessZaloPayCallbackAsync(callback);

            // ZaloPay yêu cầu trả về JSON
            if (success)
                return Ok(new { return_code = 1, return_message = "success" });

            return Ok(new { return_code = 0, return_message = "failed" });
        }

        /// <summary>
        /// ZaloPay redirect URL (GET - trình duyệt redirect về sau thanh toán)
        /// GET /api/payment/zalopay/return
        /// </summary>
        [HttpGet("zalopay/return")]
        public IActionResult ZaloPayReturn([FromQuery] string? apptransid, [FromQuery] int status = 1)
        {
            _logger.LogInformation("[ZaloPay Return] app_trans_id={TransId} | status={Status}", apptransid, status);

            string payStatus = status == 1 ? "success" : "failed";
            string redirectUrl = $"/ket-qua-thanh-toan/index.html?status={payStatus}&method=ZaloPay";
            return Redirect(redirectUrl);
        }

        // ============================================================
        // Huỷ giao dịch
        // ============================================================

        /// <summary>
        /// Huỷ giao dịch thanh toán (khi người dùng chủ động huỷ)
        /// POST /api/payment/cancel
        /// Body: { "transactionRef": "SBGD-...", "reason": "..." }
        /// </summary>
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelPayment([FromBody] CancelPaymentDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.TransactionCode))
                return BadRequest(new { Success = false, Message = "transactionCode không được để trống." });

            bool success = await _paymentService.CancelPaymentAsync(request.TransactionCode, request.Reason ?? "");

            if (!success)
                return BadRequest(new { Success = false, Message = "Không thể huỷ giao dịch. Giao dịch không tồn tại hoặc đã hoàn tất." });

            return Ok(new { Success = true, Message = $"Đã huỷ giao dịch {request.TransactionCode}." });
    }

    /// <summary>
    /// DTO cho request huỷ giao dịch
    /// </summary>
    public class CancelPaymentDto
    {
        public string TransactionRef { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
