using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Ticket.Data;
using SmartBusTicketing.Services;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly BackendAuthorizationService _authorization;
        private readonly ApplicationDbContext _context;

        public StatisticsController(
            ITripService tripService,
            BackendAuthorizationService authorization,
            ApplicationDbContext context)
        {
            _tripService = tripService;
            _authorization = authorization;
            _context = context;
        }
        // =====================================================
        // LẤY TỶ LỆ LẤP ĐẦY CỦA MỘT CHUYẾN XE (US-160)
        // GET: /api/Statistics/trip/{tripId}/occupancy
        // =====================================================
        [HttpGet("trip/{tripId:int}/occupancy")]
        public async Task<IActionResult> GetTripOccupancy(
            int tripId,
            [FromHeader(Name = "X-User-Id")] string? userId)
        {
            // Chỉ MANAGER hoặc ADMIN mới được xem thống kê
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER",
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền xem thống kê."
                });
            }

            try
            {
                var result =
                    await _tripService
                        .GetTripOccupancyAsync(tripId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // CHI TIẾT CHUYẾN XE CHO TRANG QUẢN LÝ
        // GET: /api/Statistics/trip/{tripId}/detail
        // Chỉ ADMIN/MANAGER được xem thông tin vé và doanh thu.
        // =====================================================
        [HttpGet("trip/{tripId:int}/detail")]
        public async Task<IActionResult> GetTripDetail(int tripId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "MANAGER", "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền xem chi tiết chuyến xe."
                });
            }

            var trip = await _context.Trips
                .AsNoTracking()
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Include(t => t.Schedules)
                    .ThenInclude(s => s.BusStop)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null)
            {
                return NotFound(new
                {
                    message = $"Không tìm thấy chuyến xe có ID {tripId}."
                });
            }

            var schedules = trip.Schedules
                .OrderBy(s => s.StopOrder)
                .ToList();

            var firstStop = schedules.FirstOrDefault();
            var lastStop = schedules.LastOrDefault();

            // Thống kê ghế luôn dùng công thức chung của TripServices.
            var occupancy = await _tripService.GetTripOccupancyAsync(tripId);

            // Danh sách vé thực tế của chuyến.
            var tickets = await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Booking)
                    .ThenInclude(b => b!.User)
                .Where(t => t.TripId == tripId)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            // Doanh thu thuần = thanh toán thành công - hoàn tiền thành công.
            var successfulPayments = await _context.PaymentTransactions
                .AsNoTracking()
                .Where(p =>
                    p.Status == "SUCCESS" &&
                    p.Booking != null &&
                    p.Booking.TripId == tripId)
                .Select(p => p.Amount)
                .ToListAsync();

            var grossRevenue = successfulPayments.Sum();

            var successfulRefunds = await _context.RefundTransactions
                .AsNoTracking()
                .Where(r =>
                    r.Status == "SUCCESS" &&
                    r.Ticket != null &&
                    r.Ticket.TripId == tripId)
                .Select(r => r.Amount)
                .ToListAsync();

            var refundAmount = successfulRefunds.Sum();

            var netRevenue = Math.Max(0m, grossRevenue - refundAmount);

            var assignments = await _context.TripStaffAssignments
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a => a.TripId == tripId && a.IsActive)
                .ToListAsync();

            var driverName = assignments
                .FirstOrDefault(a =>
                    string.Equals(a.AssignmentType, "DRIVER",
                        StringComparison.OrdinalIgnoreCase))
                ?.User?.FullName;

            var attendantName = assignments
                .FirstOrDefault(a =>
                    string.Equals(a.AssignmentType, "ATTENDANT",
                        StringComparison.OrdinalIgnoreCase))
                ?.User?.FullName;

            var ticketItems = tickets.Select(t =>
            {
                var ticketStatus = (string.Equals(
                        t.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "CANCELED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "REFUNDED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Booking?.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                    ? "cancelled"
                    : (string.Equals(t.Status, "VALID", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(t.Status, "USED", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(t.Booking?.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                        ? "confirmed"
                        : "pending";

                return new
                {
                    code = t.TicketCode,
                    passenger = t.Booking?.User?.FullName ?? "Chưa cập nhật",
                    seat = t.SeatCode,
                    price = t.Fare,
                    status = ticketStatus,
                    issuedAt = t.IssuedAt
                };
            }).ToList();

            return Ok(new
            {
                tripId = trip.Id,
                tripCode = trip.TripCode,
                routeCode = trip.Route?.RouteCode ?? string.Empty,
                routeName = trip.Route?.RouteName ?? string.Empty,
                originStopName = firstStop?.BusStop?.Name
                    ?? trip.Route?.StartPoint ?? "Chưa xác định",
                destinationStopName = lastStop?.BusStop?.Name
                    ?? trip.Route?.EndPoint ?? "Chưa xác định",
                tripDate = trip.TripDate.ToString("yyyy-MM-dd"),
                departureTime = trip.DepartureTime.ToString(@"hh\:mm"),
                arrivalTime = trip.ArrivalTime.HasValue
                    ? trip.ArrivalTime.Value.ToString(@"hh\:mm")
                    : (lastStop != null
                        ? lastStop.ArrivalTime.ToString(@"hh\:mm")
                        : null),
                status = trip.Status,
                isActive = trip.IsActive,
                busCode = trip.Bus?.BusCode ?? "Chưa phân xe",
                licensePlate = trip.Bus?.LicensePlate ?? string.Empty,
                totalSeats = occupancy.TotalSeats,
                bookedSeats = occupancy.BookedSeats,
                heldSeats = occupancy.HeldSeats,
                availableSeats = occupancy.AvailableSeats,
                occupancyRate = occupancy.OccupancyRate,
                revenue = netRevenue,
                driverName = driverName ?? "Chưa phân công",
                attendantName = attendantName ?? "Chưa phân công",
                stops = schedules.Select(s => new
                {
                    stopOrder = s.StopOrder,
                    stopName = s.BusStop?.Name ?? "Chưa xác định",
                    arrivalTime = s.ArrivalTime.ToString(@"hh\:mm"),
                    departureTime = s.DepartureTime.ToString(@"hh\:mm")
                }).ToList(),
                tickets = ticketItems
            });
        }

        // =====================================================
        // US16: BÁO CÁO DOANH THU
        // GET /api/Statistics/revenue?from=2026-10-01&to=2026-10-31&routeId=1
        //
        // Gross revenue = Thanh toán thành công
        // Refund amount = Hoàn tiền thành công
        // Net revenue = Gross revenue - Refund amount
        // =====================================================

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? routeId)
        {
            // 1. Kiểm tra đăng nhập
            var userId = Request.Headers["X-User-Id"]
                .ToString()
                .Trim();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message = "Vui lòng đăng nhập để xem báo cáo doanh thu."
                });
            }

            // 2. Chỉ Admin và Manager được xem báo cáo
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN",
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền xem báo cáo doanh thu."
                });
            }

            // 3. Kiểm tra điều kiện lọc
            if (!from.HasValue || !to.HasValue)
            {
                return BadRequest(new
                {
                    message = "Vui lòng cung cấp ngày bắt đầu và ngày kết thúc."
                });
            }

            var fromDate = from.Value.Date;
            var toDate = to.Value.Date;

            if (fromDate > toDate)
            {
                return BadRequest(new
                {
                    message = "Ngày bắt đầu không được lớn hơn ngày kết thúc."
                });
            }

            if (routeId.HasValue && routeId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Mã tuyến xe không hợp lệ."
                });
            }

            try
            {
                // 4. Chuẩn hóa khoảng ngày theo múi giờ Việt Nam.
                // Bao gồm toàn bộ ngày kết thúc.
                var vietnamTimeZone = GetVietnamTimeZone();

                var startLocal = DateTime.SpecifyKind(
                    fromDate,
                    DateTimeKind.Unspecified);

                var endLocalExclusive = DateTime.SpecifyKind(
                    toDate.AddDays(1),
                    DateTimeKind.Unspecified);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                    startLocal,
                    vietnamTimeZone);

                var endUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(
                    endLocalExclusive,
                    vietnamTimeZone);

                // 5. Nếu có lọc tuyến, kiểm tra tuyến tồn tại.
                if (routeId.HasValue)
                {
                    var routeExists = await _context.BusRoutes
                        .AsNoTracking()
                        .AnyAsync(r => r.Id == routeId.Value);

                    if (!routeExists)
                    {
                        return NotFound(new
                        {
                            message = "Không tìm thấy tuyến xe."
                        });
                    }
                }

                // 6. Lấy danh sách tuyến cho bộ lọc giao diện.
                // Trả tất cả tuyến để người dùng có thể đổi bộ lọc.
                var routes = await _context.BusRoutes
                    .AsNoTracking()
                    .OrderBy(r => r.RouteCode)
                    .Select(r => new
                    {
                        id = r.Id,
                        code = r.RouteCode,
                        name = r.RouteName
                    })
                    .ToListAsync();

                // 7. GROSS REVENUE:
                // Chỉ tính giao dịch thanh toán SUCCESS
                // có PaidAt nằm trong khoảng ngày đã chọn.
                var paymentQuery = _context.PaymentTransactions
                    .AsNoTracking()
                    .Where(p =>
                        p.Status == "SUCCESS" &&
                        p.PaidAt.HasValue &&
                        p.PaidAt.Value >= startUtc &&
                        p.PaidAt.Value < endUtcExclusive &&
                        p.Booking != null &&
                        p.Booking.Trip != null);

                // Lọc theo tuyến của chuyến thuộc Booking.
                if (routeId.HasValue)
                {
                    paymentQuery = paymentQuery.Where(p =>
                        p.Booking!.Trip!.RouteId == routeId.Value);
                }

                var payments = await paymentQuery
                    .Select(p => new
                    {
                        p.Amount,
                        PaidAt = p.PaidAt!.Value,

                        RouteId = p.Booking!.Trip!.RouteId,

                        RouteCode = p.Booking!.Trip!.Route!.RouteCode,

                        RouteName = p.Booking!.Trip!.Route!.RouteName
                    })
                    .ToListAsync();

                // 8. REFUND AMOUNT:
                // Chỉ tính khoản hoàn SUCCESS đã được xử lý.
                // Ngày ghi nhận hoàn tiền là ProcessedAt.
                var refundQuery = _context.RefundTransactions
                    .AsNoTracking()
                    .Where(r =>
                        r.Status == "SUCCESS" &&
                        r.ProcessedAt.HasValue &&
                        r.ProcessedAt.Value >= startUtc &&
                        r.ProcessedAt.Value < endUtcExclusive &&
                        r.Ticket != null);

                // Lọc theo tuyến lưu trên Ticket được hoàn.
                if (routeId.HasValue)
                {
                    refundQuery = refundQuery.Where(r =>
                        r.Ticket!.RouteId == routeId.Value);
                }

                var refunds = await refundQuery
                    .Select(r => new
                    {
                        r.Amount,
                        ProcessedAt = r.ProcessedAt!.Value,
                        RouteId = r.Ticket!.RouteId
                    })
                    .ToListAsync();

                // 9. Tính các chỉ số tổng quan.
                var grossRevenue = payments.Sum(p => p.Amount);

                var refundAmount = refunds.Sum(r => r.Amount);

                var netRevenue = grossRevenue - refundAmount;

                var transactionCount = payments.Count;

                var averageTransaction = transactionCount > 0
                    ? grossRevenue / transactionCount
                    : 0m;

                // 10. Tổng hợp doanh thu bán vé theo ngày Việt Nam.
                var daily = payments
                    .GroupBy(p =>
                    {
                        var utc = DateTime.SpecifyKind(
                            p.PaidAt,
                            DateTimeKind.Utc);

                        return TimeZoneInfo.ConvertTimeFromUtc(
                            utc,
                            vietnamTimeZone).Date;
                    })
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        date = g.Key.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture),

                        revenue = g.Sum(p => p.Amount),

                        transactions = g.Count()
                    })
                    .ToList();

                // 11. Tổng hợp doanh thu bán vé theo tuyến.
                var byRoute = payments
                    .GroupBy(p => new
                    {
                        p.RouteId,
                        p.RouteCode,
                        p.RouteName
                    })
                    .Select(g => new
                    {
                        routeId = g.Key.RouteId,
                        routeCode = g.Key.RouteCode,
                        routeName = g.Key.RouteName,

                        revenue = g.Sum(p => p.Amount),

                        transactions = g.Count()
                    })
                    .OrderByDescending(x => x.revenue)
                    .ToList();

                // 12. Trả đúng cấu trúc mà script.js đang sử dụng.
                return Ok(new
                {
                    from = fromDate.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),

                    to = toDate.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),

                    routeId,

                    netRevenue,
                    grossRevenue,
                    refundAmount,

                    transactionCount,
                    averageTransaction,

                    routes,
                    daily,
                    byRoute
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi khi tổng hợp báo cáo doanh thu."
                });
            }
        }

        // Tìm múi giờ Việt Nam trên Windows hoặc Linux.
        private static TimeZoneInfo GetVietnamTimeZone()
        {
            var timeZoneIds = new[]
            {
                "SE Asia Standard Time",
                "Asia/Ho_Chi_Minh"
            };

            foreach (var timeZoneId in timeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(
                        timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Thử ID tiếp theo.
                }
                catch (InvalidTimeZoneException)
                {
                    // Thử ID tiếp theo.
                }
            }

            return TimeZoneInfo.Utc;
        }

        // =====================================================
        // US-159: KIỂM TRA TÍNH HỢP LỆ DỮ LIỆU
        // GET: /api/Statistics/data-integrity-check
        // =====================================================
        [HttpGet("data-integrity-check")]
        public async Task<IActionResult> CheckDataIntegrity(
            [FromHeader(Name = "X-User-Id")] string? userId)
        {
            // Chỉ MANAGER hoặc ADMIN mới được xem báo cáo
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER",
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền kiểm tra dữ liệu."
                });
            }

            try
            {
                var result =
                    await _tripService
                        .CheckDataIntegrityAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi kiểm tra dữ liệu.",
                    detail = ex.Message
                });
            }
        }
    }
}