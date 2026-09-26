namespace Ticket.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
