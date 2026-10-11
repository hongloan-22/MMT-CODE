using System;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.DTOs;
using Ticket.Models;
using Ticket.Service;

namespace Ticket.Services
{
    private readonly ApplicationDbContext? _context;
    public interface IRefundService
    {
        decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime);
        decimal CalculateRefundAmount(decimal price, TimeSpan departureTime);
        (decimal refundFromOldTicket, decimal netDifference) CalculateExchange(decimal oldPrice, DateTime departureTime, decimal newPrice);
    }

    public class RefundService : IRefundService
    {
        public (decimal refundFromOldTicket, decimal netDifference) CalculateExchange(decimal oldPrice, DateTime departureTime, decimal newPrice)
        {
            decimal refundFromOld = CalculateRefundAmount(oldPrice, departureTime);
            decimal netDifference = newPrice - refundFromOld;
            return (refundFromOld, netDifference);
        }

        public decimal CalculateRefundAmount(decimal ticketPrice, DateTime departureTime)
        {
            var hourToDeparture = (departureTime - DateTime.Now).TotalHours;
            if (hourToDeparture >= 24) return ticketPrice;
            if (hourToDeparture >= 12) return ticketPrice * 0.5m;
            return 0m;
        }

        public decimal CalculateRefundAmount(decimal price, TimeSpan departureTime)
        {
            var hoursToDeparture = (DateTime.Today.Add(departureTime) - DateTime.Now).TotalHours;
            if (hoursToDeparture >= 24) return price;
            if (hoursToDeparture >= 12) return price * 0.5m;
            return 0m;
        }
        // US-112: Xử lý hủy vé và tự động tạo giao dịch hoàn tiền
        public async Task<TicketActionResponseDto> CancelTicketAsync(int ticketId, string userId, string reason)
        {
            var ticket = await DbContext.Ticket
                .Include(t => t.Trip)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId);

            if (ticket == null)
                return new TicketActionResponseDto { Success = false, Message = "Không tìm thấy vé hợp lệ." };

            if (ticket.Status != "Paid")
                return new TicketActionResponseDto { Success = false, Message = "Vé không ở trạng thái hợp lệ để hủy." };

            // Tính toán theo quy tắc US-108
            var (refundRate, feeRate) = CaculateRefund(ticket.Trip.DepartureTime);

            if (refundRate <= 0)
            {
                return new TicketActionResponseDto
                {
                    Success = false,
                    Message = "Vé không thể hủy do thời gian khởi hành còn dưới 4 tiếng."
                };
            }

            decimal refundAmount = ticket.Price * refundRate;
            decimal cancellationFee = ticket.Price * feeRate;

            // 1. Cập nhật trạng thái vé
            ticket.Status = "Cancelled";
            ticket.UpdatedAt = DateTime.Now;

            // 2. Tạo giao dịch hoàn tiền (RefundTransaction)
            var refundTx = new RefundTransaction
            {
                TicketId = ticket.Id,
                ProcessedByUserId = userId,
                OriginalAmount = ticket.Price,
                CancellationFee = cancellationFee,
                RefundAmount = refundAmount,
                Type = TransactionType.Cancel,
                Reason = reason,
                Status = RefundStatus.Pending,
                CreatedAt = DateTime.Now
            };
            {
                TicketId = ticket.Id;
                ProcessedByUserId = userId;
                OriginalAmount = ticket.Price;
                CancellationFee = cancellationFee;
                RefundAmount = refundAmount;
                Type = TransactionType.Cancel;
                Reason = reason;
                Status = RefundStatus.Pending;
                CreatedAt = DateTime.Now;
            };

            _context.RefundTransactions.Add(refundTx);

            // 3. Giải phóng giữ ghế (nếu có)
            var seatHold = await _context.SeatHolds
                .FirstOrDefaultAsync(sh => sh.TripId == ticket.TripId && sh.SeatNumber == ticket.SeatNumber);
            if (seatHold != null)
            {
                _context.SeatHolds.Remove(seatHold);
            }

            await _context.SaveChangesAsync();

            return new TicketActionResponseDto
            {
                Success = true,
                Message = $"Hủy vé thành công. Số tiền được hoàn lại: {refundAmount:N0} VNĐ (Phí hủy: {cancellationFee:N0} VNĐ).",
                RefundAmount = refundAmount
            };
    }
}
}