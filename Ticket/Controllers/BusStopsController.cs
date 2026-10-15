using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusStopsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;
        private readonly AuditLogService _auditLog;

        public BusStopsController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization,
            AuditLogService auditLog)
        {
            _context = context;
            _authorization = authorization;
            _auditLog = auditLog;
        }

        // =========================================================
        // GET: api/BusStops
        // =========================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusStop>>>
            GetBusStops()
        {
            var stops = await _context.BusStops
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();

            return Ok(stops);
        }

        // =========================================================
        // GET: api/BusStops/5
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BusStop>>
            GetBusStop(int id)
        {
            var busStop =
                await _context.BusStops
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (busStop == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy trạm dừng."
                });
            }

            return Ok(busStop);
        }

        // =========================================================
        // POST: api/BusStops
        // Chỉ MANAGER
        // =========================================================
        [HttpPost]
        public async Task<ActionResult<BusStop>>
            CreateBusStop(
                [FromBody] BusStop busStop)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Manager được phép thêm trạm."
                });
            }

            if (busStop == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu trạm không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    busStop.StopCode))
            {
                return BadRequest(new
                {
                    message =
                        "Mã trạm không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    busStop.Name))
            {
                return BadRequest(new
                {
                    message =
                        "Tên trạm không được để trống."
                });
            }

            var stopCode =
                busStop.StopCode.Trim().ToUpper();

            var codeExists =
                await _context.BusStops
                    .AnyAsync(x =>
                        x.StopCode == stopCode);

            if (codeExists)
            {
                return BadRequest(new
                {
                    message =
                        "Mã trạm đã tồn tại."
                });
            }

            busStop.StopCode = stopCode;
            busStop.Name = busStop.Name.Trim();
            busStop.Address =
                busStop.Address?.Trim() ?? "";
            busStop.IsActive = true;

            _context.BusStops.Add(busStop);

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Tạo trạm",
                "BusStop",
                busStop.Id.ToString(),
                "SUCCESS");

            return CreatedAtAction(
                nameof(GetBusStop),
                new { id = busStop.Id },
                busStop);
        }

        // =========================================================
        // PUT: api/BusStops/5
        // Chỉ MANAGER
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult>
            UpdateBusStop(
                int id,
                [FromBody] BusStop busStop)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Manager được phép sửa trạm."
                });
            }

            if (busStop == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu trạm không hợp lệ."
                });
            }

            if (id != busStop.Id)
            {
                return BadRequest(new
                {
                    message =
                        "ID trạm không khớp."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    busStop.StopCode))
            {
                return BadRequest(new
                {
                    message =
                        "Mã trạm không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    busStop.Name))
            {
                return BadRequest(new
                {
                    message =
                        "Tên trạm không được để trống."
                });
            }

            var existing =
                await _context.BusStops
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (existing == null)
            {
                return NotFound(new
                {
                    message =
                        "Trạm không tồn tại."
                });
            }

            var normalizedCode =
                busStop.StopCode
                    .Trim()
                    .ToUpper();

            var duplicatedCode =
                await _context.BusStops
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.StopCode ==
                        normalizedCode);

            if (duplicatedCode)
            {
                return BadRequest(new
                {
                    message =
                        "Mã trạm đã tồn tại."
                });
            }

            existing.StopCode =
                normalizedCode;

            existing.Name =
                busStop.Name.Trim();

            existing.Address =
                busStop.Address?.Trim() ?? "";

            existing.Description =
                busStop.Description?.Trim();

            existing.Latitude =
                busStop.Latitude;

            existing.Longitude =
                busStop.Longitude;

            existing.IsActive =
                busStop.IsActive;

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Cập nhật trạm",
                "BusStop",
                existing.Id.ToString(),
                "SUCCESS");

            return Ok(new
            {
                message =
                    "Cập nhật trạm thành công.",
                data = existing
            });
        }

        // =========================================================
        // DELETE: api/BusStops/5
        // Chỉ MANAGER
        //
        // Nếu trạm đang được dùng trong RouteStops:
        // -> Không xóa cứng
        // -> Chuyển sang ngừng hoạt động
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult>
            DeleteBusStop(int id)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Manager được phép xóa trạm."
                });
            }

            var busStop =
                await _context.BusStops
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (busStop == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy trạm để xóa."
                });
            }

            var isUsed =
                await _context.RouteStops
                    .AnyAsync(x =>
                        x.StopId == id);

            if (isUsed)
            {
                busStop.IsActive = false;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Trạm đang được sử dụng nên đã được chuyển sang Ngừng hoạt động."
                });
            }

            _context.BusStops.Remove(busStop);

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Xoá trạm",
                "BusStop",
                busStop.Id.ToString(),
                "SUCCESS");

            return Ok(new
            {
                message =
                    "Đã xóa trạm thành công."
            });
        }
    }
}