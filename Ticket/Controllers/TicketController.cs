using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;
using TicketEntity = Ticket.Models.Ticket;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;
        private readonly IConfiguration _configuration;

        public TicketController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization,
            IConfiguration configuration)
        {
            _context = context;
            _authorization = authorization;
            _configuration = configuration;
        }

        // =====================================================
        // VÉ CỦA HÀNH KHÁCH
        // =====================================================
        [HttpGet("my")]
        public async Task<IActionResult> GetMyTickets()
        {
            var userId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var tickets = await _context.Tickets
                .AsNoTracking()
                .Include(x => x.Booking).ThenInclude(x => x!.User)
                .Include(x => x.Trip)
                .Include(x => x.Route)
                .Include(x => x.DepartureStop)
                .Include(x => x.ArrivalStop)
                .Include(x => x.QR)
                .Where(x => x.Booking != null && x.Booking.UserId == userId)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync();

            return Ok(tickets.Select(ToDto));
        }

        [HttpGet("booking/{bookingId:int}")]
        public async Task<IActionResult> GetTicketsByBooking(int bookingId)
        {
            var userId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var tickets = await _context.Tickets
                .AsNoTracking()
                .Include(x => x.Booking).ThenInclude(x => x!.User)
                .Include(x => x.Trip)
                .Include(x => x.Route)
                .Include(x => x.DepartureStop)
                .Include(x => x.ArrivalStop)
                .Include(x => x.QR)
                .Where(x => x.Booking != null && x.Booking.Id == bookingId && x.Booking.UserId == userId)
                .OrderBy(x => x.SeatCode)
                .ToListAsync();

            if (tickets.Count == 0) return NotFound(new { message = "Booking chưa có vé điện tử." });
            return Ok(tickets.Select(ToDto).ToList());
        }

        // =====================================================
        // LỊCH SỬ MUA VÉ DÀNH CHO ADMIN / MANAGER
        // GET: /api/Ticket/history
        // =====================================================
        [HttpGet("history")]
        public async Task<IActionResult> GetTicketHistory(
            [FromHeader(Name = "X-User-Id")] string? userId,
            [FromQuery] string? search = null,
            [FromQuery] string? ticketStatus = null,
            [FromQuery] string? paymentStatus = null,
            [FromQuery] string? refundStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập." });

            if (!await _authorization.HasAnyRoleAsync(Request, "ADMIN", "MANAGER"))
                return StatusCode(403, new { message = "Bạn không có quyền xem lịch sử mua vé." });

            page = Math.Max(1, page);
            if (pageSize < 1) pageSize = 20;
            pageSize = Math.Min(pageSize, 100);

            var keyword = search?.Trim().ToLowerInvariant();
            var normalizedTicketStatus = ticketStatus?.Trim().ToUpperInvariant();
            var normalizedPaymentStatus = paymentStatus?.Trim().ToUpperInvariant();
            var normalizedRefundStatus = refundStatus?.Trim().ToUpperInvariant();

            var query = _context.Tickets.AsNoTracking().Where(t => t.Booking != null);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t =>
                    t.TicketCode.ToLower().Contains(keyword) ||
                    t.Booking!.BookingCode.ToLower().Contains(keyword) ||
                    (t.Booking.User != null && t.Booking.User.FullName.ToLower().Contains(keyword)) ||
                    (t.Trip != null && t.Trip.TripCode.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(normalizedTicketStatus) && normalizedTicketStatus != "ALL")
                query = query.Where(t => t.Status == normalizedTicketStatus);

            if (!string.IsNullOrWhiteSpace(normalizedPaymentStatus) && normalizedPaymentStatus != "ALL")
            {
                if (normalizedPaymentStatus == "NONE" || normalizedPaymentStatus == "NO_TRANSACTION")
                {
                    query = query.Where(t => !_context.PaymentTransactions.Any(p => p.BookingId == t.BookingId));
                }
                else
                {
                    query = query.Where(t =>
                        _context.PaymentTransactions
                            .Where(p => p.BookingId == t.BookingId)
                            .OrderByDescending(p => p.CreatedAt)
                            .ThenByDescending(p => p.Id)
                            .Select(p => p.Status)
                            .FirstOrDefault() == normalizedPaymentStatus);
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedRefundStatus) && normalizedRefundStatus != "ALL")
            {
                if (normalizedRefundStatus == "NONE" || normalizedRefundStatus == "NO_TRANSACTION")
                {
                    query = query.Where(t => !_context.RefundTransactions.Any(r => r.TicketId == t.Id));
                }
                else
                {
                    query = query.Where(t =>
                        _context.RefundTransactions
                            .Where(r => r.TicketId == t.Id)
                            .OrderByDescending(r => r.CreatedAt)
                            .ThenByDescending(r => r.Id)
                            .Select(r => r.Status)
                            .FirstOrDefault() == normalizedRefundStatus);
                }
            }

            var totalCount = await query.CountAsync();

            var tickets = await query
                .Include(t => t.Booking).ThenInclude(b => b!.User)
                .Include(t => t.Trip)
                .Include(t => t.Route)
                .Include(t => t.DepartureStop)
                .Include(t => t.ArrivalStop)
                .OrderByDescending(t => t.IssuedAt)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var bookingIds = tickets.Select(t => t.BookingId).Distinct().ToList();
            var ticketIds = tickets.Select(t => t.Id).ToList();

            var paymentRows = bookingIds.Count == 0 ? new List<PaymentTransaction>() :
                await _context.PaymentTransactions.AsNoTracking()
                    .Where(p => bookingIds.Contains(p.BookingId))
                    .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
                    .ToListAsync();

            var refundRows = ticketIds.Count == 0 ? new List<RefundTransaction>() :
                await _context.RefundTransactions.AsNoTracking()
                    .Where(r => ticketIds.Contains(r.TicketId))
                    .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                    .ToListAsync();

            var latestPaymentByBooking = paymentRows.GroupBy(p => p.BookingId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).First());

            var latestRefundByTicket = refundRows.GroupBy(r => r.TicketId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).First());

            var items = tickets.Select(t =>
            {
                latestPaymentByBooking.TryGetValue(t.BookingId, out var payment);
                latestRefundByTicket.TryGetValue(t.Id, out var refund);

                return new TicketHistoryItemDto
                {
                    TicketId = t.Id,
                    TicketCode = t.TicketCode,
                    BookingCode = t.Booking?.BookingCode ?? string.Empty,
                    BookingStatus = t.Booking?.Status ?? string.Empty,
                    PassengerName = t.Booking?.User?.FullName ?? "Chưa cập nhật",
                    TripCode = t.Trip?.TripCode ?? string.Empty,
                    RouteCode = t.Route?.RouteCode ?? string.Empty,
                    RouteName = t.Route?.RouteName ?? string.Empty,
                    TripDate = t.Trip?.TripDate,
                    DepartureTime = t.Trip != null ? t.Trip.DepartureTime.ToString(@"hh\:mm") : string.Empty,
                    ArrivalTime = t.Trip != null && t.Trip.ArrivalTime.HasValue ? t.Trip.ArrivalTime.Value.ToString(@"hh\:mm") : string.Empty,
                    DepartureStop = t.DepartureStop?.Name ?? string.Empty,
                    ArrivalStop = t.ArrivalStop?.Name ?? string.Empty,
                    SeatCode = t.SeatCode,
                    Fare = t.Fare,
                    TicketStatus = t.Status,
                    IssuedAt = t.IssuedAt,
                    UsedAt = t.UsedAt,
                    PaymentStatus = payment?.Status ?? "NO_TRANSACTION",
                    PaymentProvider = payment?.Provider ?? string.Empty,
                    PaymentTransactionCode = payment?.TransactionCode ?? string.Empty,
                    PaymentAmount = payment?.Amount,
                    PaidAt = payment?.PaidAt,
                    RefundStatus = refund?.Status ?? "NO_TRANSACTION",
                    RefundReason = refund?.Reason ?? string.Empty,
                    RefundAmount = refund?.Amount,
                    RefundedAt = refund?.ProcessedAt
                };
            }).ToList();

            return Ok(new TicketHistoryResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = items
            });
        }

        // =====================================================
        // CHI TIẾT VÉ
        // =====================================================
        [HttpGet("{ticketId:int}")]
        public async Task<IActionResult> GetTicket(int ticketId)
        {
            var userId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var ticket = await _context.Tickets
                .AsNoTracking()
                .Include(x => x.Booking).ThenInclude(x => x!.User)
                .Include(x => x.Trip).Include(x => x.Route)
                .Include(x => x.DepartureStop).Include(x => x.ArrivalStop).Include(x => x.QR)
                .FirstOrDefaultAsync(x => x.Id == ticketId && x.Booking != null && x.Booking.UserId == userId);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy vé." });
            return Ok(ToDto(ticket));
        }

        // =====================================================
        // HỦY VÉ
        // =====================================================
        [HttpGet("{ticketId:int}/cancel-info")]
        public async Task<IActionResult> GetCancelInfo(int ticketId)
        {
            var userId = Request.Headers["X-User-Id"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { canCancel = false, message = "Vui lòng đăng nhập." });

            var ticket = await _context.Tickets.AsNoTracking()
                .Include(x => x.Booking).Include(x => x.Trip)
                .FirstOrDefaultAsync(x => x.Id == ticketId && x.Booking != null && x.Booking.UserId == userId);

            if (ticket == null) return NotFound(new { canCancel = false, message = "Không tìm thấy vé." });
            if (ticket.Status != "VALID") return Ok(new { canCancel = false, message = "Vé không còn ở trạng thái có thể hủy." });
            if (ticket.Trip == null) return Ok(new { canCancel = false, message = "Không tìm thấy chuyến xe." });

            var cutoffMinutes = _configuration.GetValue<int?>("TicketPolicy:CancelCutoffMinutes") ?? 30;
            var departureAt = ticket.Trip.TripDate.Date.Add(ticket.Trip.DepartureTime);
            var cutoffAt = departureAt.AddMinutes(-cutoffMinutes);

            if (DateTime.Now >= cutoffAt)
                return Ok(new { canCancel = false, message = $"Vé phải được hủy trước giờ khởi hành ít nhất {cutoffMinutes} phút." });

            return Ok(new { canCancel = true, message = "Vé đủ điều kiện hủy.", departureAt, cutoffAt });
        }

        [HttpPost("{ticketId:int}/cancel")]
        public async Task<IActionResult> CancelTicket(int ticketId)
        {
            var userId = Request.Headers["X-User-Id"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = await _context.Tickets
                    .Include(x => x.Booking).ThenInclude(x => x!.SeatHolds)
                    .Include(x => x.Trip)
                    .FirstOrDefaultAsync(x => x.Id == ticketId && x.Booking != null && x.Booking.UserId == userId);

                if (ticket == null) return NotFound(new { success = false, message = "Không tìm thấy vé." });
                if (ticket.Status == "CANCELLED" || ticket.Status == "USED" || ticket.Status != "VALID")
                    return BadRequest(new { success = false, message = "Vé không ở trạng thái có thể hủy." });
                if (ticket.Trip == null || ticket.Booking == null)
                    return BadRequest(new { success = false, message = "Dữ liệu vé không đầy đủ." });

                var cutoffMinutes = _configuration.GetValue<int?>("TicketPolicy:CancelCutoffMinutes") ?? 30;
                var departureAt = ticket.Trip.TripDate.Date.Add(ticket.Trip.DepartureTime);
                var cutoffAt = departureAt.AddMinutes(-cutoffMinutes);

                if (DateTime.Now >= cutoffAt)
                    return BadRequest(new { success = false, message = $"Vé phải được hủy trước giờ khởi hành ít nhất {cutoffMinutes} phút." });

                ticket.Status = "CANCELLED";

                var seatHold = ticket.Booking.SeatHolds.FirstOrDefault(x => x.TripId == ticket.TripId && x.SeatCode == ticket.SeatCode && x.Status == "CONFIRMED");
                if (seatHold != null) seatHold.Status = "RELEASED";

                ticket.Trip.BookedSeats = Math.Max(0, ticket.Trip.BookedSeats - 1);
                ticket.Trip.AvailableSeats = Math.Min(ticket.Trip.TotalSeats, ticket.Trip.AvailableSeats + 1);

                var otherTickets = await _context.Tickets.Where(x => x.BookingId == ticket.BookingId && x.Id != ticket.Id).ToListAsync();
                ticket.Booking.Status = otherTickets.Any(x => x.Status == "VALID") ? "PARTIALLY_CANCELLED" : "CANCELLED";

                // Lấy giao dịch thanh toán thành công mới nhất
                var payment = await _context.PaymentTransactions
                    .Where(x => x.BookingId == ticket.BookingId && x.Status == "SUCCESS")
                    .OrderByDescending(x => x.PaidAt ?? x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                RefundTransaction? refund = null;
                var isCashPayment = payment != null && string.Equals(payment.Provider?.Trim(), "Cash", StringComparison.OrdinalIgnoreCase);

                if (payment != null && !isCashPayment && ticket.Fare > 0)
                {
                    refund = await _context.RefundTransactions
                        .FirstOrDefaultAsync(x => x.TicketId == ticket.Id && (x.Status == "PENDING" || x.Status == "SUCCESS"));

                    if (refund == null)
                    {
                        refund = new RefundTransaction
                        {
                            TicketId = ticket.Id,
                            PaymentTransactionId = payment.Id,
                            Amount = ticket.Fare,
                            Reason = "CANCEL_TICKET",
                            Status = "PENDING",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.RefundTransactions.Add(refund);
                    }
                }

                _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = $"Hủy vé {ticket.TicketCode}", IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CreatedAt = DateTime.UtcNow });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Hủy vé thành công.", ticketId = ticket.Id, refundId = refund?.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // ĐỔI VÉ
        // =====================================================
        [HttpPost("{ticketId:int}/change")]
        public async Task<IActionResult> ChangeTicket(int ticketId, [FromBody] ChangeTicketRequestDto request)
        {
            var userId = Request.Headers["X-User-Id"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (request == null || request.NewTripId <= 0 || string.IsNullOrWhiteSpace(request.NewSeatCode))
                return BadRequest(new { success = false, message = "Thông tin đổi vé không hợp lệ." });

            var newSeatCode = request.NewSeatCode.Trim().ToUpperInvariant();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = await _context.Tickets
                    .Include(x => x.Booking).ThenInclude(x => x!.SeatHolds)
                    .Include(x => x.Trip).Include(x => x.Route)
                    .FirstOrDefaultAsync(x => x.Id == ticketId && x.Booking != null && x.Booking.UserId == userId);

                if (ticket == null) return NotFound(new { success = false, message = "Không tìm thấy vé." });
                if (ticket.Status != "VALID") return BadRequest(new { success = false, message = "Chỉ vé còn hiệu lực mới được đổi." });
                if (ticket.Trip == null || ticket.Booking == null || ticket.Route == null) return BadRequest(new { success = false, message = "Dữ liệu vé không đầy đủ." });

                var activeOtherTickets = await _context.Tickets.CountAsync(x => x.BookingId == ticket.BookingId && x.Id != ticket.Id && x.Status == "VALID");
                if (activeOtherTickets > 0) return Conflict(new { success = false, message = "Booking có nhiều vé đang hiệu lực. Hiện tại chức năng đổi vé áp dụng cho booking một vé." });

                var cutoffMinutes = _configuration.GetValue<int?>("TicketPolicy:CancelCutoffMinutes") ?? 30;
                var oldDepartureAt = ticket.Trip.TripDate.Date.Add(ticket.Trip.DepartureTime);

                if (DateTime.Now >= oldDepartureAt.AddMinutes(-cutoffMinutes))
                    return BadRequest(new { success = false, message = $"Vé phải được đổi trước giờ khởi hành ít nhất {cutoffMinutes} phút." });

                var newTrip = await _context.Trips.Include(x => x.Route).Include(x => x.Bus)
                    .FirstOrDefaultAsync(x => x.Id == request.NewTripId && x.RouteId == ticket.RouteId && x.BusId != null && x.IsActive && x.Status != "CANCELLED");

                if (newTrip == null) return NotFound(new { success = false, message = "Không tìm thấy chuyến mới hợp lệ." });

                var newDepartureAt = newTrip.TripDate.Date.Add(newTrip.DepartureTime);
                if (newDepartureAt <= DateTime.Now.AddMinutes(cutoffMinutes))
                    return BadRequest(new { success = false, message = $"Chuyến mới phải còn ít nhất {cutoffMinutes} phút trước giờ khởi hành." });

                if (newTrip.AvailableSeats <= 0) return BadRequest(new { success = false, message = "Chuyến mới đã hết chỗ." });

                var validSeat = await _context.BusSeats.AnyAsync(x => x.BusId == newTrip.BusId && x.SeatCode == newSeatCode && x.IsActive);
                if (!validSeat) return BadRequest(new { success = false, message = "Ghế không thuộc xe của chuyến mới." });

                var seatBusy = await _context.SeatHolds.AnyAsync(x => x.TripId == newTrip.Id && x.SeatCode == newSeatCode && (x.Status == "CONFIRMED" || (x.Status == "HELD" && x.ExpiresAt > DateTime.UtcNow)));
                if (seatBusy) return Conflict(new { success = false, message = "Ghế đã được giữ hoặc đặt. Vui lòng chọn ghế khác." });

                var newFare = newTrip.Route?.Fare ?? ticket.Fare;
                var difference = newFare - ticket.Fare;
                var oldTripId = ticket.TripId;
                var oldSeatCode = ticket.SeatCode;
                var oldFare = ticket.Fare;

                // Nếu vé mới rẻ hơn, chỉ tiếp tục khi có thanh toán gốc thành công và không dùng tiền mặt.
                PaymentTransaction? originalPayment = null;

                if (difference < 0)
                {
                    originalPayment = await _context.PaymentTransactions
                        .Where(x => x.BookingId == ticket.BookingId && x.Status == "SUCCESS")
                        .OrderByDescending(x => x.PaidAt ?? x.CreatedAt)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                    if (originalPayment == null)
                        return BadRequest(new { success = false, message = "Không thể đổi sang vé rẻ hơn vì không tìm thấy giao dịch thanh toán thành công để hoàn chênh lệch." });

                    if (string.Equals(originalPayment.Provider?.Trim(), "Cash", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new { success = false, message = "Không hỗ trợ hoàn chênh lệch khi vé được thanh toán bằng tiền mặt. Vui lòng chọn chuyến có giá bằng hoặc cao hơn." });
                }

                // GHẾ CŨ
                var oldHold = ticket.Booking.SeatHolds.FirstOrDefault(x => x.TripId == oldTripId && x.SeatCode == oldSeatCode && x.Status == "CONFIRMED");
                if (oldHold != null)
                {
                    oldHold.TripId = newTrip.Id;
                    oldHold.SeatCode = newSeatCode;
                    oldHold.HeldAt = DateTime.UtcNow;
                    oldHold.ExpiresAt = DateTime.UtcNow;
                    oldHold.Status = "CONFIRMED";
                }
                else
                {
                    _context.SeatHolds.Add(new SeatHold { BookingId = ticket.BookingId, TripId = newTrip.Id, SeatCode = newSeatCode, HeldAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow, Status = "CONFIRMED" });
                }

                ticket.Trip.BookedSeats = Math.Max(0, ticket.Trip.BookedSeats - 1);
                ticket.Trip.AvailableSeats = Math.Min(ticket.Trip.TotalSeats, ticket.Trip.AvailableSeats + 1);

                newTrip.BookedSeats++;
                newTrip.AvailableSeats = Math.Max(0, newTrip.TotalSeats - newTrip.BookedSeats);

                ticket.TripId = newTrip.Id;
                ticket.RouteId = newTrip.RouteId;
                ticket.SeatCode = newSeatCode;
                ticket.Fare = newFare;

                var routeStops = await _context.RouteStops.Include(x => x.BusStop).Where(x => x.RouteId == newTrip.RouteId).OrderBy(x => x.StopOrder).ToListAsync();
                if (routeStops.Count >= 2)
                {
                    ticket.DepartureStopId = routeStops.First().StopId;
                    ticket.ArrivalStopId = routeStops.Last().StopId;
                }

                ticket.Booking.TripId = newTrip.Id;
                ticket.Booking.TotalAmount = newFare;
                ticket.Booking.Status = "CONFIRMED";

                _context.TicketChangeHistories.Add(new TicketChangeHistory
                {
                    TicketId = ticket.Id,
                    OldTripId = oldTripId,
                    NewTripId = newTrip.Id,
                    OldSeatCode = oldSeatCode,
                    NewSeatCode = newSeatCode,
                    OldFare = oldFare,
                    NewFare = newFare,
                    Difference = difference,
                    Status = "COMPLETED",
                    Reason = "Đổi chuyến/ghế",
                    CreatedAt = DateTime.UtcNow
                });

                RefundTransaction? refund = null;
                if (difference < 0 && originalPayment != null)
                {
                    refund = new RefundTransaction
                    {
                        TicketId = ticket.Id,
                        PaymentTransactionId = originalPayment.Id,
                        Amount = Math.Abs(difference),
                        Reason = "CHANGE_TICKET",
                        Status = "PENDING",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.RefundTransactions.Add(refund);
                }

                if (difference > 0)
                {
                    _context.PaymentTransactions.Add(new PaymentTransaction { BookingId = ticket.BookingId, Provider = "DEMO_CHANGE", TransactionCode = GenerateTransactionCode(), Amount = difference, Status = "SUCCESS", CreatedAt = DateTime.UtcNow, PaidAt = DateTime.UtcNow });
                }

                _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = $"Đổi vé {ticket.TicketCode}: {oldTripId}/{oldSeatCode} -> {newTrip.Id}/{newSeatCode}", IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CreatedAt = DateTime.UtcNow });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Đổi vé thành công.", ticketId = ticket.Id, oldTripId, newTripId = newTrip.Id, oldSeatCode, newSeatCode, oldFare, newFare, difference, refundId = refund?.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet("{ticketId:int}/change-options")]
        public async Task<IActionResult> GetChangeOptions(int ticketId)
        {
            var userId = Request.Headers["X-User-Id"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var ticket = await _context.Tickets.AsNoTracking().Include(x => x.Booking).Include(x => x.Trip).FirstOrDefaultAsync(x => x.Id == ticketId && x.Booking != null && x.Booking.UserId == userId);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy vé." });
            if (ticket.Status != "VALID") return BadRequest(new { message = "Chỉ vé còn hiệu lực mới được đổi." });
            if (ticket.Trip == null) return BadRequest(new { message = "Không tìm thấy chuyến xe hiện tại." });

            var cutoffMinutes = _configuration.GetValue<int?>("TicketPolicy:CancelCutoffMinutes") ?? 30;
            var oldDepartureAt = ticket.Trip.TripDate.Date.Add(ticket.Trip.DepartureTime);

            if (DateTime.Now >= oldDepartureAt.AddMinutes(-cutoffMinutes))
                return BadRequest(new { message = $"Vé phải được đổi trước giờ khởi hành ít nhất {cutoffMinutes} phút." });

            var minimumNewDepartureAt = DateTime.Now.AddMinutes(cutoffMinutes);

            var trips = await _context.Trips.AsNoTracking().Include(x => x.Route)
                .Where(x => x.Id != ticket.TripId && x.RouteId == ticket.RouteId && x.BusId != null && x.IsActive && x.Status != "CANCELLED" && x.AvailableSeats > 0 && x.TripDate.Date.Add(x.DepartureTime) > minimumNewDepartureAt)
                .OrderBy(x => x.TripDate).ThenBy(x => x.DepartureTime).Take(30)
                .Select(x => new { tripId = x.Id, tripCode = x.TripCode, tripDate = x.TripDate.ToString("yyyy-MM-dd"), departureTime = x.DepartureTime.ToString(@"hh\:mm"), availableSeats = x.AvailableSeats, fare = x.Route != null ? x.Route.Fare : 0, routeName = x.Route != null ? x.Route.RouteName : "" })
                .ToListAsync();

            return Ok(new { oldFare = ticket.Fare, trips });
        }

        // =====================================================
        // QR & SOÁT VÉ
        // =====================================================
        [HttpPost("verify-qr")]
        public async Task<IActionResult> VerifyQR([FromBody] VerifyTicketQRRequestDto request)
        {
            if (!await _authorization.HasAnyRoleAsync(Request, "DRIVER", "MANAGER")) return StatusCode(403, new { valid = false, message = "Bạn không có quyền kiểm tra vé." });
            if (request == null || string.IsNullOrWhiteSpace(request.QRToken)) return BadRequest(new { valid = false, message = "Mã QR không hợp lệ." });
            if (request.TripId <= 0) return BadRequest(new { valid = false, message = "Chưa xác định được chuyến xe." });

            var token = request.QRToken.Trim();

            var ticket = await _context.Tickets.Include(x => x.Booking).ThenInclude(x => x!.User).Include(x => x.Trip).Include(x => x.Route).Include(x => x.DepartureStop).Include(x => x.ArrivalStop).Include(x => x.QR).FirstOrDefaultAsync(x => x.QR != null && x.QR.QRToken == token);

            if (ticket != null)
            {
                var driverUserId = Request.Headers["X-User-Id"].ToString().Trim();
                var driver = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == driverUserId && x.Status == "ACTIVE" && x.IsActive);
                var driverRole = driver?.Role?.RoleCode?.Trim().ToUpperInvariant();

                if (driverRole == "DRIVER")
                {
                    var assigned = ticket.Trip != null && ticket.Trip.BusId != null && await _context.BusStaffAssignments.AnyAsync(x => x.BusId == ticket.Trip.BusId && x.UserId == driverUserId && x.AssignmentType == "DRIVER" && x.IsActive);
                    if (!assigned) return StatusCode(403, new { valid = false, message = "Bạn không được phân công cho xe của chuyến này." });
                }
            }

            if (ticket == null) return NotFound(new { valid = false, message = "QR không tồn tại trong hệ thống." });
            if (ticket.QR!.Status != "ACTIVE") return BadRequest(new { valid = false, message = "Mã QR không còn hiệu lực." });
            if (ticket.Status == "CANCELLED") return BadRequest(new { valid = false, message = "Vé đã bị hủy." });
            if (ticket.Status == "USED") return BadRequest(new { valid = false, message = "Vé đã được sử dụng." });
            if (ticket.Status != "VALID") return BadRequest(new { valid = false, message = "Vé không hợp lệ." });
            if (ticket.Trip == null) return BadRequest(new { valid = false, message = "Không tìm thấy chuyến xe của vé." });

            if (!ticket.Trip.IsActive || string.Equals(ticket.Trip.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { valid = false, message = "Chuyến xe đã ngừng hoạt động hoặc bị hủy. Không thể soát vé." });

            if (ticket.TripId != request.TripId) return BadRequest(new { valid = false, message = "Vé không thuộc chuyến xe đang kiểm tra." });
            if (ticket.Trip.TripDate.Date != DateTime.Today) return BadRequest(new { valid = false, message = "Vé không thuộc ngày chạy hiện tại." });

            return Ok(new { valid = true, message = "Vé hợp lệ.", ticket = ToDto(ticket) });
        }

        [HttpPost("{ticketId:int}/use")]
        public async Task<IActionResult> UseTicket(int ticketId, [FromQuery] int tripId, [FromBody] UseTicketRequestDto? request)
        {
            if (!await _authorization.HasAnyRoleAsync(Request, "DRIVER", "MANAGER")) return StatusCode(403, new { valid = false, message = "Bạn không có quyền xác nhận vé." });
            if (tripId <= 0) return BadRequest(new { valid = false, message = "Chưa xác định được chuyến xe." });

            if (request == null || string.IsNullOrWhiteSpace(request.QRToken))
                return BadRequest(new { valid = false, message = "Thiếu mã QR đã xác minh. Vui lòng quét hoặc nhập lại mã QR." });

            var ticket = await _context.Tickets.Include(x => x.QR).Include(x => x.Trip).FirstOrDefaultAsync(x => x.Id == ticketId);
            if (ticket == null) return NotFound(new { valid = false, message = "Không tìm thấy vé." });

            var useUserId = Request.Headers["X-User-Id"].ToString().Trim();
            var useUser = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == useUserId && x.Status == "ACTIVE" && x.IsActive);
            var useRole = useUser?.Role?.RoleCode?.Trim().ToUpperInvariant();

            if (useRole == "DRIVER")
            {
                if (ticket.Trip?.BusId == null) return StatusCode(403, new { valid = false, message = "Chuyến xe này chưa được gán xe." });
                var isAssigned = await _context.BusStaffAssignments.AnyAsync(x => x.BusId == ticket.Trip.BusId && x.UserId == useUserId && x.AssignmentType == "DRIVER" && x.IsActive);
                if (!isAssigned) return StatusCode(403, new { valid = false, message = "Bạn không được phân công cho xe của vé này." });
            }

            if (ticket.TripId != tripId) return BadRequest(new { valid = false, message = "Vé không thuộc chuyến đang kiểm tra." });
            if (ticket.Trip == null) return BadRequest(new { valid = false, message = "Không tìm thấy chuyến xe." });

            if (!ticket.Trip.IsActive || string.Equals(ticket.Trip.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { valid = false, message = "Chuyến xe đã ngừng hoạt động hoặc bị hủy. Không thể xác nhận vé." });

            if (ticket.Trip.TripDate.Date != DateTime.Today) return BadRequest(new { valid = false, message = "Vé không thuộc ngày chạy hiện tại." });
            if (ticket.Status == "CANCELLED") return BadRequest(new { valid = false, message = "Vé đã bị hủy." });
            if (ticket.Status == "USED") return BadRequest(new { valid = false, message = "Vé đã được sử dụng trước đó." });
            if (ticket.Status != "VALID") return BadRequest(new { valid = false, message = "Vé không hợp lệ." });
            if (ticket.QR == null) return BadRequest(new { valid = false, message = "Vé chưa có mã QR." });

            if (!string.Equals(ticket.QR.QRToken, request.QRToken.Trim(), StringComparison.Ordinal))
                return BadRequest(new { valid = false, message = "Mã QR không khớp với vé cần xác nhận. Hãy quét lại mã QR." });

            if (ticket.QR.Status != "ACTIVE") return BadRequest(new { valid = false, message = "QR của vé không còn hiệu lực." });

            ticket.Status = "USED";
            ticket.UsedAt = DateTime.UtcNow;
            ticket.QR.Status = "USED";
            await _context.SaveChangesAsync();

            return Ok(new { valid = true, message = "Đã xác nhận vé đã sử dụng.", ticketCode = ticket.TicketCode, seatCode = ticket.SeatCode, usedAt = ticket.UsedAt });
        }

        [HttpGet("today-trips")]
        public async Task<IActionResult> GetTodayTrips()
        {
            if (!await _authorization.HasAnyRoleAsync(Request, "DRIVER", "MANAGER")) return StatusCode(403, new { message = "Bạn không có quyền xem chuyến." });
            var today = DateTime.Today;
            var currentUserId = Request.Headers["X-User-Id"].ToString().Trim();
            var currentUser = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == currentUserId && x.Status == "ACTIVE" && x.IsActive);
            if (currentUser == null) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var currentRole = currentUser.Role?.RoleCode?.Trim().ToUpperInvariant() ?? "";
            var tripsQuery = _context.Trips.AsNoTracking().Include(x => x.Route).Include(x => x.Bus).Where(x => x.TripDate.Date == today && x.IsActive && x.Status != "CANCELLED");

            if (currentRole == "DRIVER")
            {
                tripsQuery = tripsQuery.Where(x => x.BusId != null && _context.BusStaffAssignments.Any(a => a.BusId == x.BusId && a.UserId == currentUserId && a.AssignmentType == "DRIVER" && a.IsActive));
            }

            var trips = await tripsQuery.OrderBy(x => x.DepartureTime).ToListAsync();
            var result = trips.Select(x => new { tripId = x.Id, tripCode = x.TripCode, routeCode = x.Route?.RouteCode ?? "", routeName = x.Route?.RouteName ?? "", departureTime = x.DepartureTime.ToString(@"hh\:mm"), arrivalTime = x.ArrivalTime.HasValue ? x.ArrivalTime.Value.ToString(@"hh\:mm") : "", busCode = x.Bus?.BusCode ?? "", licensePlate = x.Bus?.LicensePlate ?? "" });
            return Ok(result);
        }

        private static TicketResponseDto ToDto(TicketEntity ticket)
        {
            return new TicketResponseDto
            {
                TicketId = ticket.Id,
                TicketCode = ticket.TicketCode,
                BookingCode = ticket.Booking?.BookingCode ?? string.Empty,
                TripCode = ticket.Trip?.TripCode ?? string.Empty,
                RouteCode = ticket.Route?.RouteCode ?? string.Empty,
                RouteName = ticket.Route?.RouteName ?? string.Empty,
                PassengerName = ticket.Booking?.User?.FullName ?? string.Empty,
                DepartureStop = ticket.DepartureStop?.Name ?? string.Empty,
                ArrivalStop = ticket.ArrivalStop?.Name ?? string.Empty,
                TripDate = ticket.Trip?.TripDate ?? DateTime.MinValue,
                DepartureTime = ticket.Trip != null ? ticket.Trip.DepartureTime.ToString(@"hh\:mm") : string.Empty,
                ArrivalTime = ticket.Trip?.ArrivalTime.HasValue == true ? ticket.Trip.ArrivalTime.Value.ToString(@"hh\:mm") : string.Empty,
                SeatCode = ticket.SeatCode,
                Fare = ticket.Fare,
                Status = ticket.Status,
                IssuedAt = ticket.IssuedAt,
                UsedAt = ticket.UsedAt,
                QRToken = ticket.QR?.QRToken ?? string.Empty
            };
        }

        private static string GenerateTransactionCode()
        {
            return $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}";
        }
    }
}