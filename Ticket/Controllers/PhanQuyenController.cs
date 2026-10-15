using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhanQuyenController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;
        private readonly AuditLogService _auditLog;

        public PhanQuyenController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization,
            AuditLogService auditLog)
        {
            _context = context;
            _authorization = authorization;
            _auditLog = auditLog;
        }

        // =========================================================
        // HASH PASSWORD
        // =========================================================
        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();

            var bytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(input));

            var builder = new StringBuilder();

            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        private static string GenerateSalt()
        {
            var bytes = RandomNumberGenerator.GetBytes(16);

            return Convert.ToBase64String(bytes);
        }

        // =========================================================
        // DTO TẠO TÀI KHOẢN
        // =========================================================
        public class CreateUserDto
        {
            public string FullName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;

            public string? Phone { get; set; }

            public int RoleId { get; set; } = 4;
        }

        // =========================================================
        // DTO SỬA TÀI KHOẢN
        // =========================================================
        public class UpdateUserDto
        {
            public string FullName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string? Phone { get; set; }

            public int RoleId { get; set; }

            public string Status { get; set; } = "ACTIVE";

            public bool IsActive { get; set; } = true;

            public string? NewPassword { get; set; }
        }

        // =========================================================
        // GET: api/PhanQuyen/danh-sach-user
        // ADMIN ONLY
        // =========================================================
        [HttpGet("danh-sach-user")]
        public async Task<IActionResult> GetUsers()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép xem danh sách tài khoản."
                });
            }

            var list = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .OrderBy(u => u.CreatedAt)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,

                    Phone = u.PhoneEncrypted,

                    u.RoleId,

                    RoleCode =
                        u.Role != null
                            ? u.Role.RoleCode
                            : "USER",

                    RoleName =
                        u.Role != null
                            ? u.Role.RoleName
                            : "Hành khách",

                    u.Status,
                    u.IsActive,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        // =========================================================
        // GET: api/PhanQuyen/danh-sach-role
        // ADMIN ONLY
        // =========================================================
        [HttpGet("danh-sach-role")]
        public async Task<IActionResult> GetRoles()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép xem danh sách vai trò."
                });
            }

            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .Select(r => new
                {
                    r.RoleId,
                    r.RoleCode,
                    r.RoleName,
                    r.Description
                })
                .ToListAsync();

            return Ok(roles);
        }

        // =========================================================
        // HỦY PHÂN CÔNG XE KHI USER KHÔNG CÒN ROLE DRIVER
        // =========================================================

        private async Task DeactivateBusAssignmentsAsync(
            string userId)
        {
            var assignments =
                await _context.BusStaffAssignments
                    .Where(
                        x =>
                            x.UserId == userId &&
                            x.IsActive)
                    .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
            }
        }

        // =========================================================
        // POST: api/PhanQuyen/tao-tai-khoan
        // ADMIN ONLY
        // =========================================================
        [HttpPost("tao-tai-khoan")]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserDto dto)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép tạo tài khoản."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(new
                {
                    message =
                        "Họ tên không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message =
                        "Email không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    message =
                        "Mật khẩu không được để trống."
                });
            }

            if (dto.Password.Length < 6)
            {
                return BadRequest(new
                {
                    message =
                        "Mật khẩu phải có ít nhất 6 ký tự."
                });
            }

            var email =
                dto.Email.Trim().ToLower();

            if (!new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(new
                {
                    message =
                        "Email không hợp lệ."
                });
            }

            var emailExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.Email.ToLower() == email);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message =
                        "Email đã tồn tại."
                });
            }

            var roleExists =
                await _context.Roles
                    .AnyAsync(r =>
                        r.RoleId == dto.RoleId);

            if (!roleExists)
            {
                return BadRequest(new
                {
                    message =
                        "Vai trò không tồn tại."
                });
            }

            var salt = GenerateSalt();

            var user = new User
            {
                UserId =
                    Guid.NewGuid().ToString(),

                FullName =
                    dto.FullName.Trim(),

                Email =
                    email,

                Salt =
                    salt,

                PasswordHash =
                    ComputeHash(
                        dto.Password + salt),

                PhoneEncrypted =
                    dto.Phone?.Trim() ?? "",

                RoleId =
                    dto.RoleId,

                Status =
                    "ACTIVE",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Tạo tài khoản",
                "User",
                user.UserId,
                "SUCCESS");

            return Ok(new
            {
                message = "Tạo tài khoản thành công.",
                userId = user.UserId
            });
        }

        // =========================================================
        // PUT: api/PhanQuyen/cap-nhat-tai-khoan/{userId}
        // ADMIN ONLY
        // =========================================================
        [HttpPut("cap-nhat-tai-khoan/{userId}")]
        public async Task<IActionResult> UpdateUser(
            string userId,
            [FromBody] UpdateUserDto dto)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép sửa tài khoản."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu không hợp lệ."
                });
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tài khoản."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(new
                {
                    message =
                        "Họ tên không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message =
                        "Email không được để trống."
                });
            }

            var email =
                dto.Email.Trim().ToLower();

            if (!new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(new
                {
                    message =
                        "Email không hợp lệ."
                });
            }

            var duplicateEmail =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId != userId &&
                        u.Email.ToLower() == email);

            if (duplicateEmail)
            {
                return BadRequest(new
                {
                    message =
                        "Email đã được sử dụng bởi tài khoản khác."
                });
            }

            var roleExists =
                await _context.Roles
                    .AnyAsync(r =>
                        r.RoleId == dto.RoleId);

            if (!roleExists)
            {
                return BadRequest(new
                {
                    message =
                        "Vai trò không tồn tại."
                });
            }

            var currentUserId =
                Request.Headers["X-User-Id"]
                    .ToString();

            // Không cho Admin tự đổi role của chính mình
            if (currentUserId == userId &&
                dto.RoleId != user.RoleId)
            {
                return BadRequest(new
                {
                    message =
                        "Không thể tự thay đổi vai trò của tài khoản Admin đang đăng nhập."
                });
            }

            user.FullName =
                dto.FullName.Trim();

            user.Email =
                email;

            user.PhoneEncrypted =
                dto.Phone?.Trim() ?? "";

            var oldRole =
    await _context.Roles
        .FirstOrDefaultAsync(
            x =>
                x.RoleId ==
                user.RoleId);

            var newRole =
                await _context.Roles
                    .FirstOrDefaultAsync(
                        x =>
                            x.RoleId ==
                            dto.RoleId);

            if (oldRole?.RoleCode == "DRIVER" &&
                newRole?.RoleCode != "DRIVER")
            {
                await DeactivateBusAssignmentsAsync(
                    user.UserId);
            }

            user.RoleId =
                dto.RoleId;

            user.Status =
                string.IsNullOrWhiteSpace(dto.Status)
                    ? "ACTIVE"
                    : dto.Status.Trim().ToUpper();

            user.IsActive =
                dto.IsActive;

            user.UpdatedAt =
                DateTime.UtcNow;

            // Nếu Admin nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(
                    dto.NewPassword))
            {
                if (dto.NewPassword.Length < 6)
                {
                    return BadRequest(new
                    {
                        message =
                            "Mật khẩu mới phải có ít nhất 6 ký tự."
                    });
                }

                var newSalt =
                    GenerateSalt();

                user.Salt =
                    newSalt;

                user.PasswordHash =
                    ComputeHash(
                        dto.NewPassword +
                        newSalt);
            }

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Cập nhật tài khoản",
                "User",
                user.UserId,
                "SUCCESS");

            return Ok(new
            {
                message = "Cập nhật tài khoản thành công."
            });
        }

        // =========================================================
        // PUT: api/PhanQuyen/cap-nhat-quyen
        // ADMIN ONLY
        // =========================================================
        public class CapNhatQuyenDto
        {
            public string UserId { get; set; } =
                string.Empty;

            public int RoleId { get; set; }
        }

        [HttpPut("cap-nhat-quyen")]
        public async Task<IActionResult> UpdateRole(
            [FromBody] CapNhatQuyenDto req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép thay đổi vai trò."
                });
            }

            if (req == null ||
                string.IsNullOrWhiteSpace(
                    req.UserId))
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu không hợp lệ."
                });
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserId == req.UserId);

            if (user == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tài khoản."
                });
            }

            var roleExists =
                await _context.Roles
                    .AnyAsync(r =>
                        r.RoleId == req.RoleId);

            if (!roleExists)
            {
                return BadRequest(new
                {
                    message =
                        "Vai trò không tồn tại."
                });
            }

            var currentUserId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (currentUserId == user.UserId &&
                req.RoleId != user.RoleId)
            {
                return BadRequest(new
                {
                    message =
                        "Không thể tự thay đổi vai trò của chính tài khoản Admin."
                });
            }
            var oldRole =
    await _context.Roles
        .FirstOrDefaultAsync(
            x =>
                x.RoleId ==
                user.RoleId);

            var newRole =
                await _context.Roles
                    .FirstOrDefaultAsync(
                        x =>
                            x.RoleId ==
                            req.RoleId);

            if (oldRole?.RoleCode == "DRIVER" &&
                newRole?.RoleCode != "DRIVER")
            {
                await DeactivateBusAssignmentsAsync(
                    user.UserId);
            }

            user.RoleId =
                req.RoleId;

            user.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Cập nhật vai trò",
                "User",
                user.UserId,
                "SUCCESS");

            return Ok(new
            {
                message = "Cập nhật vai trò thành công."
            });
        }

        // =========================================================
        // PUT: api/PhanQuyen/khoa-tai-khoan/{userId}
        // ADMIN ONLY
        // =========================================================
        [HttpPut("khoa-tai-khoan/{userId}")]
        public async Task<IActionResult> LockUser(
            string userId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép khóa tài khoản."
                });
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tài khoản."
                });
            }

            var currentUserId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (currentUserId == user.UserId)
            {
                return BadRequest(new
                {
                    message =
                        "Không thể tự khóa tài khoản Admin đang đăng nhập."
                });
            }
            await DeactivateBusAssignmentsAsync(
                user.UserId);

            user.IsActive = false;
            user.Status = "LOCKED";
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Khóa tài khoản",
                "User",
                user.UserId,
                "SUCCESS");

            return Ok(new
            {
                message =
                    "Đã khóa tài khoản."
            });
        }

        // =========================================================
        // PUT: api/PhanQuyen/mo-khoa-tai-khoan/{userId}
        // ADMIN ONLY
        // =========================================================
        [HttpPut("mo-khoa-tai-khoan/{userId}")]
        public async Task<IActionResult> UnlockUser(
            string userId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin được phép mở khóa tài khoản."
                });
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tài khoản."
                });
            }

            user.IsActive = true;
            user.Status = "ACTIVE";
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Mở khóa tài khoản",
                "User",
                user.UserId,
                "SUCCESS");

            return Ok(new
            {
                message =
                    "Đã mở khóa tài khoản."
            });
        }
    }
}