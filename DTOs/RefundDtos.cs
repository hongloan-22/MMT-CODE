using System.ComponentModel.DataAnnotations;

namespace Ticket.DTOs
{
    /// <summary>
    /// US-109: DTO nhận yêu cầu hoàn tiền từ user
    /// </summary>
    public class CreateRefundRequestDto
    {
        [Required]
        public int TicketId { get; set; }

        /// <summary>
        /// ⚠️ TẠM: nhận userId từ body.
        /// Khi JWT sẵn sàng → xoá field này, lấy userId từ User.FindFirst() trong controller.
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// US-110: DTO khi admin từ chối yêu cầu hoàn tiền
    /// </summary>
    public class RejectRefundDto
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do từ chối")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}