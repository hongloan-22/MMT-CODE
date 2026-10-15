using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class BusStaffAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusId { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }

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