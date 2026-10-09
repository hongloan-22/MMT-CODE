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
            if (request == null || string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ!" });
            }

            // 1. Kiểm tra lịch trình tồn tại trong CSDL
            var schedule = await _context.BusSchedules.FindAsync(request.BusScheduleId);
            if (schedule == null || schedule.AvailableSeats <= 0)
            {
                return BadRequest(new { success = false, message = "Lịch trình không tồn tại hoặc đã hết ghế trống!" });
            }

            // 2. Sinh mã giao dịch & mã vé điện tử độc nhất
            string transactionId = $"TXN-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}";
            string ticketCode = $"ETICKET-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

            // 3. Gắn thông tin giao dịch vào chuỗi dữ liệu mã QR
            string qrCodeDataPayload = $"QR_VERIFY|Txn:{transactionId}|Ticket:{ticketCode}|Schedule:{request.BusScheduleId}|User:{request.UserId}";

            // 4. Tạo đối tượng Vé điện tử
            var electronicTicket = new ElectronicTicket
            {
                TicketCode = ticketCode,
                TransactionId = transactionId,
                QrCodeData = qrCodeDataPayload,
                BusScheduleId = request.BusScheduleId,
                UserId = request.UserId,
                TransactionAmount = request.Amount,
                TransactionTime = DateTime.Now,
                TransactionStatus = "Paid",
                IsQrScanned = false
            };

            // 5. Cập nhật số ghế và lưu giao dịch vào CSDL
            schedule.AvailableSeats -= 1;
            _context.ElectronicTickets.Add(electronicTicket);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Hoàn tất giao dịch và tạo vé điện tử gắn mã QR thành công!",
                data = new
                {
                    ticketId = electronicTicket.Id,
                    transactionId = electronicTicket.TransactionId,
                    ticketCode = electronicTicket.TicketCode,
                    qrCodeData = electronicTicket.QrCodeData,
                    amount = electronicTicket.TransactionAmount,
                    bookingTime = electronicTicket.TransactionTime.ToString("yyyy-MM-dd HH:mm:ss")
                }
            });
        }
    }

    // DTO nhận dữ liệu gửi lên từ giao diện/client
    public class TicketTransactionRequest
    {
        public int BusScheduleId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}