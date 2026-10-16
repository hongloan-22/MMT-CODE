using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongKeSoVeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThongKeSoVeApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // HOA KỲ-186: XÂY DỰNG TRUY VẤN/API THỐNG KÊ SỐ VÉ VÀ SỐ LƯỢNG CÒN TRỐNG
        // Endpoint: GET /api/ThongKeSoVeApi/GetThongKeVeChuyenXe/{chuyenXeId}
        // =========================================================================
        [HttpGet("GetThongKeVeChuyenXe/{chuyenXeId}")]
        public async Task<IActionResult> GetThongKeVeChuyenXe(int chuyenXeId)
        {
            if (chuyenXeId <= 0)
            {
                return BadRequest(new { success = false, message = "Mã chuyến xe không hợp lệ!" });
            }

            // Tong so ghe mặc định của chuyến xe
            int tongSoGhe = 40;

            // Đếm số lượng vé đã đặt của chuyến xe này
            int soVeDaDat = await _context.Tickets
                .CountAsync(t => t.Id == chuyenXeId || t.Status == "Booked" || t.Status == "Paid");

            // Tính số lượng ghế còn trống theo quy tắc: Ghế trống = Tổng ghế - Ghế đã đặt
            int soGheConTrong = tongSoGhe - soVeDaDat;
            if (soGheConTrong < 0) soGheConTrong = 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    chuyenXeId = chuyenXeId,
                    tongSoGhe = tongSoGhe,
                    soVeDaDat = soVeDaDat,
                    soGheConTrong = soGheConTrong,
                    tyLeLapDay = $"{(double)soVeDaDat / tongSoGhe * 100:F1}%",
                    trangThaiChuyenXe = soGheConTrong == 0 ? "Hết vé" : "Còn vé"
                }
            });
        }
    }
}