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

        // US-154: Xuất báo cáo doanh thu ra file Excel (.xlsx).
        //
        // Luôn dùng ĐÚNG bộ lọc đang xem (BR-26) bằng cách
        // gọi lại GetRevenueReportAsync(filter) bên trong.
        //
        // Trả về mảng byte để controller tự quyết định cách
        // trả file cho client (không ghi file tạm trên server).
        Task<byte[]> ExportRevenueToExcelAsync(
            RevenueReportFilterDto filter);
    }
}
