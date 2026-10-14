using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;

        public BookingController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization)
        {
            _context = context;
            _authorization = authorization;
        }

        // =========================================================
        // POST: api/Booking/hold
        // TẠO BOOKING + GIỮ GHẾ 10 PHÚT
        // =========================================================

        [HttpPost("hold")]
        public async Task<IActionResult> HoldSeats(
            [FromBody] HoldSeatRequestDto request)
        {
            if (!await _authorization.HasAnyRoleAsync(
        Request,
        "ADMIN",
        "MANAGER",
        "USER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Bạn không có quyền thực hiện nghiệp vụ đặt chỗ."
                    });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu đặt chỗ không hợp lệ."
                });
            }

            if (request.TripId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Chưa xác định được chuyến xe."
                });
            }

            var seatCodes =
                request.SeatCodes
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                        x.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

            if (seatCodes.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng chọn ít nhất một ghế."
                });
            }

            if (seatCodes.Count > 5)
            {
                return BadRequest(new
                {
                    message =
                        "Mỗi lần đặt chỉ được chọn tối đa 5 ghế."
                });
            }

            var userId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message =
                        "Chưa xác định được người dùng."
                });
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        x =>
                            x.UserId == userId &&
                            x.IsActive &&
                            x.Status == "ACTIVE");

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "Tài khoản không hợp lệ hoặc đã bị khóa."
                });
            }

            var trip =
                await _context.Trips
                    .Include(x => x.Bus)
                    .Include(x => x.Route)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                request.TripId &&
                            x.IsActive &&
                            x.Status != "CANCELLED");

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy chuyến xe."
                });
            }

            if (trip.Bus == null)
            {
                return BadRequest(new
                {
                    message =
                        "Chuyến xe chưa được gắn xe."
                });
            }

            if (trip.Route == null)
            {
                return BadRequest(new
                {
                    message =
                        "Chuyến xe chưa được gắn tuyến."
                });
            }

            // =========================================================
            // KIỂM TRA CHUYẾN ĐÃ QUÁ GIỜ KHỞI HÀNH
            // =========================================================

            var currentTime =
                DateTime.Now;

            var departureDateTime =
                trip.TripDate.Date
                    .Add(trip.DepartureTime);

            if (departureDateTime <= currentTime)
            {
                return BadRequest(new
                {
                    message =
                        $"Chuyến xe đã khởi hành lúc {departureDateTime:HH:mm dd/MM/yyyy}, không thể đặt vé."
                });
            }

            await ExpireOldHoldsAsync(
                trip.Id);

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable);

            try
            {
                var validSeats =
                    await _context.BusSeats
                        .Where(x =>
                            x.BusId ==
                                trip.BusId &&
                            x.IsActive &&
                            seatCodes.Contains(
                                x.SeatCode))
                        .ToListAsync();

                if (validSeats.Count != seatCodes.Count)
                {
                    var foundCodes =
                        validSeats
                            .Select(x => x.SeatCode)
                            .ToHashSet();

                    var invalidSeats =
                        seatCodes
                            .Where(x =>
                                !foundCodes.Contains(x))
                            .ToList();

                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Có ghế không thuộc xe của chuyến.",
                        invalidSeats
                    });
                }

                var now =
                    DateTime.UtcNow;

                var activeHolds =
                    await _context.SeatHolds
                        .Where(x =>
                            x.TripId ==
                                trip.Id &&
                            x.ExpiresAt >
                                now &&
                            x.Status == "HELD" &&
                            seatCodes.Contains(
                                x.SeatCode))
                        .ToListAsync();

                if (activeHolds.Count > 0)
                {
                    var heldSeats =
                        activeHolds
                            .Select(x =>
                                x.SeatCode)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        message =
                            "Một hoặc nhiều ghế vừa được người khác giữ.",
                        heldSeats
                    });
                }

                var activeBookingForUser =
                    await _context.Bookings
                        .Where(x =>
                            x.UserId ==
                                userId &&
                            x.TripId ==
                                trip.Id &&
                            x.Status == "HELD" &&
                            x.ExpiresAt >
                                now)
                        .Include(x =>
                            x.SeatHolds)
                        .FirstOrDefaultAsync();

                if (activeBookingForUser != null)
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        message =
                            "Bạn đang có một đơn giữ chỗ chưa hết hạn cho chuyến này.",
                        bookingId =
                            activeBookingForUser.Id
                    });
                }

                var heldAt =
                    now;

                var expiresAt =
                    heldAt.AddMinutes(10);

                var totalAmount =
                    trip.Route.Fare *
                    seatCodes.Count;

                var booking =
                    new Booking
                    {
                        UserId =
                            userId,

                        TripId =
                            trip.Id,

                        BookingCode =
                            GenerateBookingCode(),

                        Status =
                            "HELD",

                        CreatedAt =
                            heldAt,

                        ExpiresAt =
                            expiresAt,

                        TotalAmount =
                            totalAmount
                    };

                _context.Bookings.Add(
                    booking);

                await _context.SaveChangesAsync();

                foreach (var seatCode in seatCodes)
                {
                    _context.SeatHolds.Add(
                        new SeatHold
                        {
                            BookingId =
                                booking.Id,

                            TripId =
                                trip.Id,

                            SeatCode =
                                seatCode,

                            HeldAt =
                                heldAt,

                            ExpiresAt =
                                expiresAt,

                            Status =
                                "HELD"
                        });
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(
                    new HoldSeatResponseDto
                    {
                        BookingId =
                            booking.Id,

                        BookingCode =
                            booking.BookingCode,

                        TripId =
                            trip.Id,

                        TripCode =
                            trip.TripCode,

                        SeatCodes =
                            seatCodes,

                        TotalAmount =
                            totalAmount,

                        HeldAt =
                            heldAt,

                        ExpiresAt =
                            expiresAt,

                        RemainingSeconds =
                            600,

                        Status =
                            "HELD"
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================
        // GET:
        // api/Booking/{bookingId}
        // =========================================================

        [HttpGet("{bookingId:int}")]
        public async Task<IActionResult>
            GetBooking(int bookingId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var booking =
                await _context.Bookings
                    .AsNoTracking()
                    .Include(x => x.Trip)
                        .ThenInclude(x =>
                            x!.Route)
                    .Include(x =>
                        x.SeatHolds)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                bookingId &&
                            x.UserId ==
                                userId);

            if (booking == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy đơn đặt chỗ."
                });
            }

            if (
                booking.Status == "HELD" &&
                booking.ExpiresAt <=
                    DateTime.UtcNow)
            {
                await ExpireBookingAsync(
                    booking);

                booking.Status =
                    "EXPIRED";
            }

            var routeStops =
    await _context.RouteStops
        .AsNoTracking()
        .Include(x => x.BusStop)
        .Where(
            x =>
                booking.Trip != null &&
                x.RouteId ==
                    booking.Trip.RouteId)
        .OrderBy(
            x =>
                x.StopOrder)
        .ToListAsync();

            var departureStop =
                routeStops.FirstOrDefault();

            var arrivalStop =
                routeStops.LastOrDefault();

            return Ok(new
            {
                bookingId =
                    booking.Id,

                bookingCode =
                    booking.BookingCode,

                tripId =
                    booking.TripId,

                tripCode =
                    booking.Trip?.TripCode,

                routeCode =
                    booking.Trip?.Route?.RouteCode,

                routeName =
                    booking.Trip?.Route?.RouteName,

                // =============================================
                // ĐIỂM ĐI / ĐIỂM ĐẾN
                // =============================================

                startPoint =
                    departureStop?.BusStop?.Name
                    ?? booking.Trip?.Route?.StartPoint
                    ?? "—",

                endPoint =
                    arrivalStop?.BusStop?.Name
                    ?? booking.Trip?.Route?.EndPoint
                    ?? "—",

                startAddress =
                    departureStop?.BusStop?.Address
                    ?? booking.Trip?.Route?.StartPoint
                    ?? "—",

                endAddress =
                    arrivalStop?.BusStop?.Address
                    ?? booking.Trip?.Route?.EndPoint
                    ?? "—",

                // =============================================
                // NGÀY / GIỜ
                // =============================================

                tripDate =
                    booking.Trip?.TripDate,

                departureTime =
                    booking.Trip != null
                        ? booking.Trip.DepartureTime
                            .ToString(@"hh\:mm")
                        : "—",

                arrivalTime =
                    booking.Trip?.ArrivalTime.HasValue == true
                        ? booking.Trip.ArrivalTime.Value
                            .ToString(@"hh\:mm")
                        : "—",

                // =============================================
                // GHẾ
                // =============================================

                seatCodes =
                    booking.SeatHolds
                        .Where(
                            x =>
                                x.Status == "HELD" ||
                                x.Status == "CONFIRMED")
                        .Select(
                            x =>
                                x.SeatCode)
                        .Distinct()
                        .OrderBy(
                            x =>
                                x)
                        .ToList(),

                // =============================================
                // TIỀN
                // =============================================

                totalAmount =
                    booking.TotalAmount,

                // =============================================
                // TRẠNG THÁI
                // =============================================

                status =
                    booking.Status,

                createdAt =
                    booking.CreatedAt,

                expiresAt =
                    booking.ExpiresAt,

                remainingSeconds =
                    Math.Max(
                        0,
                        (int)
                            (
                                booking.ExpiresAt -
                                DateTime.UtcNow
                            ).TotalSeconds
                    )
            });
        }

            // =========================================================
            // POST:
            // api/Booking/{bookingId}/cancel
            // =========================================================

            [HttpPost("{bookingId:int}/cancel")]
        public async Task<IActionResult>
            CancelBooking(int bookingId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var booking =
                await _context.Bookings
                    .Include(x =>
                        x.SeatHolds)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                bookingId &&
                            x.UserId ==
                                userId);

            if (booking == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy đơn đặt chỗ."
                });
            }

            if (booking.Status != "HELD")
            {
                return BadRequest(new
                {
                    message =
                        "Đơn đặt chỗ không còn ở trạng thái giữ chỗ."
                });
            }

            booking.Status =
                "CANCELLED";

            foreach (var hold in booking.SeatHolds)
            {
                if (hold.Status == "HELD")
                {
                    hold.Status =
                        "RELEASED";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã hủy giữ chỗ và giải phóng ghế."
            });
        }

        // =========================================================
        // HỖ TRỢ HẾT HẠN
        // =========================================================

        private async Task ExpireOldHoldsAsync(
            int tripId)
        {
            var now =
                DateTime.UtcNow;

            var expiredHolds =
                await _context.SeatHolds
                    .Where(x =>
                        x.TripId ==
                            tripId &&
                        x.Status == "HELD" &&
                        x.ExpiresAt <=
                            now)
                    .ToListAsync();

            if (expiredHolds.Count == 0)
                return;

            foreach (var hold in expiredHolds)
            {
                hold.Status =
                    "EXPIRED";
            }

            var bookingIds =
                expiredHolds
                    .Select(x =>
                        x.BookingId)
                    .Distinct()
                    .ToList();

            var bookings =
                await _context.Bookings
                    .Where(x =>
                        bookingIds.Contains(x.Id) &&
                        x.Status == "HELD")
                    .ToListAsync();

            foreach (var booking in bookings)
            {
                booking.Status =
                    "EXPIRED";
            }

            await _context.SaveChangesAsync();
        }

        private async Task ExpireBookingAsync(
            Booking booking)
        {
            booking.Status =
                "EXPIRED";

            foreach (var hold in booking.SeatHolds)
            {
                if (hold.Status == "HELD")
                {
                    hold.Status =
                        "EXPIRED";
                }
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateBookingCode()
        {
            return
                $"BK-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 6)
                    .ToUpperInvariant();
        }
    }
}