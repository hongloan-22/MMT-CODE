using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TicketApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/TicketApi/CreateTransactionWithQr
        // MỸ-70: THIẾT KẾ CƠ SỞ DỮ LIỆU ĐIỆN TỬ VÀ GẮN MÃ QR VỚI GIAO DỊCH
        [HttpPost("CreateTransactionWithQr")]
        public async Task<IActionResult> CreateTransactionWithQr([FromBody] TicketTransactionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "UserId và IdempotencyKey là bắt buộc. Khi retry, hãy gửi lại đúng IdempotencyKey cũ."
                });
            }

            var key = request.IdempotencyKey.Trim();
            // Callback/request lặp: trả lại vé đã tạo, tuyệt đối không trừ ghế lần nữa.
            var existingTicket = await _context.ElectronicTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdempotencyKey == key);
            if (existingTicket != null)
                return Ok(BuildResponse(existingTicket, "Yêu cầu đã được xử lý trước đó; trả về vé hiện có."));

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra lại sau khi bắt đầu transaction để thu hẹp race giữa các request.
                existingTicket = await _context.ElectronicTickets
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == key);
                if (existingTicket != null)
                {
                    await transaction.RollbackAsync();
                    return Ok(BuildResponse(existingTicket, "Yêu cầu đã được xử lý trước đó; trả về vé hiện có."));
                }

                var schedule = await _context.BusSchedules
                    .FirstOrDefaultAsync(s => s.Id == request.BusScheduleId);
                if (schedule == null || schedule.AvailableSeats <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = "Lịch trình không tồn tại hoặc đã hết ghế trống!" });
                }

                string transactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();
                string ticketCode = $"ETICKET-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant();
                string qrCodeDataPayload = $"QR_VERIFY|Txn:{transactionId}|Ticket:{ticketCode}|Schedule:{request.BusScheduleId}|User:{request.UserId}";

                var electronicTicket = new ElectronicTicket
                {
                    TicketCode = ticketCode,
                    TransactionId = transactionId,
                    IdempotencyKey = key,
                    QrCodeData = qrCodeDataPayload,
                    BusScheduleId = request.BusScheduleId,
                    UserId = request.UserId,
                    TransactionAmount = request.Amount,
                    TransactionTime = DateTime.Now,
                    TransactionStatus = "Paid",
                    IsQrScanned = false
                };

                schedule.AvailableSeats -= 1;
                _context.ElectronicTickets.Add(electronicTicket);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(BuildResponse(electronicTicket, "Hoàn tất giao dịch và tạo vé điện tử gắn mã QR thành công!"));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                // Nếu hai request cùng IdempotencyKey chạy đồng thời, unique index chỉ cho một request thắng.
                var existing = await _context.ElectronicTickets.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == key);
                if (existing != null)
                    return Ok(BuildResponse(existing, "Yêu cầu đã được xử lý trước đó; trả về vé hiện có."));
                throw;
            }
        }

        private static object BuildResponse(ElectronicTicket ticket, string message) => new
        {
            success = true,
            message,
            data = new
            {
                ticketId = ticket.Id,
                transactionId = ticket.TransactionId,
                ticketCode = ticket.TicketCode,
                qrCodeData = ticket.QrCodeData,
                amount = ticket.TransactionAmount,
                bookingTime = ticket.TransactionTime.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };

    }

    // DTO nhận dữ liệu gửi lên từ giao diện/client
    public class TicketTransactionRequest
    {
        public int BusScheduleId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}