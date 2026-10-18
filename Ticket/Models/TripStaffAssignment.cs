using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class TripStaffAssignment
    {
        [Key]
        public int Id { get; set; }

        // Chuyến xe được phân công
        [Required]
        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        // Nhân viên được phân công
        [Required]
        [MaxLength(36)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // DRIVER = Tài xế
        // ATTENDANT = Phụ xe
        [Required]
        [MaxLength(20)]
        public string AssignmentType { get; set; } = "DRIVER";

        public DateTime AssignedAt { get; set; } =
            DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}