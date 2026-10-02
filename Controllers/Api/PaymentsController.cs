using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.DTOs;
using Ticket.Models;

namespace Ticket.Controllers
{
    /// <summary>
    /// API tạo giao dịch và tiếp nhận callback/webhook từ cổng thanh toán.
    /// </summary>
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentsController> _logger;

        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "PENDING", "SUCCESS", "FAILED", "CANCELLED"
            };

        private static readonly HashSet<string> TerminalStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "SUCCESS", "FAILED", "CANCELLED"
            };

        public PaymentsController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/payments
        // Tạo giao dịch ở trạng thái PENDING trước khi chuyển sang cổng thanh toán.
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreatePaymentTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var exists = await _context.PaymentTransactions
                .AnyAsync(x => x.TransactionCode == request.TransactionCode);

            if (exists)
            {
                return Conflict(new
                {
                    message = "Mã giao dịch đã tồn tại.",
                    transactionCode = request.TransactionCode
                });
            }

            var transaction = new PaymentTransaction
            {
                TransactionCode = request.TransactionCode.Trim(),
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = string.IsNullOrWhiteSpace(request.Currency)
                    ? "VND"
                    : request.Currency.Trim().ToUpperInvariant(),
                Provider = string.IsNullOrWhiteSpace(request.Provider)
                    ? "DEMO"
                    : request.Provider.Trim().ToUpperInvariant(),
                OrderCode = request.OrderCode,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = transaction.Id },
                transaction);
        }

        // GET: api/payments/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var transaction = await _context.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (transaction == null)
                return NotFound(new { message = "Không tìm thấy giao dịch." });

            return Ok(transaction);
        }

        // GET: api/payments/code/TX001
        [HttpGet("code/{transactionCode}")]
        public async Task<IActionResult> GetByCode(string transactionCode)
        {
            var transaction = await _context.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TransactionCode == transactionCode);

            if (transaction == null)
                return NotFound(new { message = "Không tìm thấy giao dịch." });

            return Ok(transaction);
        }

        /// <summary>
        /// Nhận callback/webhook từ cổng thanh toán.
        /// Header bắt buộc: X-Webhook-Signature = HMAC-SHA256(raw JSON, secret).
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // Đọc nguyên body để xác thực chữ ký trước khi deserialize.
            Request.EnableBuffering();

            using var reader = new StreamReader(
                Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(rawBody))
                return BadRequest(new { message = "Webhook body không được để trống." });

            var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
            var secret = _configuration["PaymentWebhook:Secret"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogError("PaymentWebhook:Secret chưa được cấu hình.");
                return StatusCode(500, new
                {
                    message = "Server chưa cấu hình secret cho webhook."
                });
            }

            if (!VerifySignature(rawBody, signature, secret))
            {
                _logger.LogWarning("Webhook bị từ chối do chữ ký không hợp lệ.");
                return Unauthorized(new
                {
                    message = "Chữ ký webhook không hợp lệ."
                });
            }

            PaymentWebhookRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<PaymentWebhookRequest>(
                    rawBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException)
            {
                return BadRequest(new { message = "Webhook JSON không hợp lệ." });
            }

            if (request == null)
                return BadRequest(new { message = "Webhook data không hợp lệ." });

            if (string.IsNullOrWhiteSpace(request.TransactionCode))
                return BadRequest(new { message = "Thiếu transactionCode." });

            if (!ValidStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Status không hợp lệ.",
                    allowed = ValidStatuses
                });
            }

            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(x =>
                    x.TransactionCode == request.TransactionCode);

            if (transaction == null)
            {
                // Không tự tạo giao dịch từ webhook lạ.
                return NotFound(new
                {
                    message = "Không tìm thấy giao dịch tương ứng.",
                    transactionCode = request.TransactionCode
                });
            }

            // Đối chiếu số tiền để tránh callback giả hoặc nhầm đơn.
            if (request.Amount != transaction.Amount)
            {
                _logger.LogWarning(
                    "Sai amount cho giao dịch {TransactionCode}: expected {Expected}, received {Received}",
                    transaction.TransactionCode,
                    transaction.Amount,
                    request.Amount);

                return BadRequest(new
                {
                    message = "Số tiền callback không khớp giao dịch.",
                    expectedAmount = transaction.Amount,
                    receivedAmount = request.Amount
                });
            }

            var incomingStatus = request.Status.Trim().ToUpperInvariant();
            var currentStatus = transaction.Status.Trim().ToUpperInvariant();

            // Idempotency:
            // Nếu webhook SUCCESS/FAILED/CANCELLED được gửi lại nhiều lần,
            // không xử lý nghiệp vụ lần thứ hai.
            if (currentStatus == incomingStatus && TerminalStatuses.Contains(currentStatus))
            {
                transaction.CallbackCount++;
                transaction.LastCallbackAt = DateTime.UtcNow;
                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.LastCallbackPayload = rawBody;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Webhook đã được xử lý trước đó (idempotent).",
                    transactionId = transaction.Id,
                    transactionCode = transaction.TransactionCode,
                    status = transaction.Status
                });
            }

            // Không cho giao dịch đã kết thúc quay ngược về PENDING.
            if (TerminalStatuses.Contains(currentStatus))
            {
                return Conflict(new
                {
                    message = "Giao dịch đã ở trạng thái kết thúc, không thể cập nhật lại.",
                    currentStatus = transaction.Status,
                    requestedStatus = incomingStatus
                });
            }

            // Cập nhật trạng thái.
            transaction.Status = incomingStatus;
            transaction.ProviderTransactionId =
                request.ProviderTransactionId ?? transaction.ProviderTransactionId;
            transaction.OrderCode =
                request.OrderCode ?? transaction.OrderCode;
            transaction.FailureReason =
                incomingStatus == "FAILED" ? request.FailureReason : null;

            transaction.CallbackCount++;
            transaction.LastCallbackAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.LastCallbackPayload = rawBody;

            if (incomingStatus == "SUCCESS")
                transaction.PaidAt ??= DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Webhook cập nhật giao dịch {TransactionCode}: {OldStatus} -> {NewStatus}",
                transaction.TransactionCode,
                currentStatus,
                transaction.Status);

            return Ok(new
            {
                message = "Webhook xử lý thành công.",
                transactionId = transaction.Id,
                transactionCode = transaction.TransactionCode,
                oldStatus = currentStatus,
                status = transaction.Status,
                updatedAt = transaction.UpdatedAt
            });
        }

        private static bool VerifySignature(
            string rawBody,
            string? receivedSignature,
            string secret)
        {
            if (string.IsNullOrWhiteSpace(receivedSignature))
                return false;

            // Hỗ trợ cả "hex" và "sha256=<hex>".
            var normalized = receivedSignature.Trim();

            if (normalized.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                normalized = normalized["sha256=".Length..];

            byte[] expectedBytes;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            }

            byte[] receivedBytes;

            try
            {
                receivedBytes = Convert.FromHexString(normalized);
            }
            catch
            {
                return false;
            }

            return receivedBytes.Length == expectedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(
                       receivedBytes,
                       expectedBytes);
        }
    }
}
