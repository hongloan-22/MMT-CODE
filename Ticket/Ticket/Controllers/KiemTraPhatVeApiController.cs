using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KiemTraPhatVeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KiemTraPhatVeApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // us-122: KIỂM TRA VIỆC KHÔNG PHÁT VÉ KHIN THANH TOÁN CHƯA THÀNH CÔNG
        // Endpoint: POST /api/KiemTraPhatVeApi/KiemTraThanhToanPhatVe
        // =========================================================================
        [HttpPost("KiemTraThanhToanPhatVe")]
        public async Task<IActionResult> KiemTraThanhToanPhatVe([FromBody] KiemTraThanhToanRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MaGiaoDich))
            {
                return BadRequest(new { success = false, message = "Thông tin giao dịch không hợp lệ!" });
            }

            // Chuẩn hóa trạng thái thanh toán về chữ hoa
            string trangThai = request.TrangThaiThanhToan?.Trim().ToUpper() ?? "";

            // Ràng buộc nghiệp vụ Hoa Kỳ-122: Nếu thanh toán chưa thành công (PENDING, FAILED, CANCELLED)
            if (trangThai != "SUCCESS" && trangThai != "PAID")
            {
                return BadRequest(new
                {
                    success = false,
                    isTicketIssued = false, // Không phát vé
                    trangThaiThanhToan = request.TrangThaiThanhToan,
                    message = "Thanh toán chưa thành công. Hệ thống KHÔNG phát vé điện tử!"
                });
            }

            // Nếu thanh toán thành công -> Phát vé
            return Ok(new
            {
                success = true,
                isTicketIssued = true, // Cho phép phát vé
                trangThaiThanhToan = request.TrangThaiThanhToan,
                message = "Thanh toán thành công. Đã phát vé điện tử!"
            });
        }
    }

    public class KiemTraThanhToanRequest
    {
        public string MaGiaoDich { get; set; } = string.Empty;
        public string TrangThaiThanhToan { get; set; } = string.Empty; // SUCCESS, PAID, PENDING, FAILED, CANCELLED
    }
}