using Ticket.Data;
using Ticket.Models;

namespace Ticket.Services
{
    /// <summary>
    /// Service ghi nhật ký hành động người dùng vào bảng AuditLogs.
    /// </summary>
    public interface IAuditLogService
    {
        Task LogAsync(string userId, string action, string? ipAddress = null);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string action, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}
