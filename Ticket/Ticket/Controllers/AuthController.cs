using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public class RegisterDto
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? Phone { get; set; }
        }

        public class LoginRequestDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // Băm SHA-256 kết hợp chuỗi đầu vào
        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        // =====================================================
        // POST: api/auth/register (US-01)
        // =====================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Email và mật khẩu không được để trống" });
            }

            var emailLower = dto.Email.Trim().ToLower();
            var exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailLower);
            if (exists)
            {
                return BadRequest(new { message = "Email này đã được sử dụng" });
            }

            var salt = "s@lt_smartbus";
            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString(),
                FullName = dto.FullName?.Trim() ?? "Hành khách",
                Email = emailLower,
                Salt = salt,
                PasswordHash = ComputeHash(dto.Password + salt), // Băm kèm Salt chuẩn bảo mật
                RoleId = 4, // Role 4 = USER
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký tài khoản thành công", userId = newUser.UserId });
        }

        // =====================================================
        // POST: api/auth/login (US-02)
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Vui lòng nhập email và mật khẩu" });
            }

            var emailLower = dto.Email.Trim().ToLower();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower && u.Status == "ACTIVE");

            if (user == null)
            {
                return BadRequest(new { message = "Email hoặc mật khẩu không chính xác" });
            }

            // Băm mật khẩu người dùng nhập kèm Salt lấy trực tiếp từ DB của User đó
            var userSalt = user.Salt ?? "";
            var hashedInput = ComputeHash(dto.Password + userSalt);

            if (user.PasswordHash != hashedInput)
            {
                return BadRequest(new { message = "Email hoặc mật khẩu không chính xác" });
            }

            var roleCode = user.Role != null ? user.Role.RoleCode : "USER";
            var (token, expiresAt) = GenerateJwtToken(user, roleCode);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                role = roleCode,
                token,
                expiresAt
            });
        }

        // =====================================================
        // Sinh JWT token chứa claim định danh người dùng (US-171)
        // =====================================================
        private (string token, DateTime expiresAt) GenerateJwtToken(User user, string roleCode)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"]
                ?? throw new InvalidOperationException("Thiếu cấu hình 'Jwt:Key'");

            var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes) ? minutes : 120;
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, roleCode)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}