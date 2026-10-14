using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers.Api
{
    /// <summary>
    /// Xem chi tiết giao dịch thanh toán, trạng thái thanh toán và các vé thuộc giao dịch.
    /// </summary>
    [ApiController]
    [Route("api/ticket-transactions")]
    public class TransactionDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TransactionDetailsController(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// GET /api/ticket-transactions/{transactionRef}?userId=...
        /// </summary>
        [HttpGet("{transactionRef}")]
        public async Task<IActionResult> GetTransactionDetails(
            string transactionRef,
            [FromQuery] string? userId)
        {
            if (string.IsNullOrWhiteSpace(transactionRef))
                return BadRequest(new { success = false, message = "Mã giao dịch không được để trống." });

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { success = false, message = "Tham số userId là bắt buộc." });

            var payment = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.TransactionRef == transactionRef || p.GatewayTransactionId == transactionRef);

            var tickets = await _db.ElectronicTickets.AsNoTracking()
                .Include(t => t.BusSchedule)
                    .ThenInclude(s => s!.BusRoute)
                .Where(t => t.TransactionId == transactionRef || t.TicketCode == transactionRef)
                .ToListAsync();

            // Nếu tìm bằng mã giao dịch Payment, lấy tất cả vé liên kết bằng mã tham chiếu nội bộ.
            if (payment != null)
            {
                tickets = await _db.ElectronicTickets.AsNoTracking()
                    .Include(t => t.BusSchedule)
                        .ThenInclude(s => s!.BusRoute)
                    .Where(t => t.TransactionId == payment.TransactionRef ||
                                (!string.IsNullOrEmpty(payment.GatewayTransactionId) && t.TransactionId == payment.GatewayTransactionId))
                    .ToListAsync();

                if (payment.UserId != userId && (payment.UserId != null || tickets.Count == 0))
                    return NotFound(new { success = false, message = "Không tìm thấy giao dịch." });
            }

            if (payment == null && tickets.Count == 0)
                return NotFound(new { success = false, message = "Không tìm thấy vé hoặc giao dịch." });

            if (tickets.Any(t => t.UserId != userId))
                return NotFound(new { success = false, message = "Không tìm thấy vé hoặc giao dịch." });

            var primaryTicket = tickets.FirstOrDefault();
            var paymentStatus = payment?.Status.ToString()
                ?? (primaryTicket?.TransactionStatus ?? "Unknown");

            return Ok(new
            {
                success = true,
                transaction = payment == null ? null : new
                {
                    payment.Id,
                    payment.TransactionRef,
                    payment.GatewayTransactionId,
                    payment.GatewayOrderId,
                    method = payment.Method.ToString(),
                    amount = payment.Amount,
                    status = payment.Status.ToString(),
                    statusCode = (int)payment.Status,
                    payment.CreatedAt,
                    payment.ExpiresAt,
                    payment.CompletedAt,
                    payment.GatewayResponseCode,
                    payment.GatewayResponseMessage,
                    payment.Note
                },
                paymentStatus,
                ticketCount = tickets.Count,
                tickets = tickets.Select(t => new
                {
                    t.Id,
                    t.TicketCode,
                    t.TransactionId,
                    t.TransactionAmount,
                    t.TransactionTime,
                    status = t.TransactionStatus,
                    t.IsQrScanned,
                    t.QrScannedAt,
                    t.BusScheduleId,
                    routeName = t.BusSchedule != null && t.BusSchedule.BusRoute != null
                        ? t.BusSchedule.BusRoute.RouteName : "Chưa xác định tuyến",
                    departureTime = t.BusSchedule == null ? (DateTime?)null : t.BusSchedule.DepartureTime,
                    arrivalTime = t.BusSchedule == null ? (DateTime?)null : t.BusSchedule.ArrivalTime
                }).ToList()
            });
        }

        /// <summary>
        /// GET /api/ticket-transactions/ticket/{ticketCode}?userId=...
        /// Tra cứu chi tiết một vé theo mã vé.
        /// </summary>
        [HttpGet("ticket/{ticketCode}")]
        public async Task<IActionResult> GetTicketDetails(string ticketCode, [FromQuery] string? userId)
        {
            if (string.IsNullOrWhiteSpace(ticketCode))
                return BadRequest(new { success = false, message = "Mã vé không được để trống." });

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { success = false, message = "Tham số userId là bắt buộc." });

            var ticket = await _db.ElectronicTickets.AsNoTracking()
                .Include(t => t.BusSchedule)
                    .ThenInclude(s => s!.BusRoute)
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCode && t.UserId == userId);

            if (ticket == null)
                return NotFound(new { success = false, message = "Không tìm thấy vé." });

            var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p =>
                p.TransactionRef == ticket.TransactionId ||
                p.GatewayTransactionId == ticket.TransactionId);

            return Ok(new
            {
                success = true,
                ticket = new
                {
                    ticket.Id,
                    ticket.TicketCode,
                    ticket.TransactionId,
                    ticket.TransactionAmount,
                    ticket.TransactionTime,
                    status = ticket.TransactionStatus,
                    ticket.IsQrScanned,
                    ticket.QrScannedAt,
                    ticket.BusScheduleId,
                    routeName = ticket.BusSchedule?.BusRoute?.RouteName ?? "Chưa xác định tuyến",
                    departureTime = ticket.BusSchedule?.DepartureTime,
                    arrivalTime = ticket.BusSchedule?.ArrivalTime
                },
                payment = payment == null ? null : new
                {
                    payment.TransactionRef,
                    payment.GatewayTransactionId,
                    method = payment.Method.ToString(),
                    amount = payment.Amount,
                    status = payment.Status.ToString(),
                    statusCode = (int)payment.Status,
                    payment.CreatedAt,
                    payment.CompletedAt,
                    payment.GatewayResponseCode,
                    payment.GatewayResponseMessage
                },
                paymentStatus = payment?.Status.ToString() ?? ticket.TransactionStatus
            });
        }
    }
}
