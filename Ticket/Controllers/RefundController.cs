using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefundController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;

        public RefundController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization)
        {
            _context = context;
            _authorization = authorization;
        }

        // =====================================================
        // GET /api/Refund/ticket/{ticketId}
        // =====================================================

        [HttpGet("ticket/{ticketId:int}")]
        public async Task<IActionResult>
            GetByTicket(int ticketId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message =
                        "Vui lòng đăng nhập."
                });
            }

            var refund =
                await _context.RefundTransactions
                    .AsNoTracking()
                    .Include(x => x.Ticket)
                        .ThenInclude(x =>
                            x!.Booking)
                    .Where(x =>
                        x.TicketId ==
                            ticketId &&
                        x.Ticket != null &&
                        x.Ticket.Booking != null &&
                        x.Ticket.Booking.UserId ==
                            userId)
                    .OrderByDescending(
                        x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (refund == null)
            {
                return NotFound(new
                {
                    message =
                        "Chưa có giao dịch hoàn tiền."
                });
            }

            return Ok(new
            {
                refundId =
                    refund.Id,

                ticketId =
                    refund.TicketId,

                amount =
                    refund.Amount,

                reason =
                    refund.Reason,

                status =
                    refund.Status,

                createdAt =
                    refund.CreatedAt,

                processedAt =
                    refund.ProcessedAt
            });
        }

        // =====================================================
        // GET /api/Refund/pending
        // DANH SÁCH HOÀN TIỀN CHỜ XỬ LÝ
        // Chỉ ADMIN / MANAGER được truy cập
        // =====================================================

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRefunds()
        {
            var userId = Request.Headers["X-User-Id"]
                .ToString()
                .Trim();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập."
                });
            }

            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Chỉ Admin hoặc Quản lý được xem danh sách hoàn tiền."
                });
            }

            var refunds = await _context.RefundTransactions
                .AsNoTracking()
                .Where(x =>
                    x.Status == "PENDING" &&
                    x.PaymentTransaction != null &&
                    x.PaymentTransaction.Provider != "Cash" &&
                    x.Ticket != null)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    refundId = x.Id,
                    ticketId = x.TicketId,
                    ticketCode = x.Ticket!.TicketCode,
                    bookingCode = x.Ticket.Booking != null
                        ? x.Ticket.Booking.BookingCode
                        : "",
                    amount = x.Amount,
                    provider = x.PaymentTransaction!.Provider,
                    transactionCode =
                        x.PaymentTransaction.TransactionCode,
                    reason = x.Reason,
                    status = x.Status,
                    createdAt = x.CreatedAt,
                    processedAt = x.ProcessedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = refunds.Count,
                data = refunds
            });
        }


        // =====================================================
        // POST /api/Refund/{refundId}/process
        // DEMO: ADMIN / MANAGER XỬ LÝ HOÀN TIỀN
        // Chỉ mô phỏng cập nhật trạng thái, không chuyển tiền thật.
        // =====================================================

        [HttpPost("{refundId:int}/process")]
        public async Task<IActionResult> ProcessRefund(int refundId)
        {
            var userId = Request.Headers["X-User-Id"]
                .ToString()
                .Trim();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập."
                });
            }

            // Xác minh tài khoản và vai trò ở phía server.
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    u.Status == "ACTIVE" &&
                    u.IsActive);

            if (user == null || user.Role == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Tài khoản không hợp lệ hoặc đã bị khóa."
                });
            }

            var roleCode = user.Role.RoleCode
                .Trim()
                .ToUpperInvariant();

            if (roleCode != "ADMIN" && roleCode != "MANAGER")
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Chỉ Admin hoặc Manager được xử lý hoàn tiền."
                });
            }

            var refund = await _context.RefundTransactions
                .Include(r => r.PaymentTransaction)
                .Include(r => r.Ticket)
                    .ThenInclude(t => t!.Booking)
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (refund == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy yêu cầu hoàn tiền."
                });
            }

            // Không cho hoàn tiền nếu thiếu giao dịch thanh toán gốc.
            var payment = refund.PaymentTransaction;

            if (payment == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không tìm thấy giao dịch thanh toán gốc."
                });
            }

            // Chặn hoàn tiền tiền mặt, kể cả khi gọi API trực tiếp.
            if (string.Equals(
                payment.Provider?.Trim(),
                "Cash",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không hỗ trợ hoàn tiền cho vé thanh toán bằng tiền mặt."
                });
            }

            if (payment.Status != "SUCCESS")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Giao dịch thanh toán gốc chưa thành công."
                });
            }

            if (refund.Status == "SUCCESS")
            {
                return Ok(new
                {
                    success = true,
                    message = "Yêu cầu hoàn tiền đã được xử lý trước đó.",
                    refundId = refund.Id,
                    ticketId = refund.TicketId,
                    amount = refund.Amount,
                    status = refund.Status,
                    processedAt = refund.ProcessedAt
                });
            }

            if (refund.Status != "PENDING")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Yêu cầu hoàn tiền không ở trạng thái chờ xử lý."
                });
            }

            refund.Status = "SUCCESS";
            refund.ProcessedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new Ticket.Models.AuditLog
            {
                UserId = userId,
                Action = $"Xử lý hoàn tiền #{refund.Id} cho vé #{refund.TicketId}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Mô phỏng xử lý hoàn tiền thành công.",
                refundId = refund.Id,
                ticketId = refund.TicketId,
                amount = refund.Amount,
                status = refund.Status,
                processedAt = refund.ProcessedAt
            });
        }
    }
}