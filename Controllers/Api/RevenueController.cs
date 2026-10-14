using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers.Api
{
    /// <summary>
    /// Báo cáo doanh thu từ các giao dịch thanh toán thành công.
    /// Mặc định lấy tháng hiện tại; from/to dùng định dạng yyyy-MM-dd và bao gồm cả ngày to.
    /// </summary>
    [ApiController]
    [Route("api/revenue")]
    public class RevenueController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public RevenueController(ApplicationDbContext db) => _db = db;

        /// <summary>Tổng quan doanh thu trong khoảng thời gian.</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!TryGetRange(from, to, out var start, out var endExclusive, out var error))
                return BadRequest(new { success = false, message = error });

            var payments = await GetSuccessfulPayments(start, endExclusive);
            return Ok(new
            {
                success = true,
                from = start.ToString("yyyy-MM-dd"),
                to = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
                totalRevenue = payments.Sum(x => x.Amount),
                successfulTransactions = payments.Count,
                averageTransaction = payments.Count == 0 ? 0m : decimal.Round(payments.Sum(x => x.Amount) / payments.Count, 2)
            });
        }

        /// <summary>Tổng hợp doanh thu theo ngày.</summary>
        [HttpGet("daily")]
        public async Task<IActionResult> Daily([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!TryGetRange(from, to, out var start, out var endExclusive, out var error))
                return BadRequest(new { success = false, message = error });

            var payments = await GetSuccessfulPayments(start, endExclusive);
            var result = payments
                .GroupBy(x => x.RevenueDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), totalRevenue = g.Sum(x => x.Amount), successfulTransactions = g.Count() })
                .ToList();

            return Ok(new { success = true, from = start.ToString("yyyy-MM-dd"), to = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"), data = result });
        }

        /// <summary>Tổng hợp doanh thu theo tháng.</summary>
        [HttpGet("monthly")]
        public async Task<IActionResult> Monthly([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!TryGetRange(from, to, out var start, out var endExclusive, out var error))
                return BadRequest(new { success = false, message = error });

            var payments = await GetSuccessfulPayments(start, endExclusive);
            var result = payments
                .GroupBy(x => new { x.RevenueDate.Year, x.RevenueDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new { month = $"{g.Key.Year:D4}-{g.Key.Month:D2}", totalRevenue = g.Sum(x => x.Amount), successfulTransactions = g.Count() })
                .ToList();

            return Ok(new { success = true, from = start.ToString("yyyy-MM-dd"), to = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"), data = result });
        }

        /// <summary>Tổng hợp doanh thu theo tuyến xe.</summary>
        [HttpGet("by-route")]
        public async Task<IActionResult> ByRoute([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!TryGetRange(from, to, out var start, out var endExclusive, out var error))
                return BadRequest(new { success = false, message = error });

            var payments = await GetSuccessfulPayments(start, endExclusive);
            var trips = await _db.Trips.AsNoTracking().Select(t => new { t.TripCode, t.RouteId }).ToListAsync();
            var routes = await _db.BusRoutes.AsNoTracking().Select(r => new { r.Id, r.RouteName }).ToListAsync();
            var tripRoute = (from trip in trips
                             join route in routes on trip.RouteId equals route.Id
                             select new { trip.TripCode, route.RouteName }).ToDictionary(x => x.TripCode, x => x.RouteName);

            var result = payments
                .GroupBy(p => tripRoute.TryGetValue(p.TripCode, out var routeName) ? routeName : "Chưa xác định tuyến")
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .Select(g => new { routeName = g.Key, totalRevenue = g.Sum(x => x.Amount), successfulTransactions = g.Count() })
                .ToList();

            return Ok(new { success = true, from = start.ToString("yyyy-MM-dd"), to = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"), data = result });
        }

        private async Task<List<RevenuePayment>> GetSuccessfulPayments(DateTime start, DateTime endExclusive)
        {
            // Chỉ tính giao dịch thành công, dùng CompletedAt làm thời điểm ghi nhận doanh thu.
            // Fallback CreatedAt dành cho dữ liệu cũ chưa lưu CompletedAt.
            var rows = await _db.Payments.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Success)
                .Select(p => new
                {
                    p.Amount,
                    p.TripCode,
                    RevenueDate = p.CompletedAt ?? p.CreatedAt
                })
                .Where(p => p.RevenueDate >= start && p.RevenueDate < endExclusive)
                .ToListAsync();

            return rows.Select(p => new RevenuePayment(p.Amount, p.TripCode, p.RevenueDate)).ToList();
        }

        private static bool TryGetRange(DateTime? from, DateTime? to, out DateTime start, out DateTime endExclusive, out string error)
        {
            var today = DateTime.Today;
            start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
            var lastDay = (to ?? today).Date;
            endExclusive = lastDay.AddDays(1);
            error = string.Empty;

            if (start > lastDay)
            {
                error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
                return false;
            }
            if ((lastDay - start).TotalDays > 3660)
            {
                error = "Khoảng thời gian truy vấn không được vượt quá 10 năm.";
                return false;
            }
            return true;
        }

        private sealed record RevenuePayment(decimal Amount, string TripCode, DateTime RevenueDate);
    }
}
