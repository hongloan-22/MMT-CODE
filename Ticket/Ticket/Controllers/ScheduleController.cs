using Microsoft.AspNetCore.Mvc;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        // Danh sách lưu trữ dữ liệu tĩnh (In-Memory)
        private static readonly List<BusScheduleDto> Schedules = new();

        [HttpGet("search")]
        public IActionResult Search(
            [FromQuery] string? departureLocation,
            [FromQuery] string? arrivalLocation,
            [FromQuery] DateTime? departureDate)
        {
            var query = Schedules.AsQueryable();

            // Logic lọc theo điểm đi
            if (!string.IsNullOrWhiteSpace(departureLocation))
            {
                query = query.Where(s => s.DepartureLocation.Contains(departureLocation, StringComparison.OrdinalIgnoreCase));
            }

            // Logic lọc theo điểm đến
            if (!string.IsNullOrWhiteSpace(arrivalLocation))
            {
                query = query.Where(s => s.ArrivalLocation.Contains(arrivalLocation, StringComparison.OrdinalIgnoreCase));
            }

            // Logic lọc theo ngày khởi hành
            if (departureDate.HasValue)
            {
                query = query.Where(s => s.DepartureTime.Date == departureDate.Value.Date);
            }

            return Ok(query.ToList());
        }
    }

    public class BusScheduleDto
    {
        public int Id { get; set; }
        public string DepartureLocation { get; set; } = string.Empty;
        public string ArrivalLocation { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
    }
}