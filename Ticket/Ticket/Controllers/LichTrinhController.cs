using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Models;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LichTrinhController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LichTrinhController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: api/LichTrinh
        // Lấy toàn bộ lịch trình
        // =====================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripSchedule>>> GetLichTrinh()
        {
            var schedules = await _context.TripSchedules
                .Include(x => x.Trip)
                    .ThenInclude(x => x.Route)
                .Include(x => x.BusStop)
                .OrderBy(x => x.TripId)
                .ThenBy(x => x.StopOrder)
                .ToListAsync();

            return Ok(schedules);
        }


        // =====================================================
        // GET: api/LichTrinh/5
        // Lấy một lịch trình theo ID
        // =====================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<TripSchedule>> GetLichTrinh(int id)
        {
            var schedule = await _context.TripSchedules
                .Include(x => x.Trip)
                    .ThenInclude(x => x.Route)
                .Include(x => x.BusStop)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch trình"
                });
            }

            return Ok(schedule);
        }


        // =====================================================
        // POST: api/LichTrinh
        // Tạo lịch trình
        // =====================================================
        [HttpPost]
        public async Task<ActionResult<TripSchedule>> CreateLichTrinh(
            TripSchedule schedule)
        {
            if (schedule == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu lịch trình không hợp lệ"
                });
            }

            // Kiểm tra chuyến xe
            var trip = await _context.Trips
                .FirstOrDefaultAsync(x => x.Id == schedule.TripId);

            if (trip == null)
            {
                return BadRequest(new
                {
                    message = "Chuyến xe không tồn tại"
                });
            }

            // Kiểm tra trạm xe
            var busStop = await _context.BusStops
                .FirstOrDefaultAsync(x => x.Id == schedule.StopId);

            if (busStop == null)
            {
                return BadRequest(new
                {
                    message = "Điểm dừng không tồn tại"
                });
            }

            // Kiểm tra thứ tự trạm
            if (schedule.StopOrder <= 0)
            {
                return BadRequest(new
                {
                    message = "Thứ tự trạm phải lớn hơn 0"
                });
            }

            // Kiểm tra trùng thứ tự trạm
            var duplicateOrder = await _context.TripSchedules
                .AnyAsync(x =>
                    x.TripId == schedule.TripId &&
                    x.StopOrder == schedule.StopOrder);

            if (duplicateOrder)
            {
                return BadRequest(new
                {
                    message = "Thứ tự trạm đã tồn tại trong chuyến xe"
                });
            }

            // Kiểm tra trùng trạm
            var duplicateStop = await _context.TripSchedules
                .AnyAsync(x =>
                    x.TripId == schedule.TripId &&
                    x.StopId == schedule.StopId);

            if (duplicateStop)
            {
                return BadRequest(new
                {
                    message = "Điểm dừng đã tồn tại trong chuyến xe"
                });
            }

            schedule.Id = 0;

            _context.TripSchedules.Add(schedule);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetLichTrinh),
                new { id = schedule.Id },
                schedule
            );
        }


        // =====================================================
        // PUT: api/LichTrinh/5
        // Sửa lịch trình
        // =====================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLichTrinh(
            int id,
            TripSchedule schedule)
        {
            if (id != schedule.Id)
            {
                return BadRequest(new
                {
                    message = "ID không khớp"
                });
            }

            // Tìm lịch trình hiện tại
            var existingSchedule = await _context.TripSchedules
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existingSchedule == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch trình"
                });
            }

            // Kiểm tra chuyến xe
            var trip = await _context.Trips
                .FirstOrDefaultAsync(x => x.Id == schedule.TripId);

            if (trip == null)
            {
                return BadRequest(new
                {
                    message = "Chuyến xe không tồn tại"
                });
            }

            // Kiểm tra điểm dừng
            var busStop = await _context.BusStops
                .FirstOrDefaultAsync(x => x.Id == schedule.StopId);

            if (busStop == null)
            {
                return BadRequest(new
                {
                    message = "Điểm dừng không tồn tại"
                });
            }

            // Kiểm tra thứ tự
            if (schedule.StopOrder <= 0)
            {
                return BadRequest(new
                {
                    message = "Thứ tự trạm phải lớn hơn 0"
                });
            }

            // Kiểm tra trùng thứ tự
            var duplicateOrder = await _context.TripSchedules
                .AnyAsync(x =>
                    x.Id != id &&
                    x.TripId == schedule.TripId &&
                    x.StopOrder == schedule.StopOrder);

            if (duplicateOrder)
            {
                return BadRequest(new
                {
                    message = "Thứ tự trạm đã tồn tại"
                });
            }

            // Kiểm tra trùng điểm dừng
            var duplicateStop = await _context.TripSchedules
                .AnyAsync(x =>
                    x.Id != id &&
                    x.TripId == schedule.TripId &&
                    x.StopId == schedule.StopId);

            if (duplicateStop)
            {
                return BadRequest(new
                {
                    message = "Điểm dừng đã tồn tại trong chuyến xe"
                });
            }

            // Cập nhật
            existingSchedule.TripId = schedule.TripId;
            existingSchedule.StopId = schedule.StopId;
            existingSchedule.StopOrder = schedule.StopOrder;
            existingSchedule.ArrivalTime = schedule.ArrivalTime;
            existingSchedule.DepartureTime = schedule.DepartureTime;
            existingSchedule.Status = schedule.Status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật lịch trình thành công",
                data = existingSchedule
            });
        }


        // =====================================================
        // DELETE: api/LichTrinh/5
        // Xóa lịch trình
        // =====================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLichTrinh(int id)
        {
            var schedule = await _context.TripSchedules
                .FirstOrDefaultAsync(x => x.Id == id);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch trình"
                });
            }

            _context.TripSchedules.Remove(schedule);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa lịch trình thành công"
            });
        }
    }
}