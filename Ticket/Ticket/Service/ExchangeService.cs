using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.DTOs;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.Service
{
    public class TicketExchangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.RefundService _refundService;

        public TicketExchangeService(ApplicationDbContext context, RefundService refundService)
        {
            _context = context;
            _refundService = refundService;
        }

        public async Task<TicketActionResponseDto> ExchangeTicketAsync(ExchangeTicketDto request, string userId)
        {
            var oldTicket = await _context.Tickets
                .Include(t => t.Trip)
                .FirstOrDefaultAsync(t => t.Id == request.OldTicketId && t.UserId == userId);

            if (oldTicket == null || oldTicket.Status != TicketStatus.Booked)
                return new TicketActionResponseDto { Success = false, Message = "Vé cũ không hợp lệ hoặc chưa thanh toán." };

            var departureDateTime = oldTicket.Trip != null
                ? oldTicket.Trip.TripDate.Date.Add(oldTicket.Trip.DepartureTime)
                : oldTicket.Schedule?.Trip != null
                    ? oldTicket.Schedule.Trip.TripDate.Date.Add(oldTicket.Schedule.DepartureTime)
                    : DateTime.Now;

            double hoursToDeparture = (departureDateTime - DateTime.Now).TotalHours;
            if (hoursToDeparture < 4)
                return new TicketActionResponseDto { Success = false, Message = "Chỉ được phép đổi vé trước giờ khởi hành ít nhất 4 tiếng." };

            var newTrip = await _context.Trips.FindAsync(request.NewTripId);
            if (newTrip == null || (newTrip.Status != "Active" && newTrip.Status != "SCHEDULED"))
                return new TicketActionResponseDto { Success = false, Message = "Chuyến xe mới không khả dụng." };

            bool isSeatTaken = await _context.Tickets.AnyAsync(t => t.TripId == request.NewTripId &&
                                                                   t.SeatNumber == request.NewSeatNumber &&
                                                                   t.Status != TicketStatus.Cancelled);
            if (isSeatTaken)
                return new TicketActionResponseDto { Success = false, Message = "Ghế mới chọn đã có người đặt." };

            decimal exchangeFeeRate = hoursToDeparture >= 24 ? 0.00m : 0.10m;
            decimal exchangeFee = oldTicket.Price * exchangeFeeRate;
            decimal priceDifference = (newTrip.Price - oldTicket.Price) + exchangeFee;

            if (priceDifference > 0)
            {
                return new TicketActionResponseDto
                {
                    Success = true,
                    Message = $"Giá vé mới cao hơn. Bạn cần thanh toán thêm {priceDifference:N0} VNĐ để hoàn tất đổi vé.",
                    AdditionalPaymentAmount = priceDifference,
                    PaymentUrl = $"/api/payment/create-checkout?ticketId={oldTicket.Id}&newTripId={newTrip.Id}&seat={request.NewSeatNumber}&amount={priceDifference}"
                };
            }
            else if (priceDifference < 0)
            {
                decimal refundAmount = Math.Abs(priceDifference);

                oldTicket.TripId = newTrip.Id;
                oldTicket.SeatNumber = request.NewSeatNumber;
                oldTicket.Price = newTrip.Price;
                oldTicket.UpdatedAt = DateTime.Now;

                var refundTx = new RefundTransaction
                {
                    TicketId = oldTicket.Id,
                    OriginalAmount = oldTicket.Price,
                    CancellationFee = exchangeFee,
                    RefundAmount = refundAmount,
                    TransactionType = "Exchange_Difference",
                    Reason = $"Hoàn tiền chênh lệch khi đổi sang chuyến #{newTrip.Id}",
                    Status = RefundStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                _context.RefundTransactions.Add(refundTx);
                await _context.SaveChangesAsync();

                return new TicketActionResponseDto
                {
                    Success = true,
                    Message = $"Đổi vé thành công! Số tiền dư {refundAmount:N0} VNĐ đã được khởi tạo giao dịch hoàn trả.",
                    RefundAmount = refundAmount,
                    NewTicketCode = oldTicket.TicketCode
                };
            }
            else
            {
                oldTicket.TripId = newTrip.Id;
                oldTicket.SeatNumber = request.NewSeatNumber;
                oldTicket.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new TicketActionResponseDto
                {
                    Success = true,
                    Message = "Đổi vé thành công! Thông tin chuyến xe mới đã được cập nhật.",
                    NewTicketCode = oldTicket.TicketCode
                };
            }
        }
    }
}