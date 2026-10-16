using Microsoft.AspNetCore.Mvc;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;

namespace Ticket.Controllers
{
    // =====================================================
    // US-148: API BÁO CÁO DOANH THU
    //
    // GET /api/reports/revenue
    //   fromDate?  (mặc định: hôm nay - 30 ngày)
    //   toDate?    (mặc định: hôm nay)
    //   routeId?   (null = tất cả tuyến)
    //   groupBy?   (day = mặc định / week / month)
    //   includeTransactions? (true = kèm danh sách giao dịch gốc
    //                         để đối chiếu số liệu)
    // =====================================================

    [Route("api/reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private const int DefaultRangeDays = 30;

        private readonly IReportService _reportService;
        private readonly BackendAuthorizationService _authorization;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            BackendAuthorizationService authorization,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _authorization = authorization;
            _logger = logger;
        }

        // =====================================================
        // GET: /api/reports/revenue
        // =====================================================

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? routeId,
            [FromQuery] string? groupBy,
            [FromQuery] bool? includeTransactions)
        {
            return await BuildRevenueReportAsync(
                fromDate,
                toDate,
                routeId,
                groupBy,
                includeTransactions);
        }

        // =====================================================
        // GET: /api/Statistics/revenue
        //
        // Alias tương thích trang wwwroot/doanh-thu (trang này
        // gọi tham số from / to và chỉ đọc dữ liệu doanh thu).
        // =====================================================

        [HttpGet("/api/statistics/revenue")]
        public async Task<IActionResult> GetRevenueForReportPage(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? routeId,
            [FromQuery] string? groupBy,
            [FromQuery] bool? includeTransactions)
        {
            return await BuildRevenueReportAsync(
                from,
                to,
                routeId,
                groupBy,
                includeTransactions);
        }

        // =====================================================
        // GET: /api/reports/revenue/export/excel
        //
        // US-154: Xuất báo cáo doanh thu ra file Excel (.xlsx).
        //
        // Dùng CHÍNH bộ lọc của màn hình báo cáo (BR-26):
        //   - Cùng quy tắc kiểm tra quyền với GetRevenue
        //   - Cùng khoảng ngày mặc định (30 ngày gần nhất)
        //   - fromDate > toDate -> 400 (BR-28)
        // =====================================================

        [HttpGet("revenue/export/excel")]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? routeId,
            [FromQuery] string groupBy = "day")
        {
            // BR-26: file xuất cũng chỉ dành cho MANAGER / ADMIN
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER",
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền xuất báo cáo doanh thu."
                });
            }

            var from = (fromDate ??
                        DateTime.UtcNow.Date
                            .AddDays(-DefaultRangeDays))
                .Date;

            var to = (toDate ??
                      DateTime.UtcNow.Date)
                .Date;

            // Không tự ý đổi bộ lọc: khoảng ngày sai
            // thì báo lỗi dễ hiểu (BR-28).
            if (from > to)
            {
                return BadRequest(new
                {
                    message =
                        "Khoảng thời gian không hợp lệ: " +
                        "fromDate phải nhỏ hơn hoặc bằng toDate."
                });
            }

            var filter = new RevenueReportFilterDto
            {
                FromDate = from,
                ToDate = to,
                RouteId = routeId,
                GroupBy = string.IsNullOrWhiteSpace(groupBy)
                    ? "day"
                    : groupBy,
                // File Excel chỉ xuất phần tổng hợp theo kỳ,
                // không cần kèm danh sách giao dịch gốc.
                IncludeTransactions = false
            };

            try
            {
                var bytes = await _reportService
                    .ExportRevenueToExcelAsync(filter);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"bao-cao-doanh-thu-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                // BR-28: lỗi dễ hiểu, không phơi bày stack trace
                _logger.LogError(
                    ex,
                    "Lỗi khi xuất báo cáo doanh thu ra Excel.");

                return StatusCode(500, new
                {
                    message =
                        "Không xuất được báo cáo doanh thu. " +
                        "Vui lòng thử lại."
                });
            }
        }

        // =====================================================
        // GET: /api/reports/revenue/export/pdf
        //
        // US-155: Xuất báo cáo doanh thu ra file PDF (.pdf).
        //
        // Dùng CHÍNH bộ lọc của màn hình báo cáo (BR-26):
        //   - Cùng quy tắc kiểm tra quyền với GetRevenue
        //   - Cùng khoảng ngày mặc định (30 ngày gần nhất)
        //   - fromDate > toDate -> 400 (BR-28)
        // =====================================================

        [HttpGet("revenue/export/pdf")]
        public async Task<IActionResult> ExportPdf(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? routeId,
            [FromQuery] string groupBy = "day")
        {
            // BR-26: file xuất cũng chỉ dành cho MANAGER / ADMIN
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER",
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền xuất báo cáo doanh thu."
                });
            }

            var from = (fromDate ??
                        DateTime.UtcNow.Date
                            .AddDays(-DefaultRangeDays))
                .Date;

            var to = (toDate ??
                      DateTime.UtcNow.Date)
                .Date;

            // Không tự ý đổi bộ lọc: khoảng ngày sai
            // thì báo lỗi dễ hiểu (BR-28).
            if (from > to)
            {
                return BadRequest(new
                {
                    message =
                        "Khoảng thời gian không hợp lệ: " +
                        "fromDate phải nhỏ hơn hoặc bằng toDate."
                });
            }

            var filter = new RevenueReportFilterDto
            {
                FromDate = from,
                ToDate = to,
                RouteId = routeId,
                GroupBy = string.IsNullOrWhiteSpace(groupBy)
                    ? "day"
                    : groupBy,
                // File PDF chỉ xuất phần tổng hợp theo kỳ,
                // không cần kèm danh sách giao dịch gốc.
                IncludeTransactions = false
            };

            try
            {
                var bytes = await _reportService
                    .ExportRevenueToPdfAsync(filter);

                return File(
                    bytes,
                    "application/pdf",
                    $"bao-cao-doanh-thu-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                // BR-28: lỗi dễ hiểu, không phơi bày stack trace
                _logger.LogError(
                    ex,
                    "Lỗi khi xuất báo cáo doanh thu ra PDF.");

                return StatusCode(500, new
                {
                    message =
                        "Không xuất được báo cáo doanh thu. " +
                        "Vui lòng thử lại."
                });
            }
        }


        // =====================================================
        // XỬ LÝ CHUNG CHO CÁC ENDPOINT TRÊN
        // =====================================================

        private async Task<IActionResult> BuildRevenueReportAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? routeId,
            string? groupBy,
            bool? includeTransactions)
        {
            // BR-26: báo cáo doanh thu chỉ dành cho MANAGER / ADMIN
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER",
                    "ADMIN"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền xem báo cáo doanh thu."
                });
            }

            var from = (fromDate ??
                        DateTime.UtcNow.Date
                            .AddDays(-DefaultRangeDays))
                .Date;

            var to = (toDate ??
                      DateTime.UtcNow.Date)
                .Date;

            // Không tự ý đổi bộ lọc: khoảng ngày sai
            // thì báo lỗi dễ hiểu (BR-28).
            if (from > to)
            {
                return BadRequest(new
                {
                    message =
                        "Khoảng thời gian không hợp lệ: " +
                        "fromDate phải nhỏ hơn hoặc bằng toDate."
                });
            }

            var filter = new RevenueReportFilterDto
            {
                FromDate = from,
                ToDate = to,
                RouteId = routeId,
                GroupBy = groupBy ?? "day",
                IncludeTransactions = includeTransactions == true
            };

            try
            {
                var report =
                    await _reportService
                        .GetRevenueReportAsync(filter);

                return Ok(report);
            }
            catch (Exception ex)
            {
                // BR-28: lỗi dễ hiểu, không phơi bày stack trace
                _logger.LogError(
                    ex,
                    "Lỗi khi tạo báo cáo doanh thu.");

                return StatusCode(500, new
                {
                    message =
                        "Không tạo được báo cáo doanh thu. " +
                        "Vui lòng thử lại."
                });
            }
        }
    }
}
