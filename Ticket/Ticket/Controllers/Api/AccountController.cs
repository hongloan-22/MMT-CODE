using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Ticket.DTOs;
using Ticket.Services;

namespace Ticket.Controllers.Api
{
    /// <summary>
    /// US-171 / US-172: Quản lý hồ sơ người dùng.
    /// Bảo mật (BR-01, NFR-01, TC-12):
    /// - Bắt buộc đăng nhập ([Authorize]).
    /// - userId LUÔN lấy từ claim trong token, KHÔNG nhận từ query/body/header.
    /// </summary>
    [Route("api/account")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// US-171: Lấy hồ sơ của chính người dùng đang đăng nhập.
        /// GET /api/account/profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Không xác định được người dùng đăng nhập" });
            }

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng" });
            }

            return Ok(profile);
        }

        /// <summary>
        /// US-172: Cập nhật hồ sơ của chính người dùng đang đăng nhập.
        /// PUT /api/account/profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Không xác định được người dùng đăng nhập" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _userService.UpdateProfileAsync(userId, dto, ipAddress);

            if (result.NotFound)
            {
                return NotFound(new { message = result.Error });
            }

            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(new { message = "Cập nhật hồ sơ thành công", data = result.Profile });
        }

        private string? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId) ? null : userId;
        }
    }
}
