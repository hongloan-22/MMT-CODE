using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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

        public AuthController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DTO ĐĂNG KÝ
        // =========================================================
        public class RegisterDto
        {
            public string FullName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;

            public string Phone { get; set; } = string.Empty;
        }

        // =========================================================
        // DTO ĐĂNG NHẬP
        // =========================================================
        public class LoginRequestDto
        {
            public string Email { get; set; } =
                string.Empty;

            public string Password { get; set; } =
                string.Empty;
        }

        // =========================================================
        // SHA-256 + SALT
        // =========================================================
        private static string ComputeHash(
            string input)
        {
            using var sha256 =
                SHA256.Create();

            var bytes =
                sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(input));

            var builder =
                new StringBuilder();

            foreach (var b in bytes)
            {
                builder.Append(
                    b.ToString("x2"));
            }

            return builder.ToString();
        }

        // =========================================================
        // TẠO SALT NGẪU NHIÊN
        // =========================================================
        private static string GenerateSalt()
        {
            var bytes =
                RandomNumberGenerator.GetBytes(16);

            return Convert.ToBase64String(bytes);
        }

        // =========================================================
        // POST: api/Auth/register
        // US-01
        //
        // Đăng ký mới luôn là USER / Hành khách
        // =========================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu đăng ký không hợp lệ."
                });
            }

            // =====================================================
            // VALIDATE HỌ TÊN
            // =====================================================

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(new
                {
                    message = "Họ tên không được để trống."
                });
            }

            var fullName = dto.FullName.Trim();

            if (fullName.Length < 2)
            {
                return BadRequest(new
                {
                    message = "Họ tên phải có ít nhất 2 ký tự."
                });
            }

            if (fullName.Length > 100)
            {
                return BadRequest(new
                {
                    message = "Họ tên không được vượt quá 100 ký tự."
                });
            }

            // =====================================================
            // VALIDATE EMAIL
            // =====================================================

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message = "Email không được để trống."
                });
            }

            var email = dto.Email.Trim().ToLowerInvariant();

            if (!new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ."
                });
            }

            if (email.Length > 100)
            {
                return BadRequest(new
                {
                    message = "Email không được vượt quá 100 ký tự."
                });
            }

            // =====================================================
            // VALIDATE SỐ ĐIỆN THOẠI
            // =====================================================

            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không được để trống."
                });
            }

            var phone = dto.Phone.Trim();

            if (phone.Length != 10 ||
                !phone.All(char.IsDigit))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại phải gồm đúng 10 chữ số."
                });
            }

            // =====================================================
            // VALIDATE PASSWORD
            // =====================================================

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    message = "Mật khẩu không được để trống."
                });
            }

            if (dto.Password.Length < 6)
            {
                return BadRequest(new
                {
                    message = "Mật khẩu phải có ít nhất 6 ký tự."
                });
            }

            if (dto.Password.Length > 100)
            {
                return BadRequest(new
                {
                    message = "Mật khẩu không được vượt quá 100 ký tự."
                });
            }

            // =====================================================
            // KIỂM TRA EMAIL ĐÃ TỒN TẠI
            // =====================================================

            var emailExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.Email.ToLower() == email);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "Email này đã được sử dụng."
                });
            }

            // =====================================================
            // LẤY ROLE USER
            // TUYỆT ĐỐI KHÔNG NHẬN ROLE TỪ CLIENT
            // =====================================================

            var passengerRole =
                await _context.Roles
                    .FirstOrDefaultAsync(r =>
                        r.RoleCode == "USER");

            if (passengerRole == null)
            {
                return StatusCode(500, new
                {
                    message =
                        "Chưa cấu hình vai trò Hành khách trong hệ thống."
                });
            }

            // =====================================================
            // TẠO SALT + HASH PASSWORD
            // =====================================================

            var salt = GenerateSalt();

            var passwordHash =
                ComputeHash(dto.Password + salt);

            // =====================================================
            // TẠO USER
            // =====================================================

            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString(),

                FullName = fullName,

                Email = email,

                Salt = salt,

                PasswordHash = passwordHash,

                PhoneEncrypted = phone,

                IdentityCardEncrypted = null,

                RoleId = passengerRole.RoleId,

                Status = "ACTIVE",

                IsActive = true,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            // =====================================================
            // GHI AUDIT LOG
            // =====================================================

            var auditLog = new AuditLog
            {
                UserId = newUser.UserId,

                Action = "Đăng ký tài khoản",

                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?
                        .ToString(),

                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);

            // =====================================================
            // LƯU DATABASE
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new
                {
                    message =
                        "Không thể tạo tài khoản. Email có thể đã tồn tại."
                });
            }

            // =====================================================
            // TRẢ KẾT QUẢ
            // =====================================================

            return Ok(new
            {
                message = "Đăng ký tài khoản thành công.",

                userId = newUser.UserId,

                fullName = newUser.FullName,

                email = newUser.Email,

                role = passengerRole.RoleCode,

                roleName = passengerRole.RoleName
            });
        }

        // =========================================================
        // POST: api/Auth/login
        // US-02
        // =========================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto dto)
        {
            // =====================================================
            // KIỂM TRA INPUT
            // =====================================================

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu đăng nhập không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message = "Email không được để trống."
                });
            }

            if (string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new
                {
                    message = "Mật khẩu không được để trống."
                });
            }

            // =====================================================
            // CHUẨN HÓA EMAIL
            // =====================================================

            var email = dto.Email
                .Trim()
                .ToLowerInvariant();

            if (!new EmailAddressAttribute()
                .IsValid(email))
            {
                return BadRequest(new
                {
                    message = "Email không đúng định dạng."
                });
            }

            // =====================================================
            // TÌM USER + ROLE
            // =====================================================

            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.Email.ToLower() == email);

            if (user == null)
            {
                return BadRequest(new
                {
                    message =
                        "Email hoặc mật khẩu không chính xác."
                });
            }

            // =====================================================
            // KIỂM TRA TRẠNG THÁI TÀI KHOẢN
            // =====================================================

            if (!user.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Tài khoản đã bị vô hiệu hóa."
                });
            }

            if (!string.Equals(
                    user.Status,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(
                        user.Status,
                        "LOCKED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message =
                            "Tài khoản đang bị khóa."
                    });
                }

                return BadRequest(new
                {
                    message =
                        "Tài khoản hiện không hoạt động."
                });
            }

            // =====================================================
            // KIỂM TRA PASSWORD
            // =====================================================

            var salt = user.Salt ?? string.Empty;

            var hashedInput =
                ComputeHash(
                    dto.Password + salt);

            if (!string.Equals(
                    user.PasswordHash,
                    hashedInput,
                    StringComparison.Ordinal))
            {
                return BadRequest(new
                {
                    message =
                        "Email hoặc mật khẩu không chính xác."
                });
            }

            // =====================================================
            // ROLE
            // =====================================================

            var roleCode =
                user.Role?.RoleCode ?? "USER";

            var roleName =
                user.Role?.RoleName ?? "Hành khách";

            // =====================================================
            // GHI AUDIT LOG
            // =====================================================

            var auditLog = new AuditLog
            {
                UserId = user.UserId,

                Action = "Đăng nhập",

                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?
                        .ToString(),

                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            // =====================================================
            // TRẢ KẾT QUẢ
            // =====================================================

            return Ok(new
            {
                message = "Đăng nhập thành công.",

                userId = user.UserId,

                fullName = user.FullName,

                email = user.Email,

                role = roleCode,

                roleName = roleName
            });
        }
    }
}