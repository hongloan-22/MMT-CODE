using Ticket.Models;

namespace Ticket.Service
{
    public interface ISeatBookingService
    {
        SeatMapResponseDto? GetSeatMap(string tripId);
        HoldResponseDto CreateHold(CreateHoldRequestDto request);
        HoldResponseDto CheckHoldStatus(string holdId);
        bool ReleaseHold(string holdId, out string message);

        /// <summary>
        /// US06: Chot giu cho sau khi thanh toan thanh cong (ghe chuyen sang Booked).
        /// </summary>
        bool ConfirmHold(string holdId, out string message);
    }
}
