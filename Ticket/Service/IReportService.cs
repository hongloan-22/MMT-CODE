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

        // US-155: Xuất báo cáo doanh thu ra file PDF (.pdf).
        //
        // Cùng nguyên tắc với US-154 (BR-26):
        //   - Luôn gọi lại GetRevenueReportAsync(filter) bên trong
        //     để file PDF dùng ĐÚNG bộ lọc và ĐÚNG logic tổng hợp
        //     như báo cáo trên màn hình.
        //   - Trả về mảng byte để controller tự quyết định cách
        //     trả file cho client (không ghi file tạm trên server).
        //
        // File rỗng (không có giao dịch thành công trong kỳ) vẫn
        // phải tạo được PDF: có tiêu đề, có bộ lọc, có dòng TỔNG = 0.
        Task<byte[]> ExportRevenueToPdfAsync(
            RevenueReportFilterDto filter);


    }
}
