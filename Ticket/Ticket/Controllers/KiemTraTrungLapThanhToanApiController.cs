using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KiemTraTrungLapThanhToanApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KiemTraTrungLapThanhToanApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // HOA KỲ-123: KIỂM TRA VIỆC KHÔNG TẠO TRÙNG LẶP KHIN XÁC ĐỊNH VÒNG TOÁN THANH TOÁN
        // Endpoint: POST /api/KiemTraTrungLapThanhToanApi/XacNhanThanhToanChongTrung
        // =========================================================================
        [HttpPost("XacNhanThanhToanChongTrung")]
        public async Task<IActionResult> XacNhanThanhToanChongTrung([FromBody] XacNhanThanhToanRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MaGiaoDich))
            {
                return BadRequest(new { success = false, message = "Mã giao dịch không hợp lệ!" });
            }

            // 1. Kiểm tra xem mã giao dịch này đã từng được lưu/xử lý trong CSDL chưa
            var veDaTonTai = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.TransactionId == request.MaGiaoDich);

            // Ràng buộc Hoa Kỳ-123: Nếu giao dịch đã tồn tại -> Ngăn chặn tạo trùng lặp
            if (veDaTonTai != null)
            {
                return Conflict(new
                {
                    success = false,
                    isDuplicate = true,
                    maGiaoDich = request.MaGiaoDich,
                    maVeHienTai = veDaTonTai.TicketCode,
                    message = "CẢNH BÁO: Giao dịch thanh toán này đã được xác nhận trước đó! Hệ thống ngăn chặn tạo trùng lặp vé."
                });
            }

            // 2. Nếu giao dịch hợp lệ và chưa từng tồn tại -> Cho phép tạo
            return Ok(new
            {
                success = true,
                isDuplicate = false,
                maGiaoDich = request.MaGiaoDich,
                message = "Giao dịch hợp lệ (Chưa bị trùng). Cho phép xử lý thanh toán và tạo vé!"
            });
        }
    }

    public class XacNhanThanhToanRequest
    {
        public string MaGiaoDich { get; set; } = string.Empty;
        public decimal SoTien { get; set; }
    }
}