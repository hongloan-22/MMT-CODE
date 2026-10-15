using SmartBusTicketing.DTOs;

namespace SmartBusTicketing.Services
{
    public interface ITripService
    {
        Task<List<TripResponseDto>> SearchTripsByOriginAsync(TripSearchRequestDto request);

        // =====================================================
        // THÊM MỚI: Lấy thông tin tổng ghế, đã đặt, còn trống, tỷ lệ lấp đầy
        // =====================================================
        Task<TripOccupancyDto> GetTripOccupancyAsync(int tripId);
    }
}