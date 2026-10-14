using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhanQuyenController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PhanQuyenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm phụ kiểm tra người gửi yêu cầu có phải là Admin (RoleId = 1) không
        private async Task<bool> IsAdminAsync()
        {
            var requestUserId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrEmpty(requestUserId)) return false;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == requestUserId);
            return user != null && user.RoleId == 1;
        }

        // GET: api/PhanQuyen/danh-sach-user
        [HttpGet("danh-sach-user")]
        public async Task<IActionResult> GetUsers()
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Truy cập bị từ chối: Chỉ Quản trị viên (ADMIN) mới có quyền xem!" });
            }

            var list = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    Phone = u.PhoneEncrypted != null ? u.PhoneEncrypted.Replace("enc_aes_", "") : "",
                    u.RoleId,
                    RoleCode = u.Role != null ? u.Role.RoleCode : "USER",
                    RoleName = u.Role != null ? u.Role.RoleName : "Người dùng",
                    u.Status
                })
                .ToListAsync();

            return Ok(list);
        }

        public class CapNhatQuyenDto
        {
            public string UserId { get; set; } = string.Empty;
            public int RoleId { get; set; }
        }

        // PUT: api/PhanQuyen/cap-nhat-quyen
        [HttpPut("cap-nhat-quyen")]
        public async Task<IActionResult> UpdateRole([FromBody] CapNhatQuyenDto req)
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Truy cập bị từ chối: Chỉ Quản trị viên (ADMIN) mới có quyền đổi vai trò!" });
            }

            if (string.IsNullOrWhiteSpace(req.UserId))
                return BadRequest(new { message = "Thiếu UserId." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == req.RoleId);
            if (!roleExists)
                return BadRequest(new { message = "Vai trò không hợp lệ." });

            user.RoleId = req.RoleId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật vai trò thành công!" });
        }
    }
}