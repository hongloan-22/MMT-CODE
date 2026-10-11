using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Services
{
    /// <summary>
    /// US-109 → US-114: Interface hoàn tiền
    /// Gộp từ: bản cũ của Tùng (CalculateRefundAmount) + workflow đầy đủ
    /// </summary>
    public interface IRefundService
    {
        // ========== US-111: Tính phí hoàn tiền (giữ từ bản cũ) ==========
        /// <summary>
        /// Tính số tiền hoàn dựa trên giá vé + thời gian khởi hành.
        /// - Còn >= 24h: hoàn 100%
        /// - Còn 12-24h: hoàn 50%
        /// - Còn  < 12h: hoàn 0%
        /// </summary>
        decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime);

        // ========== US-109: Tạo yêu cầu hoàn tiền ==========
        Task<RefundTransaction> CreateRefundRequestAsync(int ticketId, string userId, string? reason);

        // ========== US-110: Duyệt / Từ chối ==========
        Task<List<RefundTransaction>> GetPendingRefundsAsync();
        Task<bool> ApproveRefundAsync(int refundId, string adminUserId);
        Task<bool> RejectRefundAsync(int refundId, string adminUserId, string rejectReason);

        // ========== US-111: Thực thi hoàn tiền qua cổng thanh toán ==========
        Task<bool> ExecuteRefundAsync(int refundId);

        // ========== Query helpers ==========
        Task<RefundTransaction?> GetByIdAsync(int refundId);
        Task<List<RefundTransaction>> GetByUserIdAsync(string userId);
        Task<List<RefundTransaction>> GetAllAsync();
    }

    /// <summary>
    /// Service xử lý hoàn tiền (US-109 → US-114)
    /// </summary>
    public class RefundService : IRefundService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RefundService> _logger;

        public RefundService(ApplicationDbContext context, ILogger<RefundService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===================================================================
        // US-111: Tính phí hoàn tiền — GIỮ NGUYÊN logic bản cũ của Tùng
        // ===================================================================
        public decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime)
        {
            var timeUntilDeparture = departureTime - DateTime.Now;

            if (timeUntilDeparture.TotalHours >= 24) return ticketPrice;         // >= 24h: hoàn 100%
            if (timeUntilDeparture.TotalHours >= 12) return ticketPrice * 0.5m;  // 12-24h: hoàn 50%
            return 0;                                                             // < 12h: hoàn 0%
        }

        // ===================================================================
        // US-109: Tạo yêu cầu hoàn tiền
        // ===================================================================
        public async Task<RefundTransaction> CreateRefundRequestAsync(int ticketId, string userId, string? reason)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Schedule).ThenInclude(s => s.Trip)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t => t.Id == ticketId)
                ?? throw new KeyNotFoundException($"Không tìm thấy vé #{ticketId}");

            if (ticket.UserId != userId)
                throw new UnauthorizedAccessException("Vé này không thuộc về bạn");

            var existingRefund = await _context.RefundTransactions
                .FirstOrDefaultAsync(r => r.TicketId == ticketId
                    && (r.Status == RefundStatus.Pending || r.Status == RefundStatus.Approved));

            if (existingRefund != null)
                throw new InvalidOperationException(
                    $"Vé đã có yêu cầu hoàn tiền đang xử lý (#{existingRefund.Id})");

            if (ticket.Status != TicketStatus.Booked)
                throw new InvalidOperationException(
                    $"Chỉ hoàn tiền vé đã đặt. Trạng thái hiện tại: {ticket.Status}");

            // Tính phí theo logic của Tùng
            decimal refundAmount = ticket.Price;
            decimal fee = 0;

            if (ticket.Schedule?.Trip != null)
            {
                var departure = ticket.Schedule.Trip.TripDate.Date
                                + ticket.Schedule.Trip.DepartureTime;

                var realRefund = CalculateRefundAmount(ticket.Price, departure);
                fee = ticket.Price - realRefund;
                refundAmount = realRefund;

                _logger.LogInformation(
                    "[US-111] Tính phí vé #{TicketId}: giá {Price}đ, hoàn {Refund}đ, phí {Fee}đ",
                    ticketId, ticket.Price, refundAmount, fee);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var refund = new RefundTransaction
                {
                    TicketId = ticketId,
                    Amount = refundAmount,
                    Fee = fee,
                    Status = RefundStatus.Pending,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                };
                _context.RefundTransactions.Add(refund);

                ticket.Status = TicketStatus.Cancelled;
                ticket.CancelReason = reason ?? "Yêu cầu hoàn tiền";

                if (ticket.Seat != null)
                    ticket.Seat.Status = SeatStatus.Available;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "[US-109] Tạo refund #{RefundId} cho vé #{TicketId}: hoàn {Amount}đ",
                    refund.Id, ticketId, refundAmount);

                return refund;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ===================================================================
        // US-110: Lấy danh sách chờ duyệt
        // ===================================================================
        public async Task<List<RefundTransaction>> GetPendingRefundsAsync()
        {
            return await _context.RefundTransactions
                .Include(r => r.Ticket).ThenInclude(t => t!.User)   // ✅ FIX CS8602: thêm !
                .Where(r => r.Status == RefundStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        // ===================================================================
        // US-110: Duyệt
        // ===================================================================
        public async Task<bool> ApproveRefundAsync(int refundId, string adminUserId)
        {
            var refund = await _context.RefundTransactions
                .FirstOrDefaultAsync(r => r.Id == refundId)
                ?? throw new KeyNotFoundException($"Không tìm thấy refund #{refundId}");

            if (refund.Status != RefundStatus.Pending)
                throw new InvalidOperationException(
                    $"Refund #{refundId} ở trạng thái {refund.Status}, không thể duyệt");

            refund.Status = RefundStatus.Approved;
            refund.ProcessedByUserId = adminUserId;
            refund.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[US-110] Duyệt refund #{Id} bởi admin {Admin}",
                refundId, adminUserId);

            return await ExecuteRefundAsync(refundId);
        }

        // ===================================================================
        // US-110: Từ chối
        // ===================================================================
        public async Task<bool> RejectRefundAsync(int refundId, string adminUserId, string rejectReason)
        {
            var refund = await _context.RefundTransactions
                .FirstOrDefaultAsync(r => r.Id == refundId)
                ?? throw new KeyNotFoundException($"Không tìm thấy refund #{refundId}");

            if (refund.Status != RefundStatus.Pending)
                throw new InvalidOperationException(
                    $"Refund #{refundId} ở trạng thái {refund.Status}, không thể từ chối");

            refund.Status = RefundStatus.Rejected;
            refund.ProcessedByUserId = adminUserId;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.Reason = $"{refund.Reason} | Admin từ chối: {rejectReason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("[US-110] Từ chối refund #{Id}", refundId);
            return true;
        }

        // ===================================================================
        // US-111: Thực thi hoàn tiền (stub — chờ nối MoMo)
        // ===================================================================
        public async Task<bool> ExecuteRefundAsync(int refundId)
        {
            var refund = await _context.RefundTransactions
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (refund == null) return false;

            if (refund.Status != RefundStatus.Approved)
            {
                _logger.LogWarning("[US-111] Refund #{Id} chưa duyệt, status = {Status}",
                    refundId, refund.Status);
                return false;
            }

            try
            {
                // TODO (Vinh/Lộc): Nối MoMo/VNPay refund API ở đây
                var gatewayTxnRef = $"REFUND-{refund.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var success = true; // stub

                if (success)
                {
                    refund.Status = RefundStatus.Success;
                    refund.GatewayTxnRef = gatewayTxnRef;
                    refund.ProcessedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "[US-111] Hoàn tiền thành công refund #{Id}, mã GD: {Ref}",
                        refundId, gatewayTxnRef);
                }
                else
                {
                    refund.Status = RefundStatus.Failed;
                    refund.ProcessedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[US-111] Lỗi hoàn tiền refund #{Id}", refundId);

                refund.Status = RefundStatus.Failed;
                refund.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return false;
            }
        }

        // ===================================================================
        // Query helpers
        // ===================================================================
        public async Task<RefundTransaction?> GetByIdAsync(int refundId)
        {
            return await _context.RefundTransactions
                .Include(r => r.Ticket)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.Id == refundId);
        }

        public async Task<List<RefundTransaction>> GetByUserIdAsync(string userId)
        {
            return await _context.RefundTransactions
                .Include(r => r.Ticket)
                .Where(r => r.Ticket != null && r.Ticket.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RefundTransaction>> GetAllAsync()
        {
            return await _context.RefundTransactions
                .Include(r => r.Ticket).ThenInclude(t => t!.User)   // ✅ FIX CS8602: thêm !
                .Include(r => r.ProcessedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}