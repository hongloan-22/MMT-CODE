using Ticket.Models;

namespace Ticket.Service
{
    public interface ISeatBookingService
    {
        SeatMapResponseDto GetSeatMap(string tripId);
        HoldResponseDto CreateHold(CreateHoldRequestDto request);
        HoldResponseDto CheckHoldStatus(string holdId);
        bool ReleaseHold(string holdId, out string message);
    }
}
