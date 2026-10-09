using Microsoft.AspNetCore.Mvc;
using Ticket.Models;
using Ticket.Service;

namespace Ticket.Controllers.Api
{
    // ============================================================
    // SPRINT 3 - US06: API Controller Thanh Toan
    //
    // Endpoints:
    //   POST /api/payment/create          - Tao giao dich
    //   GET  /api/payment/{transactionCode} - Truy van trang thai
    //   GET  /api/payment/vnpay/return    - Callback VNPay (redirect)
    //   POST /api/payment/vnpay/ipn       - IPN VNPay
    //   POST /api/payment/momo/return     - Callback MoMo (redirect)
    //   POST /api/payment/momo/ipn        - IPN MoMo
    //   POST /api/payment/zalopay/callback- Callback ZaloPay
    //   GET  /api/payment/zalopay/return  - Redirect ZaloPay
    //   POST /api/payment/cancel          - Huy giao dich
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
        /// Tao giao dich thanh toan va lay URL chuyen huong cong thanh toan.
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

            // Gan IP client de VNPay xac thuc
            request.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _paymentService.CreatePaymentAsync(request);

            if (!result.Success)
                return BadRequest(result);

            _logger.LogInformation("[API Payment] Tao giao dich {Code} thanh cong | Method={Method}",
                result.TransactionCode, result.Method);

            return Ok(result);
        }

        /// <summary>
        /// Tra cuu trang thai giao dich theo ma tham chieu noi bo.
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
        // VNPay Callback & IPN
        // ============================================================

        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VNPayReturn([FromQuery] VNPayCallbackDto callback)
        {
            _logger.LogInformation("[VNPay Return] vnp_TxnRef={TxnRef} | ResponseCode={Code}",
                callback.vnp_TxnRef, callback.vnp_ResponseCode);

            bool success = await _paymentService.ProcessVNPayCallbackAsync(callback);

            string status = (success && callback.vnp_ResponseCode == "00") ? "success" : "failed";
            string redirectUrl = $"/ket-qua-thanh-toan/index.html?status={status}&method=VNPay&gateway_ref={callback.vnp_TransactionNo}";
            return Redirect(redirectUrl);
        }

        [HttpPost("vnpay/ipn")]
        public async Task<IActionResult> VNPayIPN([FromQuery] VNPayCallbackDto callback)
        {
            _logger.LogInformation("[VNPay IPN] vnp_TxnRef={TxnRef} | ResponseCode={Code}",
                callback.vnp_TxnRef, callback.vnp_ResponseCode);

            bool success = await _paymentService.ProcessVNPayCallbackAsync(callback);

            if (success)
                return Ok(new { RspCode = "00", Message = "Confirm Success" });

            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }

        // ============================================================
        // MoMo Callback & IPN
        // ============================================================

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

        [HttpPost("momo/ipn")]
        public async Task<IActionResult> MoMoIPN([FromBody] MoMoCallbackDto callback)
        {
            _logger.LogInformation("[MoMo IPN] orderId={OrderId} | resultCode={Code}",
                callback.orderId, callback.resultCode);

            bool success = await _paymentService.ProcessMoMoCallbackAsync(callback);
            return Ok(new { success });
        }

        // ============================================================
        // ZaloPay Callback
        // ============================================================

        [HttpPost("zalopay/callback")]
        public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackDto callback)
        {
            _logger.LogInformation("[ZaloPay Callback] type={Type}", callback.type);

            bool success = await _paymentService.ProcessZaloPayCallbackAsync(callback);

            if (success)
                return Ok(new { return_code = 1, return_message = "success" });

            return Ok(new { return_code = 0, return_message = "failed" });
        }

        [HttpGet("zalopay/return")]
        public IActionResult ZaloPayReturn([FromQuery] string? apptransid, [FromQuery] int status = 1)
        {
            _logger.LogInformation("[ZaloPay Return] app_trans_id={TransId} | status={Status}", apptransid, status);

            string payStatus = status == 1 ? "success" : "failed";
            string redirectUrl = $"/ket-qua-thanh-toan/index.html?status={payStatus}&method=ZaloPay";
            return Redirect(redirectUrl);
        }

        // ============================================================
        // Huy giao dich
        // ============================================================

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
    }
}
