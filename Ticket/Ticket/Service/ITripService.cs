using SmartBusTicketing.DTOs;

namespace SmartBusTicketing.Services
{
    public interface ITripService
    {
        Task<List<TripResponseDto>> SearchTripsByOriginAsync(TripSearchRequestDto request);
    }
}