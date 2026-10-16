using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.DTOs;
using Ticket.Data;

namespace SmartBusTicketing.Services
{
    public class ReportService : IReportService
    {
        private const string SuccessStatus = "SUCCESS";

        private const string DayGroupBy = "day";
        private const string WeekGroupBy = "week";
        private const string MonthGroupBy = "month";

        private const int DefaultRangeDays = 30;

        private readonly ApplicationDbContext _context;

        public ReportService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // US-148: BÁO CÁO DOANH THU (BR-22)
        //
        // Doanh thu thuần =
        //   Σ PaymentTransaction.Status = "SUCCESS"
        //   − Σ RefundTransaction.Status = "SUCCESS"
        //
        // Quy tắc kỳ ghi nhận:
        //   payment: PaidAt ?? CreatedAt
        //   refund : ProcessedAt ?? CreatedAt
        // =====================================================

        public async Task<RevenueReportDto> GetRevenueReportAsync(
            RevenueReportFilterDto filter)
        {
            filter ??= new RevenueReportFilterDto();

            var groupBy = NormalizeGroupBy(filter.GroupBy);

            // routeId <= 0 coi như không lọc tuyến
            var routeId = filter.RouteId.HasValue &&
                          filter.RouteId.Value > 0
                ? filter.RouteId
                : null;

            var from = (filter.FromDate ??
                        DateTime.UtcNow.Date
                            .AddDays(-DefaultRangeDays))
                .Date;

            var to = (filter.ToDate ??
                      DateTime.UtcNow.Date)
                .Date;

            var report = new RevenueReportDto
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd"),
                RouteId = routeId,
                GroupBy = groupBy,
                Currency = "VND",
                GeneratedAt = DateTime.UtcNow
            };

            var allRoutes = await LoadRoutesAsync();

            report.Routes = BuildRouteOptions(
                allRoutes,
                routeId);

            var selectedRoute = routeId.HasValue
                ? allRoutes.FirstOrDefault(x =>
                    x.Id == routeId.Value)
                : null;

            if (selectedRoute != null)
            {
                report.RouteCode = selectedRoute.Code;
                report.RouteName = selectedRoute.Name;
            }

            // Khoảng ngày không hợp lệ: trả rỗng, không lỗi.
            if (from > to)
            {
                report.Message =
                    "Khoảng thời gian không hợp lệ.";

                return report;
            }

            var toExclusive = to.AddDays(1);

            var payments = await LoadPaymentsAsync(
                from,
                toExclusive,
                routeId);

            var refunds = await LoadRefundsAsync(
                from,
                toExclusive,
                routeId);

            report.TicketCount = await CountTicketsAsync(
                from,
                toExclusive,
                routeId);

            report.GrossRevenue =
                payments.Sum(x => x.Amount);

            report.RefundAmount =
                refunds.Sum(x => x.Amount);

            report.NetRevenue =
                report.GrossRevenue -
                report.RefundAmount;

            report.TransactionCount = payments.Count;
            report.RefundCount = refunds.Count;

            report.AverageTransaction =
                report.TransactionCount > 0
                    ? Math.Round(
                        report.GrossRevenue /
                        report.TransactionCount,
                        0,
                        MidpointRounding.AwayFromZero)
                    : 0m;

            report.Daily = BuildDaily(
                payments,
                refunds);

            report.ByRoute = BuildByRoute(
                payments,
                refunds,
                allRoutes);

            report.Details = BuildDetails(
                payments,
                refunds,
                groupBy,
                routeId,
                report.RouteCode,
                report.RouteName);

            // Đối chiếu với giao dịch gốc (AC US-148 — mục 1.7)
            if (filter.IncludeTransactions)
            {
                report.Transactions = BuildTransactions(
                    payments,
                    refunds,
                    allRoutes);
            }

            report.HasData =
                report.TransactionCount > 0 ||
                report.RefundCount > 0;

            if (!report.HasData)
            {
                report.Message =
                    "Không có giao dịch thành công trong kỳ báo cáo.";
            }

            return report;
        }

        // =====================================================
        // DANH SÁCH TUYẾN CHO BỘ LỌC
        // =====================================================

        private async Task<List<RevenueRouteOptionDto>> LoadRoutesAsync()
        {
            return await _context.BusRoutes
                .AsNoTracking()
                .OrderBy(x => x.RouteCode)
                .Select(x => new RevenueRouteOptionDto
                {
                    Id = x.Id,
                    Code = x.RouteCode,
                    Name = x.RouteName,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        private static List<RevenueRouteOptionDto> BuildRouteOptions(
            List<RevenueRouteOptionDto> allRoutes,
            int? routeId)
        {
            var options = allRoutes
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .ToList();

            // Tuyến đang chọn mà đã ngừng hoạt động vẫn phải
            // xuất hiện để giữ đúng bộ lọc đang xem.
            if (routeId.HasValue &&
                options.All(x => x.Id != routeId.Value))
            {
                var selected = allRoutes
                    .FirstOrDefault(x =>
                        x.Id == routeId.Value);

                if (selected != null)
                    options.Add(selected);
            }

            return options;
        }

        // =====================================================
        // TRUY VẤN NGUỒN DOANH THU
        //
        // Chỉ lấy giao dịch Status = "SUCCESS" (BR-22),
        // bỏ qua PENDING / FAILED / CANCELLED.
        // =====================================================

        private async Task<List<RevenueRow>> LoadPaymentsAsync(
            DateTime from,
            DateTime toExclusive,
            int? routeId)
        {
            var query = _context.PaymentTransactions
                .AsNoTracking()
                .Where(x =>
                    x.Status == SuccessStatus &&
                    x.Booking != null &&
                    x.Booking.Trip != null &&
                    ((x.PaidAt != null &&
                      x.PaidAt >= from &&
                      x.PaidAt < toExclusive) ||
                     (x.PaidAt == null &&
                      x.CreatedAt >= from &&
                      x.CreatedAt < toExclusive)));

            if (routeId.HasValue)
            {
                query = query.Where(x =>
                    x.Booking!.Trip!.RouteId == routeId.Value);
            }

            return await query
                .Select(x => new RevenueRow
                {
                    Id = x.Id,
                    OccurredAt = x.PaidAt ?? x.CreatedAt,
                    CreatedAt = x.CreatedAt,
                    Amount = x.Amount,
                    RouteId = x.Booking!.Trip!.RouteId,
                    Reference = x.TransactionCode,
                    Description = x.Provider,
                    BookingCode = x.Booking!.BookingCode,
                    TicketCode = string.Empty
                })
                .ToListAsync();
        }

        private async Task<List<RevenueRow>> LoadRefundsAsync(
            DateTime from,
            DateTime toExclusive,
            int? routeId)
        {
            var query = _context.RefundTransactions
                .AsNoTracking()
                .Where(x =>
                    x.Status == SuccessStatus &&
                    x.Ticket != null &&
                    ((x.ProcessedAt != null &&
                      x.ProcessedAt >= from &&
                      x.ProcessedAt < toExclusive) ||
                     (x.ProcessedAt == null &&
                      x.CreatedAt >= from &&
                      x.CreatedAt < toExclusive)));

            if (routeId.HasValue)
            {
                query = query.Where(x =>
                    x.Ticket!.RouteId == routeId.Value);
            }

            return await query
                .Select(x => new RevenueRow
                {
                    Id = x.Id,
                    OccurredAt = x.ProcessedAt ?? x.CreatedAt,
                    CreatedAt = x.CreatedAt,
                    Amount = x.Amount,
                    RouteId = x.Ticket!.RouteId,
                    Reference = string.Empty,
                    Description = x.Reason,
                    BookingCode = x.Ticket!.Booking!.BookingCode,
                    TicketCode = x.Ticket!.TicketCode
                })
                .ToListAsync();
        }

        private async Task<int> CountTicketsAsync(
            DateTime from,
            DateTime toExclusive,
            int? routeId)
        {
            var query = _context.Tickets
                .AsNoTracking()
                .Where(x =>
                    x.IssuedAt >= from &&
                    x.IssuedAt < toExclusive);

            if (routeId.HasValue)
            {
                query = query.Where(x =>
                    x.RouteId == routeId.Value);
            }

            return await query.CountAsync();
        }

        // Một dòng doanh thu đã chuẩn hoá về kỳ ghi nhận
        private sealed class RevenueRow
        {
            public int Id { get; set; }

            public DateTime OccurredAt { get; set; }

            public DateTime CreatedAt { get; set; }

            public decimal Amount { get; set; }

            public int RouteId { get; set; }

            // PaymentTransaction.TransactionCode
            public string Reference { get; set; } = string.Empty;

            // Provider (payment) / Reason (refund)
            public string Description { get; set; } = string.Empty;

            public string BookingCode { get; set; } = string.Empty;

            public string TicketCode { get; set; } = string.Empty;
        }

        // =====================================================
        // TỔNG HỢP THEO NGÀY
        // =====================================================

        private static List<RevenueDailyItemDto> BuildDaily(
            List<RevenueRow> payments,
            List<RevenueRow> refunds)
        {
            var days = payments
                .Select(x => x.OccurredAt.Date)
                .Concat(refunds.Select(x => x.OccurredAt.Date))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var result = new List<RevenueDailyItemDto>();

            foreach (var day in days)
            {
                var dayPayments = payments
                    .Where(x => x.OccurredAt.Date == day)
                    .ToList();

                var dayRefunds = refunds
                    .Where(x => x.OccurredAt.Date == day)
                    .ToList();

                var payment = dayPayments.Sum(x => x.Amount);
                var refund = dayRefunds.Sum(x => x.Amount);

                result.Add(new RevenueDailyItemDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Payment = payment,
                    Refund = refund,
                    Revenue = payment - refund,
                    Transactions = dayPayments.Count
                });
            }

            return result;
        }

        // =====================================================
        // TỔNG HỢP THEO TUYẾN
        // =====================================================

        private static List<RevenueByRouteItemDto> BuildByRoute(
            List<RevenueRow> payments,
            List<RevenueRow> refunds,
            List<RevenueRouteOptionDto> allRoutes)
        {
            var routeIds = payments
                .Select(x => x.RouteId)
                .Concat(refunds.Select(x => x.RouteId))
                .Distinct()
                .ToList();

            var result = new List<RevenueByRouteItemDto>();

            foreach (var routeId in routeIds)
            {
                var routePayments = payments
                    .Where(x => x.RouteId == routeId)
                    .ToList();

                var routeRefunds = refunds
                    .Where(x => x.RouteId == routeId)
                    .ToList();

                var route = allRoutes
                    .FirstOrDefault(x => x.Id == routeId);

                var payment = routePayments.Sum(x => x.Amount);
                var refund = routeRefunds.Sum(x => x.Amount);

                result.Add(new RevenueByRouteItemDto
                {
                    RouteId = routeId,
                    RouteCode = route?.Code ?? string.Empty,
                    RouteName = route?.Name ?? string.Empty,
                    Payment = payment,
                    Refund = refund,
                    Revenue = payment - refund,
                    Transactions = routePayments.Count
                });
            }

            return result
                .OrderByDescending(x => x.Revenue)
                .ThenBy(x => x.RouteCode)
                .ToList();
        }

        // =====================================================
        // DANH SÁCH GIAO DỊCH GỐC (ĐỐI CHIẾU SỐ LIỆU)
        //
        // Tổng SignedAmount của danh sách này luôn bằng
        // NetRevenue của báo cáo (AC US-148 mục 1.7, TC-13).
        // =====================================================

        private static List<RevenueTransactionDto> BuildTransactions(
            List<RevenueRow> payments,
            List<RevenueRow> refunds,
            List<RevenueRouteOptionDto> allRoutes)
        {
            var result = new List<RevenueTransactionDto>();

            foreach (var row in payments)
            {
                var route = allRoutes
                    .FirstOrDefault(x =>
                        x.Id == row.RouteId);

                result.Add(new RevenueTransactionDto
                {
                    Type = "PAYMENT",
                    TransactionId = row.Id,
                    TransactionCode = row.Reference,
                    Description = row.Description,
                    BookingCode = row.BookingCode,
                    TicketCode = row.TicketCode,
                    RouteId = row.RouteId,
                    RouteCode = route?.Code ?? string.Empty,
                    RouteName = route?.Name ?? string.Empty,
                    OccurredAt = row.OccurredAt,
                    CreatedAt = row.CreatedAt,
                    Amount = row.Amount,
                    SignedAmount = row.Amount
                });
            }

            foreach (var row in refunds)
            {
                var route = allRoutes
                    .FirstOrDefault(x =>
                        x.Id == row.RouteId);

                result.Add(new RevenueTransactionDto
                {
                    Type = "REFUND",
                    TransactionId = row.Id,
                    TransactionCode = row.Reference,
                    Description = row.Description,
                    BookingCode = row.BookingCode,
                    TicketCode = row.TicketCode,
                    RouteId = row.RouteId,
                    RouteCode = route?.Code ?? string.Empty,
                    RouteName = route?.Name ?? string.Empty,
                    OccurredAt = row.OccurredAt,
                    CreatedAt = row.CreatedAt,
                    Amount = row.Amount,
                    SignedAmount = -row.Amount
                });
            }

            return result
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.Type)
                .ThenBy(x => x.TransactionId)
                .ToList();
        }

        // =====================================================
        // TỔNG HỢP THEO KỲ (ngày / tuần / tháng)
        // =====================================================

        private static List<RevenueItemDto> BuildDetails(
            List<RevenueRow> payments,
            List<RevenueRow> refunds,
            string groupBy,
            int? routeId,
            string routeCode,
            string routeName)
        {
            var buckets =
                new Dictionary<DateTime, RevenueBucket>();

            foreach (var row in payments)
            {
                GetBucket(buckets, row.OccurredAt, groupBy)
                    .Payments
                    .Add(row);
            }

            foreach (var row in refunds)
            {
                GetBucket(buckets, row.OccurredAt, groupBy)
                    .Refunds
                    .Add(row);
            }

            var result = new List<RevenueItemDto>();

            foreach (var bucket in buckets.Values
                         .OrderBy(x => x.StartDate))
            {
                var payment =
                    bucket.Payments.Sum(x => x.Amount);

                var refund =
                    bucket.Refunds.Sum(x => x.Amount);

                result.Add(new RevenueItemDto
                {
                    Period = bucket.Period,
                    StartDate = bucket.StartDate,
                    EndDate = bucket.EndDate,
                    RouteId = routeId,
                    RouteCode = routeCode,
                    RouteName = routeName,
                    Payment = payment,
                    Refund = refund,
                    Net = payment - refund,
                    Transactions = bucket.Payments.Count
                });
            }

            return result;
        }

        private static RevenueBucket GetBucket(
            Dictionary<DateTime, RevenueBucket> buckets,
            DateTime occurredAt,
            string groupBy)
        {
            var (start, end, period) = GetPeriod(
                occurredAt,
                groupBy);

            if (!buckets.TryGetValue(start, out var bucket))
            {
                bucket = new RevenueBucket
                {
                    Period = period,
                    StartDate = start,
                    EndDate = end
                };

                buckets[start] = bucket;
            }

            return bucket;
        }

        // Xác định kỳ chứa một mốc thời gian
        private static (DateTime Start, DateTime End, string Period)
            GetPeriod(
                DateTime occurredAt,
                string groupBy)
        {
            var date = occurredAt.Date;

            if (groupBy == MonthGroupBy)
            {
                var start = new DateTime(
                    date.Year,
                    date.Month,
                    1);

                return (
                    start,
                    start.AddMonths(1).AddDays(-1),
                    start.ToString("yyyy-MM"));
            }

            if (groupBy == WeekGroupBy)
            {
                // Tuần ISO: thứ 2 là ngày đầu tuần
                var offset =
                    ((int)date.DayOfWeek + 6) % 7;

                var start = date.AddDays(-offset);

                var isoYear =
                    System.Globalization.ISOWeek
                        .GetYear(start);

                var isoWeek =
                    System.Globalization.ISOWeek
                        .GetWeekOfYear(start);

                return (
                    start,
                    start.AddDays(6),
                    $"{isoYear}-W{isoWeek:D2}");
            }

            // Mặc định: theo ngày
            return (date, date, date.ToString("yyyy-MM-dd"));
        }

        private static string NormalizeGroupBy(string? groupBy)
        {
            var value = groupBy?
                .Trim()
                .ToLowerInvariant();

            return value switch
            {
                WeekGroupBy => WeekGroupBy,
                MonthGroupBy => MonthGroupBy,
                _ => DayGroupBy
            };
        }

        // Một kỳ báo cáo sau khi gom nhóm
        private sealed class RevenueBucket
        {
            public string Period { get; set; } = string.Empty;

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }

            public List<RevenueRow> Payments { get; set; }
                = new();

            public List<RevenueRow> Refunds { get; set; }
                = new();
        }
    }
}


