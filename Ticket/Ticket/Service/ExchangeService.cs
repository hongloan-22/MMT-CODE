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
        private readonly RefundService _refundService;

        public TicketExchangeService(ApplicationDbContext context, RefundService refundService)
        {
            _context = context;
            _refundService = refundService;
        }

        public async Task<TicketActionResponseDto> ExchangeTicketAsync(ExchangeTicketDto request, string userId)
        {
            // 1. Lấy thông tin vé cũ
            var oldTicket = await _context.Tickets
                .Include(t => t.Trip)
                .FirstOrDefaultAsync(t => t.Id == request.OldTicketId && t.UserId == userId);

            if (oldTicket == null || oldTicket.Status != TicketStatus.Paid)
                return new TicketActionResponseDto { Success = false, Message = "Vé cũ không hợp lệ hoặc chưa thanh toán." };

            double hoursToDeparture = (oldTicket.Trip.DepartureTime - DateTime.Now).TotalHours;
            if (hoursToDeparture < 4)
                return new TicketActionResponseDto { Success = false, Message = "Chỉ được phép đổi vé trước giờ khởi hành ít nhất 4 tiếng." };

            // 2. Lấy thông tin chuyến xe mới
            var newTrip = await _context.Trips.FindAsync(request.NewTripId);
            if (newTrip == null || newTrip.Status != "Active")
                return new TicketActionResponseDto { Success = false, Message = "Chuyến xe mới không khả dụng." };

            // 3. Kiểm tra ghế mới đã có người đặt chưa
            bool isSeatTaken = await _context.Tickets.AnyAsync(t => t.TripId == request.NewTripId &&
                                                                   t.SeatNumber == request.NewSeatNumber &&
                                                                   t.Status != TicketStatus.Booked);
            if (isSeatTaken)
                return new TicketActionResponseDto { Success = false, Message = "Ghế mới chọn đã có người đặt." };

            // 4. Tính phí đổi vé & Số tiền chênh lệch (US-108)
            decimal exchangeFeeRate = hoursToDeparture >= 24 ? 0.00m : 0.10m; // Miễn phí đổi >24h, 10% nếu 4-24h
            decimal exchangeFee = oldTicket.Price * exchangeFeeRate;

            // Công thức: Chênh lệch = (Giá vé mới - Giá vé cũ) + Phí đổi vé
            decimal priceDifference = (newTrip.Price - oldTicket.Price) + exchangeFee;

            // Xử lý các trường hợp chênh lệch:
            if (priceDifference > 0)
            {
                // Trường hợp 1: Khách cần trả thêm tiền -> Trả về yêu cầu thanh toán bổ sung
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
                // Trường hợp 2: Giá vé mới thấp hơn -> Hoàn lại phần tiền thừa
                decimal refundAmount = Math.Abs(priceDifference);

                // Cập nhật thông tin vé sang chuyến mới
                oldTicket.TripId = newTrip.Id;
                oldTicket.SeatNumber = request.NewSeatNumber;
                oldTicket.Price = newTrip.Price;
                oldTicket.UpdatedAt = DateTime.Now;

                // Tạo bản ghi giao dịch hoàn tiền phần chênh lệch
                var refundTx = new RefundTransaction
                {
                    TicketId = oldTicket.Id,
                    ProcessedByUserId = userId,
                    OriginalAmount = oldTicket.Price,
                    CancellationFee = exchangeFee,
                    RefundAmount = refundAmount,
                    Type = TransactionType.Exchange,
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
                // Trường hợp 3: Không chênh lệch giá -> Đổi vé ngay
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