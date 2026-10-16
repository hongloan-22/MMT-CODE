using SmartBusTicketing.DTOs;

namespace SmartBusTicketing.Services
{
    public interface ITripService
    {
        Task<List<TripResponseDto>> SearchTripsByOriginAsync(TripSearchRequestDto request);

        // US-160: Thống kê tỷ lệ lấp đầy
        Task<TripOccupancyDto> GetTripOccupancyAsync(int tripId);

        // US-165: Danh sách chuyến có phân trang, lọc, sắp xếp
        Task<TripListResponseDto> GetTripListAsync(TripListRequestDto request);

        // US-159: Kiểm tra tính hợp lệ dữ liệu
        Task<DataIntegrityReportDto> CheckDataIntegrityAsync();
    }
}