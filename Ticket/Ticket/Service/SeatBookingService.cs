using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Ticket.Models;
using Ticket.Service;

namespace Ticket.Service
{
    public class SeatBookingService : ISeatBookingService
    {
        // Bộ nhớ tạm lưu trữ sơ đồ ghế và lệnh giữ chỗ
        private static readonly ConcurrentDictionary<string, List<Seat>> _tripSeatMaps = new();
        private static readonly ConcurrentDictionary<string, SeatHold> _holds = new();
        private readonly object _lockObject = new();

        public SeatBookingService()
        {
            // Mặc định khởi tạo trước sơ đồ cho chuyến TRIP01
            InitSampleData("TRIP01");
        }

        /// <summary>
        /// Hàm kiểm tra định dạng mã chuyến (Chỉ chấp nhận tiền tố "TRIP" + số, VD: TRIP01, TRIP02)
        /// </summary>

        private bool IsValidTripId(string tripId)
        {
            if (string.IsNullOrWhiteSpace(tripId))
                return false;

            // Regex kiểm tra: Phải bắt đầu bằng "TRIP" (không phân biệt hoa/thường) và theo sau là các chữ số
            return Regex.IsMatch(tripId.Trim(), @"^TRIP\d+$", RegexOptions.IgnoreCase);
        }

        private void InitSampleData(string tripId)
        {
            // Chuẩn hóa tên mã TRIP thành chữ hoa (VD: trip01 -> TRIP01)
            string formattedTripId = tripId.Trim().ToUpper();

            if (!_tripSeatMaps.ContainsKey(formattedTripId))
            {
                var seats = new List<Seat>();
                char[] rows = { 'A', 'B', 'C', 'D' };
                foreach (var row in rows)
                {
                    for (int col = 1; col <= 6; col++)
                    {
                        seats.Add(new Seat
                        {
                            SeatId = $"{row}{col}",
                            SeatNumber = $"{row}{col}",
                            Row = row - 'A' + 1,
                            Column = col,
                            Price = 150000,
                            Status = SeatStatus.Available
                        });
                    }
                }
                _tripSeatMaps.TryAdd(formattedTripId, seats);
            }
        }

        // 1. Lấy sơ đồ & trạng thái ghế theo chuyến
        public SeatMapResponseDto GetSeatMap(string tripId)
        {
            // 🛑 BÁO LỖI NGAY: Nếu mã chuyến không phải dạng TRIP (VD: BC02, TB-01, abc...)
            if (!IsValidTripId(tripId))
            {
                return null; // Trả về null để Controller trả lỗi 404 Not Found ngay lập tức
            }

            string formattedTripId = tripId.Trim().ToUpper();

            // Khởi tạo dữ liệu nếu là mã TRIP mới hợp lệ
            InitSampleData(formattedTripId);
            CleanExpiredHolds(formattedTripId); // Dọn dẹp ghế hết hạn

            var seats = _tripSeatMaps[formattedTripId];
            return new SeatMapResponseDto
            {
                TripId = formattedTripId,
                TotalSeats = seats.Count,
                AvailableSeats = seats.Count(s => s.Status == SeatStatus.Available),
                Seats = seats
            };
        }

        // 2. Tạo giữ chỗ
        public HoldResponseDto CreateHold(CreateHoldRequestDto request)
        {
            lock (_lockObject)
            {
                // 🛑 BÁO LỖI NGAY: Nếu mã chuyến giữ chỗ không hợp lệ
                if (!IsValidTripId(request.TripId))
                {
                    return new HoldResponseDto
                    {
                        Status = "FAILED",
                        Message = $"Mã chuyến '{request.TripId}' không hợp lệ! Định dạng bắt buộc phải là TRIP... (Ví dụ: TRIP01)."
                    };
                }

                string formattedTripId = request.TripId.Trim().ToUpper();

                InitSampleData(formattedTripId);
                CleanExpiredHolds(formattedTripId);

                var seats = _tripSeatMaps[formattedTripId];
                var requestedSeats = seats.Where(s => request.SeatIds.Contains(s.SeatId, StringComparer.OrdinalIgnoreCase)).ToList();

                if (requestedSeats.Count != request.SeatIds.Count)
                {
                    return new HoldResponseDto { Status = "FAILED", Message = "Một số vị trí ghế không tồn tại!" };
                }

                // Kiểm tra xem có ghế nào không ở trạng thái Available không
                var unavailableSeats = requestedSeats.Where(s => s.Status != SeatStatus.Available).Select(s => s.SeatId).ToList();
                if (unavailableSeats.Any())
                {
                    return new HoldResponseDto
                    {
                        Status = "FAILED",
                        Message = $"Các ghế sau đang được giữ hoặc đã đặt: {string.Join(", ", unavailableSeats)}"
                    };
                }

                // Chuyển trạng thái các ghế sang Held
                foreach (var seat in requestedSeats)
                {
                    seat.Status = SeatStatus.Held;
                }

                var hold = new SeatHold
                {
                    TripId = formattedTripId,
                    UserId = request.UserId,
                    SeatIds = request.SeatIds,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(request.HoldDurationMinutes)
                };

                _holds.TryAdd(hold.HoldId, hold);

                return new HoldResponseDto
                {
                    HoldId = hold.HoldId,
                    TripId = hold.TripId,
                    SeatIds = hold.SeatIds,
                    ExpiresAt = hold.ExpiresAt,
                    Status = "SUCCESS",
                    Message = "Giữ chỗ thành công!"
                };
            }
        }

        // 3. Kiểm tra thông tin giữ chỗ
        public HoldResponseDto CheckHoldStatus(string holdId)
        {
            if (!_holds.TryGetValue(holdId, out var hold))
            {
                return new HoldResponseDto { Status = "NOT_FOUND", Message = "Mã giữ chỗ không tồn tại!" };
            }

            if (hold.IsReleased)
            {
                return new HoldResponseDto { HoldId = holdId, Status = "RELEASED", Message = "Lượt giữ chỗ này đã bị hủy trước đó." };
            }

            if (hold.IsExpired)
            {
                return new HoldResponseDto { HoldId = holdId, Status = "EXPIRED", Message = "Lượt giữ chỗ này đã hết hạn." };
            }

            return new HoldResponseDto
            {
                HoldId = hold.HoldId,
                TripId = hold.TripId,
                SeatIds = hold.SeatIds,
                ExpiresAt = hold.ExpiresAt,
                Status = "ACTIVE",
                Message = "Giữ chỗ đang có hiệu lực."
            };
        }

        // 4. Giải phóng (Hủy) giữ chỗ
        public bool ReleaseHold(string holdId, out string message)
        {
            lock (_lockObject)
            {
                if (!_holds.TryGetValue(holdId, out var hold))
                {
                    message = "Không tìm thấy mã giữ chỗ!";
                    return false;
                }

                if (hold.IsReleased)
                {
                    message = "Giữ chỗ đã được giải phóng trước đó!";
                    return false;
                }

                // Giải phóng trạng thái ghế trở lại Available
                if (_tripSeatMaps.TryGetValue(hold.TripId, out var seats))
                {
                    foreach (var seat in seats.Where(s => hold.SeatIds.Contains(s.SeatId, StringComparer.OrdinalIgnoreCase)))
                    {
                        if (seat.Status == SeatStatus.Held)
                        {
                            seat.Status = SeatStatus.Available;
                        }
                    }
                }

                hold.IsReleased = true;
                message = "Giải phóng giữ chỗ thành công!";
                return true;
            }
        }

        // Hàm phụ trợ tự động nhả ghế nếu quá hạn thời gian
        private void CleanExpiredHolds(string tripId)
        {
            var expiredHolds = _holds.Values
                .Where(h => h.TripId.Equals(tripId, StringComparison.OrdinalIgnoreCase) && !h.IsReleased && !h.IsConfirmed && DateTime.UtcNow > h.ExpiresAt)
                .ToList();

            foreach (var hold in expiredHolds)
            {
                ReleaseHold(hold.HoldId, out _);
            }
        }
    }
}