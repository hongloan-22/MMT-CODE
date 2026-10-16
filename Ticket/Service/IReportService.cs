using SmartBusTicketing.DTOs;

namespace SmartBusTicketing.Services
{
    public interface IReportService
    {
        // US-148: Báo cáo doanh thu theo bộ lọc ngày/tuần/tháng/tuyến.
        //
        // US-154 (Excel) và US-155 (PDF) sẽ gọi lại chính method này
        // để bảo đảm file xuất dùng đúng bộ lọc và đúng logic
        // tổng hợp như trên màn hình (BR-26).
        Task<RevenueReportDto> GetRevenueReportAsync(
            RevenueReportFilterDto filter);
    }
}
