using System.ComponentModel.DataAnnotations;

namespace Ticket.DTOs
{
    /// <summary>
    /// US-172: Dữ liệu client gửi lên khi cập nhật hồ sơ.
    /// Không nhận UserId — userId luôn lấy từ claim trong token.
    /// </summary>
    public class UpdateProfileRequestDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự")]
        public string PhoneNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string? Address { get; set; }

        [Url(ErrorMessage = "Đường dẫn ảnh đại diện không hợp lệ")]
        [StringLength(500, ErrorMessage = "Đường dẫn ảnh đại diện tối đa 500 ký tự")]
        public string? AvatarUrl { get; set; }
    }
}
