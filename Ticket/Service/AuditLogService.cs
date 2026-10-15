
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace SmartBusTicketing.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task WriteAsync(
            HttpContext httpContext,
            string action,
            string objectType,
            string? objectId,
            string result)
        {
            // Lấy ID người thao tác từ header mà hệ thống đang sử dụng.
            var userId = httpContext.Request.Headers["X-User-Id"]
                .ToString()
                .Trim();

            // AuditLog hiện yêu cầu UserId hợp lệ.
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var userExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserId == userId);

            if (!userExists)
                return;

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                ObjectType = objectType,
                ObjectId = objectId,
                Result = result,
                IpAddress = httpContext.Connection
                    .RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
