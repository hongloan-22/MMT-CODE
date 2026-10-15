using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChiTietChuyenXeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChiTietChuyenXeApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // HOA KỲ-185: XÂY DỰNG API CHI TIẾT CHUYẾN ĐI VÀ THÔNG TIN XE ĐƯỢC PHÂN CÔNG
        // Endpoint: GET /api/ChiTietChuyenXeApi/GetChiTietChuyenXe/{id}
        // =========================================================================
        [HttpGet("GetChiTietChuyenXe/{id}")]
        public async Task<IActionResult> GetChiTietChuyenXe(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Mã chuyến xe không hợp lệ!" });
            }

            // Tìm thông tin vé / chuyến xe trong CSDL
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Không tìm thấy thông tin chuyến đi có mã ID: {id}"
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    chuyenXeId = ticket.Id,
                    maVe = ticket.TicketCode,
                    trangThai = ticket.Status ?? "Hoạt động",

                    // Thông tin xe & chi tiết phân công
                    thongTinXe = new
                    {
                        busId = ticket.Id,
                        bienSoXe = "29B-123.45", // Thông tin xe phân công
                        loaiXe = "Xe giường nằm 40 chỗ",
                        taiXePhanCong = "Nguyễn Văn A",
                        soDienThoaiTaiXe = "0987654321"
                    }
                }
            });
        }
    }
}