using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Bus_Ticketing_System.Models;
using SmartBusTicketing.Data;
using SmartBusTicketing.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusStopsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BusStopsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/BusStops (Lấy danh sách tất cả trạm)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusStop>>> GetBusStops()
        {
            return await _context.BusStops.ToListAsync();
        }

        // 2. GET: api/BusStops/5 (Lấy 1 trạm theo ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<BusStop>> GetBusStop(int id)
        {
            var busStop = await _context.BusStops.FindAsync(id);

            if (busStop == null)
            {
                return NotFound(new { message = "Không tìm thấy trạm dừng!" });
            }

            return busStop;
        }

        // 3. POST: api/BusStops (Thêm trạm mới)
        [HttpPost]
        public async Task<ActionResult<BusStop>> CreateBusStop(BusStop busStop)
        {
            _context.BusStops.Add(busStop);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBusStop), new { id = busStop.Id }, busStop);
        }

        // 4. PUT: api/BusStops/5 (Cập nhật trạm)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBusStop(int id, BusStop busStop)
        {
            if (id != busStop.Id)
            {
                return BadRequest(new { message = "ID trạm không khớp!" });
            }

            _context.Entry(busStop).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BusStopExists(id))
                {
                    return NotFound(new { message = "Trạm không tồn tại!" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Cập nhật trạm thành công!", data = busStop });
        }

        // 5. DELETE: api/BusStops/5 (Xóa trạm)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusStop(int id)
        {
            var busStop = await _context.BusStops.FindAsync(id);
            if (busStop == null)
            {
                return NotFound(new { message = "Không tìm thấy trạm để xóa!" });
            }

            _context.BusStops.Remove(busStop);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa trạm thành công!" });
        }

        private bool BusStopExists(int id)
        {
            return _context.BusStops.Any(e => e.Id == id);
        }
    }
}