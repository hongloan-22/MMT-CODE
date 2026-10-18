using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using System.ComponentModel.DataAnnotations;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;

        public ProfileController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization)
        {
            _context = context;
            _authorization = authorization;
        }

        public class UpdateMyProfileDto
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }

        private string GetCurrentUserId()
        {
            return Request.Headers["X-User-Id"].ToString().Trim();
        }

        private object ToProfileResponse(User user)
        {
            return new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                // Dự án hiện lưu số điện thoại trong trường PhoneEncrypted.
                // Trả về trường này theo cùng quy ước của PhanQuyenController.
                phone = user.PhoneEncrypted,
                role = user.Role?.RoleCode ?? "USER",
                roleName = user.Role?.RoleName ?? "Hành khách",
                status = user.Status,
                createdAt = user.CreatedAt
            };
        }

        private async Task<bool> IsCurrentUserAllowedAsync()
        {
            return await _authorization.HasAnyRoleAsync(
                Request,
                "ADMIN",
                "MANAGER",
                "DRIVER",
                "USER");
        }

        // GET: api/Profile/me
        // Trả về hồ sơ của chính tài khoản gửi trong X-User-Id.
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để xem hồ sơ cá nhân."
                });
            }

            if (!await IsCurrentUserAllowedAsync())
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Tài khoản không có quyền truy cập hồ sơ."
                });
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    x.Status == "ACTIVE");

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa."
                });
            }

            return Ok(new
            {
                success = true,
                data = ToProfileResponse(user)
            });
        }

        // PUT: api/Profile/me
        // Chỉ cho phép cập nhật thông tin cơ bản của chính tài khoản hiện tại.
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateMyProfileDto dto)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để cập nhật hồ sơ cá nhân."
                });
            }

            if (!await IsCurrentUserAllowedAsync())
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Tài khoản không có quyền cập nhật hồ sơ."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu hồ sơ không hợp lệ."
                });
            }

            var fullName = (dto.FullName ?? string.Empty).Trim();
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var phone = (dto.Phone ?? string.Empty).Trim();

            if (fullName.Length < 2 || fullName.Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Họ tên phải có từ 2 đến 100 ký tự."
                });
            }

            if (string.IsNullOrWhiteSpace(email) ||
                email.Length > 100 ||
                !new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email không hợp lệ hoặc vượt quá 100 ký tự."
                });
            }

            if (phone.Length != 10 || !phone.All(char.IsDigit))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Số điện thoại phải gồm đúng 10 chữ số."
                });
            }

            var user = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    x.Status == "ACTIVE");

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa."
                });
            }

            var emailAlreadyUsed = await _context.Users
                .AnyAsync(x =>
                    x.UserId != userId &&
                    x.Email.ToLower() == email);

            if (emailAlreadyUsed)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Email này đã được sử dụng bởi tài khoản khác."
                });
            }

            user.FullName = fullName;
            user.Email = email;
            user.PhoneEncrypted = phone;
            user.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.UserId,
                Action = "Cập nhật hồ sơ cá nhân",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Không thể cập nhật hồ sơ. Email có thể đã được sử dụng."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Cập nhật hồ sơ cá nhân thành công.",
                data = ToProfileResponse(user)
            });
        }
    }
}
