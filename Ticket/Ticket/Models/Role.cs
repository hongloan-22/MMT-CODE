using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBusTicketing.Models
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("role_code")]
        public string RoleCode { get; set; } = string.Empty; // ADMIN, MANAGER, DRIVER, USER

        [Required]
        [MaxLength(50)]
        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}