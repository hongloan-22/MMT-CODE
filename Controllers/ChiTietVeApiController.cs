using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Service;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChiTietVeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IQrCodeService _qrCodeService;

        // Inject DbContext và QrCodeService
        public ChiTietVeApiController(ApplicationDbContext context, IQrCodeService qrCodeService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
        }

        // =========================================================================
        // MỸ-72: XÂY DỰNG API LẤY CHI TIẾT VÉ ĐIỆN TỬ
        // URL gọi API: GET /api/ChiTietVeApi/GetDetail?ticketCode=ETICKET-XXXXX
        // =========================================================================
        [HttpGet("GetDetail")]
        public async Task<IActionResult> GetDetail([FromQuery] string ticketCode)
        {
            if (string.IsNullOrEmpty(ticketCode))
            {
                return BadRequest(new { success = false, message = "Mã vé không được để trống!" });
            }

            // 1. Truy vấn chi tiết vé điện tử kèm theo Lịch trình, Tuyến xe và Hành khách
            var ticket = await _context.ElectronicTickets
                .Include(t => t.BusSchedule)
                    .ThenInclude(s => s!.BusRoute)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCode || t.TransactionId == ticketCode);

            // 2. Kiểm tra vé có tồn tại không
            if (ticket == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông tin vé điện tử phù hợp!" });
            }

            // 3. Sinh link/ảnh mã QR từ dữ liệu mã QR đã lưu
            string qrCodeImage = _qrCodeService.GenerateQrCodeBase64(ticket.QrCodeData);

            // 4. Trả về kết quả DTO chi tiết đầy đủ
            return Ok(new
            {
                success = true,
                message = "Lấy chi tiết vé điện tử thành công!",
                data = new
                {
                    ticketId = ticket.Id,
                    ticketCode = ticket.TicketCode,
                    transactionId = ticket.TransactionId,
                    passengerName = ticket.User != null ? ticket.User.FullName : "Hành khách",
                    passengerEmail = ticket.User != null ? ticket.User.Email : "",
                    routeName = ticket.BusSchedule?.BusRoute != null ? ticket.BusSchedule.BusRoute.RouteName : "N/A",
                    departureTime = ticket.BusSchedule?.DepartureTime.ToString("yyyy-MM-dd HH:mm") ?? "",
                    arrivalTime = ticket.BusSchedule?.ArrivalTime.ToString("yyyy-MM-dd HH:mm") ?? "",
                    price = ticket.TransactionAmount,
                    bookingTime = ticket.TransactionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    status = ticket.TransactionStatus,
                    isQrScanned = ticket.IsQrScanned,
                    qrScannedAt = ticket.QrScannedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    qrCodeImage = qrCodeImage
                }
            });
        }
    }
}