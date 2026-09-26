using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Models;
using Ticket.Data;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TuyenXeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TuyenXeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TuyenXe
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var danhSach = await _context.Set<BusRoute>()
                .AsNoTracking()
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .OrderBy(x => x.Id)
                .ToListAsync();

            return Ok(danhSach);
        }

        // GET: api/TuyenXe/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tuyen = await _context.Set<BusRoute>()
                .AsNoTracking()
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tuyến xe"
                });
            }

            return Ok(tuyen);
        }

        // POST: api/TuyenXe
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BusRoute route)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (string.IsNullOrWhiteSpace(route.RouteName))
            {
                return BadRequest(new
                {
                    message = "Tên tuyến không được để trống"
                });
            }

            if (route.TotalDistanceKm < 0)
            {
                return BadRequest(new
                {
                    message = "Tổng chiều dài tuyến không được âm"
                });
            }

            if (route.EstimatedDuration <= TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Thời gian chạy dự kiến phải lớn hơn 0"
                });
            }

            // Id do database sinh ra.
            route.Id = 0;

            // Không nhận RouteStops từ client khi tạo tuyến.
            // Trạm của tuyến nên được quản lý qua RouteStop.
            route.RouteStops = new List<RouteStop>();

            _context.Set<BusRoute>().Add(route);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = route.Id },
                route
            );
        }

        // PUT: api/TuyenXe/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] BusRoute route)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var tuyen = await _context.Set<BusRoute>()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tuyến xe"
                });
            }

            if (string.IsNullOrWhiteSpace(route.RouteName))
            {
                return BadRequest(new
                {
                    message = "Tên tuyến không được để trống"
                });
            }

            if (route.TotalDistanceKm < 0)
            {
                return BadRequest(new
                {
                    message = "Tổng chiều dài tuyến không được âm"
                });
            }

            if (route.EstimatedDuration <= TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Thời gian chạy dự kiến phải lớn hơn 0"
                });
            }

            tuyen.RouteName = route.RouteName.Trim();
            tuyen.TotalDistanceKm = route.TotalDistanceKm;
            tuyen.EstimatedDuration = route.EstimatedDuration;
            tuyen.IsActive = route.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật tuyến xe thành công",
                data = tuyen
            });
        }

        // DELETE: api/TuyenXe/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tuyen = await _context.Set<BusRoute>()
                .Include(x => x.RouteStops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tuyến xe"
                });
            }

            // Không xóa tuyến nếu tuyến vẫn đang có trạm.
            // Tránh làm mất dữ liệu RouteStop ngoài ý muốn.
            if (tuyen.RouteStops.Any())
            {
                return Conflict(new
                {
                    message = "Không thể xóa tuyến xe vì tuyến này đang có trạm dừng. Hãy xóa các RouteStop trước."
                });
            }

            _context.Set<BusRoute>().Remove(tuyen);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa tuyến xe thành công"
            });
        }
    }
}
