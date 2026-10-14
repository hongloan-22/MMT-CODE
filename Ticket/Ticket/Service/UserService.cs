using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.DTOs;
using Ticket.Models;

namespace Ticket.Services
{
    /// <summary>
    /// Kết quả cập nhật hồ sơ (US-172).
    /// </summary>
    public class UpdateProfileResult
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Error { get; set; }
        public UserProfileResponseDto? Profile { get; set; }
    }

    /// <summary>
    /// Nghiệp vụ hồ sơ người dùng (US-171, US-172).
    /// </summary>
    public interface IUserService
    {
        Task<UserProfileResponseDto?> GetProfileAsync(string userId);
        Task<UpdateProfileResult> UpdateProfileAsync(string userId, UpdateProfileRequestDto dto, string? ipAddress);
    }

    public class UserService : IUserService
    {
        // Quy ước lưu phone hiện có của project: tiền tố "enc_aes_" + số phone.
        private const string PhonePrefix = "enc_aes_";

        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public UserService(ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<UserProfileResponseDto?> GetProfileAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            return user == null ? null : MapToDto(user);
        }

        public async Task<UpdateProfileResult> UpdateProfileAsync(string userId, UpdateProfileRequestDto dto, string? ipAddress)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new UpdateProfileResult { Success = false, NotFound = true, Error = "Không tìm thấy người dùng" };
            }

            var emailLower = dto.Email.Trim().ToLower();
            var encryptedPhone = PhonePrefix + dto.PhoneNumber.Trim();

            // Kiểm tra Email đã thuộc tài khoản khác chưa
            var emailTaken = await _context.Users
                .AnyAsync(u => u.UserId != userId && u.Email.ToLower() == emailLower);
            if (emailTaken)
            {
                return new UpdateProfileResult { Success = false, Error = "Email này đã được sử dụng bởi tài khoản khác" };
            }

            // Kiểm tra Số điện thoại đã thuộc tài khoản khác chưa
            var phoneTaken = await _context.Users
                .AnyAsync(u => u.UserId != userId && u.PhoneEncrypted == encryptedPhone);
            if (phoneTaken)
            {
                return new UpdateProfileResult { Success = false, Error = "Số điện thoại này đã được sử dụng bởi tài khoản khác" };
            }

            user.FullName = dto.FullName.Trim();
            user.Email = emailLower;
            user.PhoneEncrypted = encryptedPhone;
            user.DateOfBirth = dto.DateOfBirth;
            user.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, "UPDATE_PROFILE", ipAddress);

            return new UpdateProfileResult { Success = true, Profile = MapToDto(user) };
        }

        private static UserProfileResponseDto MapToDto(User user)
        {
            return new UserProfileResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = StripPhonePrefix(user.PhoneEncrypted),
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role != null ? user.Role.RoleCode : "USER",
                Status = user.Status
            };
        }

        private static string StripPhonePrefix(string? storedPhone)
        {
            if (string.IsNullOrEmpty(storedPhone))
            {
                return string.Empty;
            }

            return storedPhone.StartsWith(PhonePrefix, StringComparison.Ordinal)
                ? storedPhone.Substring(PhonePrefix.Length)
                : storedPhone;
        }
    }
}
