using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    /// <summary>
    /// Danh sách lịch sử mua vé điện tử, hỗ trợ tìm kiếm, lọc trạng thái/ngày và phân trang.
    /// </summary>
    [ApiController]
    [Route("api/ticket-history")]
    public class PurchaseHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PurchaseHistoryController(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// GET /api/ticket-history?userId=...&search=...&status=Paid&page=1&pageSize=10
        /// userId bắt buộc để giới hạn lịch sử theo người mua.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string? userId,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { success = false, message = "Tham số userId là bắt buộc." });

            if (page < 1)
                return BadRequest(new { success = false, message = "page phải lớn hơn hoặc bằng 1." });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { success = false, message = "pageSize phải nằm trong khoảng 1 đến 100." });

            var start = from?.Date;
            var endExclusive = to?.Date.AddDays(1);

            if (start.HasValue && endExclusive.HasValue && start.Value >= endExclusive.Value)
                return BadRequest(new { success = false, message = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });

            var query =
                from ticket in _db.ElectronicTickets.AsNoTracking()
                where ticket.UserId == userId
                join schedule in _db.BusSchedules.AsNoTracking()
                    on ticket.BusScheduleId equals schedule.Id into scheduleGroup
                from schedule in scheduleGroup.DefaultIfEmpty()
                join route in _db.BusRoutes.AsNoTracking()
                    on (schedule == null ? (int?)null : schedule.BusRouteId) equals (int?)route.Id into routeGroup
                from route in routeGroup.DefaultIfEmpty()
                select new
                {
                    ticket.Id,
                    ticket.TicketCode,
                    ticket.TransactionId,
                    ticket.TransactionAmount,
                    ticket.TransactionTime,
                    ticket.TransactionStatus,
                    ticket.IsQrScanned,
                    ticket.QrScannedAt,
                    ticket.BusScheduleId,
                    DepartureTime = schedule == null ? (DateTime?)null : schedule.DepartureTime,
                    ArrivalTime = schedule == null ? (DateTime?)null : schedule.ArrivalTime,
                    RouteName = route == null ? "Chưa xác định tuyến" : route.RouteName
                };

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim();
                query = query.Where(x => x.TransactionStatus.ToLower() == normalizedStatus.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x =>
                    x.TicketCode.ToLower().Contains(keyword) ||
                    x.TransactionId.ToLower().Contains(keyword) ||
                    x.RouteName.ToLower().Contains(keyword));
            }

            if (start.HasValue)
                query = query.Where(x => x.TransactionTime >= start.Value);

            if (endExclusive.HasValue)
                query = query.Where(x => x.TransactionTime < endExclusive.Value);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .OrderByDescending(x => x.TransactionTime)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    ticketId = x.Id,
                    ticketCode = x.TicketCode,
                    transactionId = x.TransactionId,
                    amount = x.TransactionAmount,
                    purchasedAt = x.TransactionTime,
                    status = x.TransactionStatus,
                    busScheduleId = x.BusScheduleId,
                    routeName = x.RouteName,
                    departureTime = x.DepartureTime,
                    arrivalTime = x.ArrivalTime,
                    isQrScanned = x.IsQrScanned,
                    qrScannedAt = x.QrScannedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                filters = new
                {
                    userId,
                    search,
                    status,
                    from = start?.ToString("yyyy-MM-dd"),
                    to = to?.Date.ToString("yyyy-MM-dd")
                },
                pagination = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages,
                    hasPreviousPage = page > 1,
                    hasNextPage = page < totalPages
                },
                data = items
            });
        }
    }
}
