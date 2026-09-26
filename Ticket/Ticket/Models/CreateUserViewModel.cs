namespace Ticket.Models
{
    public class CreateUserViewModel

    {
        public string UserId { get; set; } = Guid.NewGuid().ToString(); // UUIDs
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}