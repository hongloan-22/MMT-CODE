using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefundController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RefundController(
            ApplicationDbContext context)
        {
            _context = context;
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
        // POST /api/Refund/{refundId}/process
        // DEMO: XỬ LÝ HOÀN TIỀN
        // =====================================================

        [HttpPost("{refundId:int}/process")]
        public async Task<IActionResult>
            ProcessRefund(int refundId)
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
                    .Include(x => x.Ticket)
                        .ThenInclude(x =>
                            x!.Booking)
                    .FirstOrDefaultAsync(x =>
                        x.Id == refundId &&
                        x.Ticket != null &&
                        x.Ticket.Booking != null &&
                        x.Ticket.Booking.UserId ==
                            userId);

            if (refund == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy giao dịch hoàn tiền."
                });
            }

            if (refund.Status ==
                "SUCCESS")
            {
                return Ok(new
                {
                    success = true,

                    message =
                        "Khoản hoàn tiền đã được xử lý.",

                    refundId =
                        refund.Id,

                    amount =
                        refund.Amount,

                    status =
                        refund.Status,

                    processedAt =
                        refund.ProcessedAt
                });
            }

            if (refund.Status !=
                "PENDING")
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Giao dịch hoàn tiền không còn ở trạng thái chờ xử lý."
                });
            }

            refund.Status =
                "SUCCESS";

            refund.ProcessedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,

                message =
                    "Hoàn tiền thành công.",

                refundId =
                    refund.Id,

                amount =
                    refund.Amount,

                status =
                    refund.Status,

                processedAt =
                    refund.ProcessedAt
            });
        }
    }
}