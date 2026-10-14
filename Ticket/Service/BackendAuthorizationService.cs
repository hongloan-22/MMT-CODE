using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace SmartBusTicketing.Services
{
    public class BackendAuthorizationService
    {
        private readonly ApplicationDbContext _context;

        public BackendAuthorizationService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasAnyRoleAsync(
            HttpRequest request,
            params string[] roleCodes)
        {
            var userId =
                request.Headers["X-User-Id"]
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var user =
                await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId == userId &&
                            u.Status == "ACTIVE" &&
                            u.IsActive);

            if (user == null ||
                user.Role == null)
            {
                return false;
            }

            var currentRole =
                user.Role.RoleCode
                    .Trim()
                    .ToUpperInvariant();

            if (currentRole == "ADMIN")
                return true;

            if (roleCodes == null ||
                roleCodes.Length == 0)
            {
                return false;
            }

            return roleCodes.Any(
                role =>
                    string.Equals(
                        currentRole,
                        role?.Trim(),
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}