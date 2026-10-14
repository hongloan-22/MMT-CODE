namespace Ticket.DTOs
{
    /// <summary>
    /// US-171: Dữ liệu hồ sơ người dùng trả về cho client.
    /// Không chứa password hash, salt hay token nội bộ.
    /// </summary>
    public class UserProfileResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
