using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Kiểm tra xem DB có dữ liệu hay chưa
            var totalUsers = await _context.Users.CountAsync();
            if (totalUsers == 0)
            {
                return StatusCode(401, new
                {
                    message = "LỖI DB RỖNG: InMemory DB chưa có bản ghi nào! Bạn chưa cấu hình db.Database.EnsureCreated() trong Program.cs."
                });
            }

            // 2. Tìm user theo Email (dùng ToLower() để tránh lệch hoa/thường)
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());

            if (user == null)
            {
                var existingEmails = await _context.Users.Select(u => u.Email).ToListAsync();
                return StatusCode(401, new
                {
                    message = $"Không tìm thấy user với email: '{request.Email}'",
                    danhSachEmailDangCoTrongDb = existingEmails
                });
            }

            // 3. Kiểm tra trạng thái tài khoản
            if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = $"Tài khoản đang ở trạng thái {user.Status}, không thể đăng nhập." });
            }

            
            // 4. So khớp hash SHA-256 (Password + Salt)
            var inputPasswordHash = HashPassword(request.Password, user.Salt);
            if (!string.Equals(user.PasswordHash, inputPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
            }

            // 5. Trả về thông tin đăng nhập thành công
            return Ok(new
            {
                message = "Đăng nhập thành công!",
                data = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    roleId = user.RoleId,
                    roleName = user.Role != null ? user.Role.RoleName : null,
                    roleCode = user.Role != null ? user.Role.RoleCode : null,
                    status = user.Status
                }
            });
        }

        private static string HashPassword(string password, string salt)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            // Bắt buộc là password + salt:
            var combinedBytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha256.ComputeHash(combinedBytes);

            var sb = new System.Text.StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // Chữ thường để khớp với chuỗi ở Seed Data
            }
            return sb.ToString();
        }
    }
}