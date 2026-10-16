using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        // =====================================================
        // US-155: Cấu hình QuestPDF (chạy một lần cho mọi request)
        // =====================================================

        static ReportService()
        {
            // QuestPDF yêu cầu khai báo license trước khi tạo file.
            QuestPDF.Settings.License =
                QuestPDF.Infrastructure.LicenseType.Community;

            // Dùng thêm font cài sẵn của hệ điều hành để tiếng Việt
            // luôn hiển thị đúng nếu font mặc định thiếu ký tự.
            QuestPDF.Settings.UseSystemFonts = true;

            // Từ 2026.9.0 QuestPDF ném lỗi khi một font trong danh
            // sách dự phòng không tồn tại trên máy chủ (mặc định
            // bật). Tắt để PDF vẫn xuất được bằng các font còn lại
            // (VD máy chủ Linux/Docker không có Arial/Tahoma) thay
            // vì trả về lỗi 500.
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

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

            // Chi tiết theo kỳ VÀ theo tuyến: mỗi dòng luôn có
            // RouteId/RouteCode/RouteName tra từ bảng BusRoutes,
            // kể cả khi báo cáo xem tất cả tuyến (routeId = null).
            report.Details = BuildDetails(
                payments,
                refunds,
                groupBy,
                allRoutes);

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
        // US-154: XUẤT BÁO CÁO DOANH THU RA EXCEL (.xlsx)
        //
        // BR-26: file xuất phải dùng đúng bộ lọc và đúng logic
        // tổng hợp như trên màn hình, nên luôn lấy dữ liệu qua
        // GetRevenueReportAsync(filter) thay vì tự truy vấn lại.
        // =====================================================

        public async Task<byte[]> ExportRevenueToExcelAsync(
            RevenueReportFilterDto filter)
        {
            filter ??= new RevenueReportFilterDto();

            // Cùng một nguồn dữ liệu với màn hình báo cáo (BR-26)
            var report = await GetRevenueReportAsync(filter);

            var details = report.Details ??
                          new List<RevenueItemDto>();

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets.Add("Doanh thu");

            // ===== Dòng tiêu đề =====
            var headers = new[]
            {
                "STT",
                "Kỳ",
                "Mã tuyến",
                "Tên tuyến",
                "Doanh thu",
                "Hoàn tiền",
                "Doanh thu thuần",
                "Số GD"
            };

            for (var column = 0;
                 column < headers.Length;
                 column++)
            {
                sheet.Cell(1, column + 1).Value =
                    headers[column];
            }

            var headerRange = sheet.Range(
                1,
                1,
                1,
                headers.Length);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Fill.BackgroundColor =
                XLColor.LightBlue;

            headerRange.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // ===== Dữ liệu theo kỳ + tuyến (đúng bộ lọc) =====
            //
            // Mỗi dòng là một kỳ của một tuyến nên cột Mã tuyến /
            // Tên tuyến luôn có giá trị.
            // Dữ liệu rỗng: file chỉ có dòng tiêu đề và dòng
            // TỔNG (bằng 0), vẫn mở được bình thường.
            var row = 2;
            var order = 1;

            foreach (var item in details)
            {
                // Excel lưu số dạng double nên ghi bằng (double)
                // để không phụ thuộc overload decimal.
                sheet.Cell(row, 1).Value = (double)order;

                WritePeriodCell(
                    sheet.Cell(row, 2),
                    item.Period);

                sheet.Cell(row, 3).Value =
                    item.RouteCode ?? string.Empty;

                sheet.Cell(row, 4).Value =
                    item.RouteName ?? string.Empty;

                sheet.Cell(row, 5).Value = (double)item.Payment;
                sheet.Cell(row, 6).Value = (double)item.Refund;
                sheet.Cell(row, 7).Value = (double)item.Net;
                sheet.Cell(row, 8).Value =
                    (double)item.Transactions;

                row++;
                order++;
            }

            // ===== Dòng tổng cộng =====
            sheet.Cell(row, 1).Value = "TỔNG";
            sheet.Cell(row, 5).Value =
                (double)report.GrossRevenue;
            sheet.Cell(row, 6).Value =
                (double)report.RefundAmount;
            sheet.Cell(row, 7).Value =
                (double)report.NetRevenue;
            sheet.Cell(row, 8).Value =
                (double)report.TransactionCount;

            var totalRange = sheet.Range(
                row,
                1,
                row,
                headers.Length);

            totalRange.Style.Font.Bold = true;

            totalRange.Style.Border.TopBorder =
                XLBorderStyleValues.Thin;

            // Định dạng tiền VND: #,##0 (không phần thập phân)
            sheet.Range(2, 5, row, 7).Style
                .NumberFormat.Format = "#,##0";

            sheet.Columns().AdjustToContents();

            // Ghi ra bộ nhớ, không tạo file tạm trên đĩa.
            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        // =====================================================
        // US-155: XUẤT BÁO CÁO DOANH THU RA PDF (.pdf)
        //
        // BR-26: file PDF phải dùng đúng bộ lọc và đúng logic
        // tổng hợp như trên màn hình, nên luôn lấy dữ liệu qua
        // GetRevenueReportAsync(filter) thay vì tự truy vấn lại.
        //
        // File rỗng (không có giao dịch thành công trong kỳ) vẫn
        // xuất được: có tiêu đề, có bộ lọc, có bảng header và
        // dòng TỔNG = 0 (không lỗi).
        // =====================================================

        public async Task<byte[]> ExportRevenueToPdfAsync(
            RevenueReportFilterDto filter)
        {
            filter ??= new RevenueReportFilterDto();

            // Cùng một nguồn dữ liệu với màn hình báo cáo (BR-26)
            var report = await GetRevenueReportAsync(filter);

            var details = report.Details ??
                          new List<RevenueItemDto>();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Bảng nhiều cột số liệu nên dùng khổ ngang
                    // cho dễ đọc.
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.2f, Unit.Centimetre);

                    page.DefaultTextStyle(x => x
                        .FontSize(10)
                        .FontFamily(
                            "Lato",
                            "Arial",
                            "Tahoma",
                            "Times New Roman"));

                    page.Header()
                        .Element(x => ComposePdfHeader(x, report));

                    page.Content()
                        .Element(x => ComposePdfContent(
                            x,
                            report,
                            details));

                    page.Footer().Element(ComposePdfFooter);
                });
            });

            // Ghi ra bộ nhớ, không tạo file tạm trên đĩa.
            return document.GeneratePdf();
        }

        // =====================================================
        // Header PDF: tiêu đề + khoảng thời gian + bộ lọc đã áp dụng
        // =====================================================

        private static void ComposePdfHeader(
            IContainer container,
            RevenueReportDto report)
        {
            var routeLabel =
                string.IsNullOrWhiteSpace(report.RouteCode)
                    ? "Tất cả tuyến"
                    : $"{report.RouteCode} - {report.RouteName}";

            container.Column(column =>
            {
                column.Item()
                    .AlignCenter()
                    .Text("BÁO CÁO DOANH THU")
                    .FontSize(16)
                    .Bold();

                column.Item()
                    .PaddingTop(3)
                    .AlignCenter()
                    .Text(
                        $"Khoảng thời gian: {report.From} " +
                        $"đến {report.To}")
                    .FontSize(11);

                column.Item()
                    .PaddingTop(2)
                    .AlignCenter()
                    .Text(
                        $"Tuyến: {routeLabel}   |   " +
                        $"Nhóm theo: {GetGroupByLabel(report.GroupBy)}   |   " +
                        $"Đơn vị tiền tệ: {report.Currency}")
                    .FontSize(10);

                column.Item()
                    .PaddingTop(2)
                    .AlignCenter()
                    .Text(
                        "Ngày tạo báo cáo: " +
                        report.GeneratedAt.ToString(
                            "yyyy-MM-dd HH:mm",
                            System.Globalization.CultureInfo
                                .InvariantCulture) +
                        " (UTC)")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                // Đường kẻ phân cách header với phần nội dung
                column.Item()
                    .PaddingTop(6)
                    .Height(1)
                    .Background(Colors.Grey.Darken2);
            });
        }

        // =====================================================
        // Nội dung PDF: bảng chi tiết theo kỳ + dòng TỔNG (bold)
        // =====================================================

        private static void ComposePdfContent(
            IContainer container,
            RevenueReportDto report,
            List<RevenueItemDto> details)
        {
            container.Column(column =>
            {
                column.Spacing(6);

                // File rỗng vẫn phản ánh đúng trạng thái không có
                // dữ liệu: thiếu các dòng dữ liệu nhưng vẫn có
                // dòng TỔNG = 0.
                if (!report.HasData)
                {
                    column.Item()
                        .Text(
                            report.Message ??
                            "Không có giao dịch thành công trong kỳ báo cáo.")
                        .Italic()
                        .FontColor(Colors.Red.Medium);
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);   // STT
                        columns.ConstantColumn(70);   // Kỳ
                        columns.ConstantColumn(60);   // Mã tuyến
                        columns.RelativeColumn(3);    // Tên tuyến
                        columns.RelativeColumn(2);    // Doanh thu
                        columns.RelativeColumn(2);    // Hoàn tiền
                        columns.RelativeColumn(2);    // Thuần
                        columns.ConstantColumn(40);   // GD
                    });

                    table.Header(header =>
                    {
                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignCenter()
                            .Text("STT");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignCenter()
                            .Text("Kỳ");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignCenter()
                            .Text("Mã tuyến");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .Text("Tên tuyến");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignRight()
                            .Text("Doanh thu");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignRight()
                            .Text("Hoàn tiền");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignRight()
                            .Text("Thuần");

                        header.Cell()
                            .Element(PdfHeaderCell)
                            .AlignRight()
                            .Text("GD");
                    });

                    // Mỗi dòng là một kỳ của một tuyến nên cột
                    // Mã tuyến / Tên tuyến luôn có giá trị.
                    var order = 1;

                    foreach (var item in details)
                    {
                        table.Cell()
                            .Element(PdfBodyCell)
                            .Text(order.ToString());

                        table.Cell()
                            .Element(PdfBodyCell)
                            .AlignCenter()
                            .Text(item.Period ?? string.Empty);

                        table.Cell()
                            .Element(PdfBodyCell)
                            .AlignCenter()
                            .Text(item.RouteCode ?? string.Empty);

                        table.Cell()
                            .Element(PdfBodyCell)
                            .Text(item.RouteName ?? string.Empty);

                        table.Cell()
                            .Element(PdfBodyCell)
                            .AlignRight()
                            .Text(FormatMoney(item.Payment));

                        table.Cell()
                            .Element(PdfBodyCell)
                            .AlignRight()
                            .Text(FormatMoney(item.Refund));

                        table.Cell()
                            .Element(PdfBodyCell)
                            .AlignRight()
                            .Text(FormatMoney(item.Net));

                        table.Cell()
                            .Element(PdfBodyCell)
                            .AlignRight()
                            .Text(item.Transactions.ToString());

                        order++;
                    }

                    // ===== Hàng TỔNG cuối bảng (bold) =====
                    table.Cell()
                        .ColumnSpan(4)
                        .Element(PdfTotalCell)
                        .Text("TỔNG")
                        .Bold();

                    table.Cell()
                        .Element(PdfTotalCell)
                        .AlignRight()
                        .Text(FormatMoney(report.GrossRevenue))
                        .Bold();

                    table.Cell()
                        .Element(PdfTotalCell)
                        .AlignRight()
                        .Text(FormatMoney(report.RefundAmount))
                        .Bold();

                    table.Cell()
                        .Element(PdfTotalCell)
                        .AlignRight()
                        .Text(FormatMoney(report.NetRevenue))
                        .Bold();

                    table.Cell()
                        .Element(PdfTotalCell)
                        .AlignRight()
                        .Text(report.TransactionCount.ToString())
                        .Bold();
                });

                // Số liệu tổng hợp, khớp với màn hình báo cáo
                column.Item().PaddingTop(4).Text(text =>
                {
                    text.Span("Doanh thu thuần: ").SemiBold();
                    text.Span(
                        $"{FormatMoney(report.NetRevenue)} " +
                        $"{report.Currency}");

                    text.Span("   |   Số GD thanh toán: ").SemiBold();
                    text.Span(report.TransactionCount.ToString());

                    text.Span("   |   Số GD hoàn tiền: ").SemiBold();
                    text.Span(report.RefundCount.ToString());

                    text.Span("   |   Số vé phát hành: ").SemiBold();
                    text.Span(report.TicketCount.ToString());

                    text.Span("   |   GD trung bình: ").SemiBold();
                    text.Span(
                        $"{FormatMoney(report.AverageTransaction)} " +
                        $"{report.Currency}");
                });
            });
        }

        // =====================================================
        // Footer PDF: số trang (Trang X / Y)
        // =====================================================

        private static void ComposePdfFooter(IContainer container)
        {
            container
                .PaddingTop(8)
                .AlignCenter()
                .DefaultTextStyle(x => x
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1))
                .Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        }

        // Kiểu ô tiêu đề bảng
        private static IContainer PdfHeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken2)
                .Padding(4);
        }

        // Kiểu ô dữ liệu
        private static IContainer PdfBodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(4)
                .PaddingVertical(3);
        }

        // Kiểu ô dòng TỔNG
        private static IContainer PdfTotalCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .BorderTop(1)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken2)
                .PaddingHorizontal(4)
                .PaddingVertical(4);
        }

        // Định dạng tiền: N0 (VD: 150,000) - không phần thập phân
        private static string FormatMoney(decimal value)
        {
            return value.ToString(
                "N0",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        // Nhãn nhóm dữ liệu hiển thị trên file PDF.
        // report.GroupBy đã được chuẩn hóa trước đó nên chỉ cần
        // ánh xạ sang nhãn tiếng Việt.
        private static string GetGroupByLabel(string? groupBy)
        {
            return groupBy switch
            {
                WeekGroupBy => "tuần",
                MonthGroupBy => "tháng",
                _ => "ngày"
            };
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
        // TỔNG HỢP THEO KỲ + TUYẾN (ngày / tuần / tháng)
        //
        // Mỗi dòng = một kỳ của MỘT tuyến, nên RouteId kèm
        // RouteCode/RouteName luôn có giá trị thật lấy từ
        // BusRoutes — kể cả khi không lọc theo tuyến.
        // =====================================================

        private static List<RevenueItemDto> BuildDetails(
            List<RevenueRow> payments,
            List<RevenueRow> refunds,
            string groupBy,
            List<RevenueRouteOptionDto> allRoutes)
        {
            // Lookup mã/tên tuyến: đủ mọi tuyến (kể cả ngừng
            // hoạt động) nên không cần truy vấn lại DB.
            var routeLookup =
                new Dictionary<int, RevenueRouteOptionDto>();

            foreach (var route in allRoutes)
            {
                routeLookup[route.Id] = route;
            }

            var buckets =
                new Dictionary<
                    (DateTime Start, int RouteId),
                    RevenueBucket>();

            foreach (var row in payments)
            {
                GetBucket(
                        buckets,
                        row.OccurredAt,
                        groupBy,
                        row.RouteId)
                    .Payments
                    .Add(row);
            }

            foreach (var row in refunds)
            {
                GetBucket(
                        buckets,
                        row.OccurredAt,
                        groupBy,
                        row.RouteId)
                    .Refunds
                    .Add(row);
            }

            var result = new List<RevenueItemDto>();

            foreach (var entry in buckets
                         .OrderBy(x => x.Key.Start)
                         .ThenBy(x => x.Key.RouteId))
            {
                var bucket = entry.Value;

                var payment =
                    bucket.Payments.Sum(x => x.Amount);

                var refund =
                    bucket.Refunds.Sum(x => x.Amount);

                // Tra theo tuyến của chính dòng dữ liệu
                RevenueRouteOptionDto? route =
                    routeLookup.TryGetValue(
                            entry.Key.RouteId,
                            out var foundRoute)
                        ? foundRoute
                        : null;

                result.Add(new RevenueItemDto
                {
                    Period = bucket.Period,
                    StartDate = bucket.StartDate,
                    EndDate = bucket.EndDate,
                    RouteId = entry.Key.RouteId,
                    RouteCode = route?.Code ?? string.Empty,
                    RouteName = route?.Name ?? string.Empty,
                    Payment = payment,
                    Refund = refund,
                    Net = payment - refund,
                    Transactions = bucket.Payments.Count
                });
            }

            return result;
        }

        // Lấy (hoặc tạo mới) bucket theo kỳ + tuyến
        private static RevenueBucket GetBucket(
            Dictionary<
                (DateTime Start, int RouteId),
                RevenueBucket> buckets,
            DateTime occurredAt,
            string groupBy,
            int routeId)
        {
            var (start, end, period) = GetPeriod(
                occurredAt,
                groupBy);

            var key = (Start: start, RouteId: routeId);

            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new RevenueBucket
                {
                    Period = period,
                    StartDate = start,
                    EndDate = end
                };

                buckets[key] = bucket;
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

        // Ghi cột "Kỳ" của file Excel:
        //   - theo ngày  (yyyy-MM-dd): ghi dạng ngày thật để
        //     Excel nhận đúng kiểu ngày.
        //   - theo tuần  (yyyy-Www) và theo tháng (yyyy-MM):
        //     giữ nguyên nhãn dạng chuỗi.
        private static void WritePeriodCell(
            IXLCell cell,
            string? period)
        {
            var value = period ?? string.Empty;

            if (DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo
                        .InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var date))
            {
                cell.Value = date;
                cell.Style.DateFormat.Format = "yyyy-MM-dd";
            }
            else
            {
                cell.Value = value;
            }

            cell.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
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


