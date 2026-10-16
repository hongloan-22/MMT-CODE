using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DanhSachChuyenDiApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DanhSachChuyenDiApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // HOA KỲ-187: XÂY DỰNG API DANH SÁCH CHUYẾN ĐI
        // Endpoint: GET /api/DanhSachChuyenDiApi/GetDanhSachChuyenDi
        // =========================================================================
        [HttpGet("GetDanhSachChuyenDi")]
        public async Task<IActionResult> GetDanhSachChuyenDi([FromQuery] string? status)
        {
            // Truy vấn danh sách chuyến xe từ CSDL
            var query = _context.Tickets.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var danhSachChuyenXe = await query
                .Select(t => new
                {
                    chuyenXeId = t.Id,
                    maVe = t.TicketCode,
                    trangThai = t.Status ?? "Hoạt động",
                    loaiXe = "Xe giường nằm",
                    giaVe = 250000
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                tongSoChuyen = danhSachChuyenXe.Count,
                data = danhSachChuyenXe
            });
        }
    }
}