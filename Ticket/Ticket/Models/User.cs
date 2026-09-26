using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [MaxLength(36)]
        [Column("user_id")]
        public string UserId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty; // Băm SHA-256

        [Required]
        [MaxLength(64)]
        [Column("salt")]
        public string Salt { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Column("phone_encrypted")]
        public string PhoneEncrypted { get; set; } = string.Empty; // Mã hóa AES-256

        [Column("identity_card_encrypted")]
        public string? IdentityCardEncrypted { get; set; } // Mã hóa AES-256 (Mã SV / CCCD)

        [Column("role_id")]
        public int RoleId { get; set; } = 4; // Mặc định Role ID = 4 (CUSTOMER)

        [ForeignKey(nameof(RoleId))]
        public virtual Role? Role { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, LOCKED, PENDING_VERIFY

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}