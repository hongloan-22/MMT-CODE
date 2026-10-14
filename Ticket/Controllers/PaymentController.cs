using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.DTOs;
using Ticket.Data;
using Ticket.Models;
using TicketEntity = Ticket.Models.Ticket;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // TẠO GIAO DỊCH
        // POST: /api/Payment/create
        // =========================================================

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(
    [FromBody] CreatePaymentRequestDto request)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message = "Vui lòng đăng nhập."
                });
            }

            if (request == null ||
                request.BookingId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Thông tin giao dịch không hợp lệ."
                });
            }

            var provider =
                request.Provider?.Trim() ?? "";

            var allowedProviders =
                new[]
                {
            "VNPay",
            "MoMo",
            "ZaloPay",
            "Bank",
            "Cash"
                };

            if (!allowedProviders.Contains(
                    provider,
                    StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message =
                        "Phương thức thanh toán không được hỗ trợ."
                });
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);

            try
            {
                var booking =
                    await _context.Bookings
                        .Include(x => x.Trip)
                            .ThenInclude(x => x!.Route)
                        .Include(x => x.SeatHolds)
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                    request.BookingId &&
                                x.UserId ==
                                    userId);

                if (booking == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy đơn đặt vé."
                    });
                }

                var now =
                    DateTime.UtcNow;

                // ============================================
                // HẾT HẠN GIỮ CHỖ
                // ============================================

                if (booking.Status == "HELD" &&
                    booking.ExpiresAt <= now)
                {
                    booking.Status =
                        "EXPIRED";

                    foreach (var hold in
                             booking.SeatHolds)
                    {
                        if (hold.Status == "HELD")
                        {
                            hold.Status =
                                "EXPIRED";
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return BadRequest(new
                    {
                        message =
                            "Thời gian giữ chỗ đã hết."
                    });
                }

                // ============================================
                // BOOKING PHẢI ĐANG HELD
                // ============================================

                if (booking.Status != "HELD")
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Đơn đặt vé không còn ở trạng thái chờ thanh toán."
                    });
                }

                if (booking.Trip == null ||
                    booking.Trip.Route == null)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Thông tin chuyến xe không hợp lệ."
                    });
                }

                // ============================================
                // KHÔNG CHO THANH TOÁN LẠI BOOKING ĐÃ SUCCESS
                // ============================================

                var existingSuccess =
                    await _context.PaymentTransactions
                        .AnyAsync(
                            x =>
                                x.BookingId ==
                                    booking.Id &&
                                x.Status ==
                                    "SUCCESS");

                if (existingSuccess)
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        message =
                            "Đơn đặt vé đã được thanh toán."
                    });
                }

                // ============================================
                // NẾU ĐÃ CÓ PAYMENT PENDING
                // ============================================

                var pending =
    await _context.PaymentTransactions
        .Where(
            x =>
                x.BookingId ==
                    booking.Id &&
                x.Status ==
                    "PENDING")
        .OrderByDescending(
            x => x.CreatedAt)
        .FirstOrDefaultAsync();

                if (pending != null)
                {
                    // Nếu người dùng chọn lại đúng phương thức
                    // thì dùng lại giao dịch đang Pending.
                    if (string.Equals(
                            pending.Provider,
                            provider,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Ok(
                            ToResponse(
                                pending));
                    }

                    // Nếu đổi sang phương thức khác,
                    // hủy giao dịch Pending cũ.
                    pending.Status =
                        "CANCELLED";

                    await _context.SaveChangesAsync();
                }

                // ============================================
                // KIỂM TRA BOOKING CÓ GHẾ
                // ============================================

                var seatCodes =
                    booking.SeatHolds
                        .Where(
                            x =>
                                x.Status ==
                                    "HELD" &&
                                x.ExpiresAt >
                                    now)
                        .Select(
                            x =>
                                x.SeatCode)
                        .Distinct()
                        .OrderBy(
                            x => x)
                        .ToList();

                if (seatCodes.Count == 0)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Không còn ghế hợp lệ để thanh toán."
                    });
                }

                // ============================================
                // TẠO PAYMENT
                // ============================================

                var payment =
                    new PaymentTransaction
                    {
                        BookingId =
                            booking.Id,

                        Provider =
                            provider,

                        TransactionCode =
                            GenerateTransactionCode(),

                        Amount =
                            booking.TotalAmount,

                        Status =
                            "PENDING",

                        CreatedAt =
                            now
                    };

                _context.PaymentTransactions
                    .Add(payment);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(
                    ToResponse(payment));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================
        // LẤY GIAO DỊCH
        // GET: /api/Payment/{paymentId}
        // =========================================================

        [HttpGet("{paymentId:int}")]
        public async Task<IActionResult> GetPayment(
            int paymentId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message =
                        "Vui lòng đăng nhập."
                });
            }

            var payment =
                await _context.PaymentTransactions
                    .Include(x => x.Booking)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                paymentId &&
                            x.Booking != null &&
                            x.Booking.UserId ==
                                userId);

            if (payment == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy giao dịch."
                });
            }

            return Ok(
                ToResponse(
                    payment));
        }

        // =========================================================
        // XÁC NHẬN THANH TOÁN DEMO
        // POST: /api/Payment/{paymentId}/confirm
        // =========================================================

        [HttpPost("{paymentId:int}/confirm")]
        public async Task<IActionResult> ConfirmPayment(
    int paymentId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message =
                        "Vui lòng đăng nhập."
                });
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);

            try
            {
                // =====================================================
                // LẤY PAYMENT
                // =====================================================

                var payment =
                    await _context.PaymentTransactions
                        .Include(x => x.Booking)
                            .ThenInclude(x => x!.Trip)
                                .ThenInclude(x => x!.Route)

                        .Include(x => x.Booking)
                            .ThenInclude(x => x!.SeatHolds)

                        .FirstOrDefaultAsync(
                            x =>
                                x.Id == paymentId &&
                                x.Booking != null &&
                                x.Booking.UserId ==
                                    userId);

                if (payment == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy giao dịch."
                    });
                }

                // =====================================================
                // NẾU ĐÃ SUCCESS
                // =====================================================

                if (payment.Status == "SUCCESS")
                {
                    await transaction.CommitAsync();

                    return Ok(
                        ToResponse(payment));
                }

                // =====================================================
                // CHỈ PENDING
                // =====================================================

                if (payment.Status != "PENDING")
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Giao dịch không còn ở trạng thái chờ xử lý."
                    });
                }

                var booking =
                    payment.Booking!;

                // =====================================================
                // KIỂM TRA BOOKING
                // =====================================================

                if (booking.Status != "HELD")
                {
                    payment.Status =
                        "CANCELLED";

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return BadRequest(new
                    {
                        message =
                            "Đơn đặt vé không còn hiệu lực."
                    });
                }

                // =====================================================
                // KIỂM TRA 10 PHÚT
                // =====================================================

                var now =
                    DateTime.UtcNow;

                if (booking.ExpiresAt <= now)
                {
                    booking.Status =
                        "EXPIRED";

                    foreach (var hold in
                             booking.SeatHolds)
                    {
                        if (hold.Status == "HELD")
                        {
                            hold.Status =
                                "EXPIRED";
                        }
                    }

                    payment.Status =
                        "CANCELLED";

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return BadRequest(new
                    {
                        message =
                            "Thời gian giữ chỗ đã hết."
                    });
                }

                // =====================================================
                // TRIP / ROUTE
                // =====================================================

                if (booking.Trip == null ||
                    booking.Trip.Route == null)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Không tìm thấy thông tin chuyến xe."
                    });
                }
                // =====================================================
                // KHÔNG CHO THANH TOÁN NẾU CHUYẾN ĐÃ QUÁ GIỜ
                // =====================================================

                var currentLocalTime =
                    DateTime.Now;

                var tripDepartureDateTime =
                    booking.Trip.TripDate.Date
                        .Add(booking.Trip.DepartureTime);

                if (tripDepartureDateTime <= currentLocalTime)
                {
                    booking.Status =
                        "EXPIRED";

                    foreach (var hold in
                             booking.SeatHolds)
                    {
                        if (hold.Status == "HELD")
                        {
                            hold.Status =
                                "EXPIRED";
                        }
                    }

                    payment.Status =
                        "CANCELLED";

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return BadRequest(new
                    {
                        message =
                            $"Chuyến xe đã khởi hành lúc {tripDepartureDateTime:HH:mm dd/MM/yyyy}. Không thể thanh toán và phát hành vé."
                    });
                }
                // =====================================================
                // KIỂM TRA ĐÃ CÓ TICKET
                // =====================================================

                var existingTickets =
                    await _context.Tickets
                        .Where(
                            x =>
                                x.BookingId ==
                                    booking.Id)
                        .ToListAsync();

                if (existingTickets.Count > 0)
                {
                    payment.Status =
                        "SUCCESS";

                    payment.PaidAt ??=
                        now;

                    booking.Status =
                        "CONFIRMED";

                    foreach (var hold in
                             booking.SeatHolds)
                    {
                        if (hold.Status == "HELD")
                        {
                            hold.Status =
                                "CONFIRMED";
                        }
                    }

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(
                        ToResponse(payment));
                }

                // =====================================================
                // LẤY GHẾ ĐANG HELD
                // =====================================================

                var seatCodes =
                    booking.SeatHolds
                        .Where(
                            x =>
                                x.Status == "HELD" &&
                                x.ExpiresAt > now)
                        .Select(
                            x =>
                                x.SeatCode)
                        .Distinct()
                        .OrderBy(
                            x =>
                                x)
                        .ToList();

                if (seatCodes.Count == 0)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Không còn ghế hợp lệ để phát hành vé."
                    });
                }

                // =====================================================
                // KIỂM TRA SỐ GHẾ
                // =====================================================

                if (
                    booking.Trip.BookedSeats +
                    seatCodes.Count >
                    booking.Trip.TotalSeats
                )
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Chuyến xe không còn đủ ghế."
                    });
                }

                // =====================================================
                // LẤY TRẠM TUYẾN
                // =====================================================

                var routeStops =
                    await _context.RouteStops
                        .Where(
                            x =>
                                x.RouteId ==
                                booking.Trip.RouteId)
                        .OrderBy(
                            x =>
                                x.StopOrder)
                        .ToListAsync();

                if (routeStops.Count < 2)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message =
                            "Tuyến chưa có đủ trạm để phát hành vé."
                    });
                }

                var departureStop =
                    routeStops.First();

                var arrivalStop =
                    routeStops.Last();

                // =====================================================
                // PAYMENT SUCCESS
                // =====================================================

                payment.Status =
                    "SUCCESS";

                payment.PaidAt =
                    now;

                booking.Status =
                    "CONFIRMED";

                // =====================================================
                // SEAT HOLD -> CONFIRMED
                // =====================================================

                foreach (var hold in
                         booking.SeatHolds)
                {
                    if (
                        hold.Status == "HELD" &&
                        hold.ExpiresAt > now
                    )
                    {
                        hold.Status =
                            "CONFIRMED";
                    }
                }

                // =====================================================
                // TĂNG BOOKED SEATS
                // =====================================================

                booking.Trip.BookedSeats +=
                    seatCodes.Count;

                booking.Trip.AvailableSeats =
                    Math.Max(
                        0,
                        booking.Trip.TotalSeats -
                        booking.Trip.BookedSeats);

                // =====================================================
                // TẠO TICKET + QR
                // =====================================================

                foreach (var seatCode in
                         seatCodes)
                {
                    var ticket =
                        new TicketEntity
                        {
                            BookingId =
                                booking.Id,

                            TripId =
                                booking.TripId,

                            RouteId =
                                booking.Trip.RouteId,

                            DepartureStopId =
                                departureStop.StopId,

                            ArrivalStopId =
                                arrivalStop.StopId,

                            TicketCode =
                                GenerateTicketCode(),

                            SeatCode =
                                seatCode,

                            Fare =
                                booking.Trip.Route.Fare,

                            Status =
                                "VALID",

                            IssuedAt =
                                now
                        };

                    _context.Tickets.Add(
                        ticket);

                    await _context.SaveChangesAsync();

                    var qr =
                        new TicketQR
                        {
                            TicketId =
                                ticket.Id,

                            QRToken =
                                GenerateQRToken(),

                            Status =
                                "ACTIVE",

                            CreatedAt =
                                now
                        };

                    _context.TicketQRs.Add(
                        qr);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(
                    ToResponse(payment));
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        // =========================================================
        // THANH TOÁN THẤT BẠI DEMO
        // POST: /api/Payment/{paymentId}/fail
        // =========================================================

        [HttpPost("{paymentId:int}/fail")]
        public async Task<IActionResult> FailPayment(
            int paymentId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message =
                        "Vui lòng đăng nhập."
                });
            }

            var payment =
                await _context.PaymentTransactions
                    .Include(x => x.Booking)
                        .ThenInclude(x => x!.SeatHolds)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                paymentId &&
                            x.Booking != null &&
                            x.Booking.UserId ==
                                userId);

            if (payment == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy giao dịch."
                });
            }

            if (payment.Status != "PENDING")
            {
                return BadRequest(new
                {
                    message =
                        "Giao dịch không còn ở trạng thái Pending."
                });
            }

            payment.Status = "FAILED";

            var booking =
                payment.Booking;

            if (booking != null &&
                booking.Status == "HELD")
            {
                booking.Status =
                    "PAYMENT_FAILED";

                foreach (var hold in booking.SeatHolds)
                {
                    if (hold.Status == "HELD")
                    {
                        hold.Status =
                            "RELEASED";
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(
                ToResponse(
                    payment));
        }

        // =========================================================
        // HỦY GIAO DỊCH DEMO
        // POST: /api/Payment/{paymentId}/cancel
        // =========================================================

        [HttpPost("{paymentId:int}/cancel")]
        public async Task<IActionResult> CancelPayment(
            int paymentId)
        {
            var userId =
                Request.Headers["X-User-Id"]
                    .ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message =
                        "Vui lòng đăng nhập."
                });
            }

            var payment =
                await _context.PaymentTransactions
                    .Include(x => x.Booking)
                        .ThenInclude(x => x!.SeatHolds)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                paymentId &&
                            x.Booking != null &&
                            x.Booking.UserId ==
                                userId);

            if (payment == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy giao dịch."
                });
            }

            if (payment.Status != "PENDING")
            {
                return BadRequest(new
                {
                    message =
                        "Giao dịch không còn ở trạng thái Pending."
                });
            }

            payment.Status =
                "CANCELLED";

            var booking =
                payment.Booking!;

            if (booking.Status == "HELD")
            {
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
            }

            await _context.SaveChangesAsync();

            return Ok(
                ToResponse(
                    payment));
        }

        // =========================================================
        // RESPONSE
        // =========================================================

        private static PaymentResponseDto ToResponse(
            PaymentTransaction payment)
        {
            return new PaymentResponseDto
            {
                PaymentId =
                    payment.Id,

                BookingId =
                    payment.BookingId,

                BookingCode =
                    payment.Booking?.BookingCode
                    ?? string.Empty,

                Provider =
                    payment.Provider,

                TransactionCode =
                    payment.TransactionCode,

                Amount =
                    payment.Amount,

                Status =
                    payment.Status,

                CreatedAt =
                    payment.CreatedAt,

                PaidAt =
                    payment.PaidAt
            };
        }

        // =========================================================
        // TRANSACTION CODE
        // =========================================================

        private static string GenerateTransactionCode()
        {
            return
                $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 8)
                    .ToUpperInvariant();
        }

        // =========================================================
        // TICKET CODE
        // =========================================================

        private static string GenerateTicketCode()
        {
            return
                $"TKT-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 8)
                    .ToUpperInvariant();
        }

        // =========================================================
        // QR TOKEN
        // =========================================================

        private static string GenerateQRToken()
        {
            return
                Convert.ToBase64String(
                    Guid.NewGuid()
                        .ToByteArray())
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "");
        }
    }
}