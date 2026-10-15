using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TuyenXeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;
        private readonly AuditLogService _auditLog;

        public TuyenXeController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization,
            AuditLogService auditLog)
        {
            _context = context;
            _authorization = authorization;
            _auditLog = auditLog;
        }

        // =========================================================
        // GET: api/TuyenXe
        // Ai cũng có thể xem danh sách tuyến
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var danhSach = await _context.BusRoutes
                .AsNoTracking()
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .OrderBy(x => x.Id)
                .ToListAsync();

            return Ok(danhSach);
        }

        // =========================================================
        // GET: api/TuyenXe/1
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tuyen = await _context.BusRoutes
                .AsNoTracking()
                .Include(x => x.RouteStops)
                    .ThenInclude(x => x.BusStop)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tuyến xe."
                });
            }

            return Ok(tuyen);
        }

        // =========================================================
        // DTO TẠO TUYẾN
        // =========================================================
        public class CreateRouteDto
        {
            public string RouteCode { get; set; } = string.Empty;

            public string RouteName { get; set; } = string.Empty;

            public string StartPoint { get; set; } = string.Empty;

            public string EndPoint { get; set; } = string.Empty;

            public decimal Fare { get; set; }

            public double TotalDistanceKm { get; set; }

            public TimeSpan EstimatedDuration { get; set; }

            public bool IsActive { get; set; } = true;

            // Giữ tương thích frontend cũ
            public string? StartStation { get; set; }

            public string? EndStation { get; set; }
        }

        // =========================================================
        // POST: api/TuyenXe
        // Chỉ MANAGER
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateRouteDto dto)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message = "Chỉ Manager được phép tạo tuyến."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.RouteName))
            {
                return BadRequest(new
                {
                    message = "Tên tuyến không được để trống."
                });
            }

            var startPoint =
                !string.IsNullOrWhiteSpace(dto.StartPoint)
                    ? dto.StartPoint.Trim()
                    : dto.StartStation?.Trim() ?? "";

            var endPoint =
                !string.IsNullOrWhiteSpace(dto.EndPoint)
                    ? dto.EndPoint.Trim()
                    : dto.EndStation?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(dto.RouteCode))
            {
                return BadRequest(new
                {
                    message = "Mã tuyến không được để trống."
                });
            }

            if (dto.Fare < 0)
            {
                return BadRequest(new
                {
                    message = "Giá vé không được âm."
                });
            }

            if (dto.TotalDistanceKm < 0)
            {
                return BadRequest(new
                {
                    message = "Tổng chiều dài tuyến không được âm."
                });
            }

            if (dto.EstimatedDuration <= TimeSpan.Zero)
            {
                dto.EstimatedDuration =
                    TimeSpan.FromMinutes(90);
            }

            var routeCode =
                dto.RouteCode.Trim().ToUpper();

            var routeCodeExists =
                await _context.BusRoutes
                    .AnyAsync(x =>
                        x.RouteCode == routeCode);

            if (routeCodeExists)
            {
                return BadRequest(new
                {
                    message = "Mã tuyến đã tồn tại."
                });
            }

            var route = new BusRoute
            {
                RouteCode = routeCode,
                RouteName = dto.RouteName.Trim(),
                StartPoint = startPoint,
                EndPoint = endPoint,
                Fare = dto.Fare,
                TotalDistanceKm = dto.TotalDistanceKm,
                EstimatedDuration = dto.EstimatedDuration,
                IsActive = dto.IsActive
            };

            _context.BusRoutes.Add(route);

            await _context.SaveChangesAsync();

            await _auditLog.WriteAsync(
                HttpContext,
                "Tạo tuyến xe",
                "BusRoute",
                route.Id.ToString(),
                "SUCCESS");

            // =====================================================
            // Tạo / lấy trạm đầu
            // =====================================================
            async Task<BusStop?> GetOrCreateStopAsync(
                string? name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var normalizedName =
                    name.Trim();

                var stop = await _context.BusStops
                    .FirstOrDefaultAsync(x =>
                        x.Name.ToLower() ==
                        normalizedName.ToLower());

                if (stop == null)
                {
                    var stopCode =
                        "S" +
                        DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    stop = new BusStop
                    {
                        StopCode = stopCode,
                        Name = normalizedName,
                        Address =
                            $"{normalizedName}, Việt Nam",
                        IsActive = true
                    };

                    _context.BusStops.Add(stop);

                    await _context.SaveChangesAsync();
                }

                return stop;
            }

            if (!string.IsNullOrWhiteSpace(startPoint))
            {
                var startStop =
                    await GetOrCreateStopAsync(startPoint);

                if (startStop != null)
                {
                    _context.RouteStops.Add(
                        new RouteStop
                        {
                            RouteId = route.Id,
                            StopId = startStop.Id,
                            StopOrder = 1,
                            DistanceFromStartKm = 0,
                            TravelTimeFromStartMinutes = 0
                        });
                }
            }

            if (!string.IsNullOrWhiteSpace(endPoint) &&
                !string.Equals(
                    startPoint,
                    endPoint,
                    StringComparison.OrdinalIgnoreCase))
            {
                var endStop =
                    await GetOrCreateStopAsync(endPoint);

                if (endStop != null)
                {
                    _context.RouteStops.Add(
                        new RouteStop
                        {
                            RouteId = route.Id,
                            StopId = endStop.Id,
                            StopOrder = 2,
                            DistanceFromStartKm =
                                dto.TotalDistanceKm,
                            TravelTimeFromStartMinutes =
                                (int)dto.EstimatedDuration.TotalMinutes
                        });
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = route.Id },
                route);
        }

        // =========================================================
        // DTO THÊM TRẠM
        // =========================================================
        public class AddStopDto
        {
            public int RouteId { get; set; }

            public string StopCode { get; set; } = string.Empty;

            public string StopName { get; set; } = string.Empty;

            public string StopAddress { get; set; } = string.Empty;

            public string? StopDescription { get; set; }

            public int StopOrder { get; set; }

            public double DistanceKm { get; set; }

            public int TravelTimeFromStartMinutes { get; set; }
        }

        // =========================================================
        // POST: api/TuyenXe/them-tram-vao-tuyen
        // Chỉ MANAGER
        // =========================================================
        [HttpPost("them-tram-vao-tuyen")]
        public async Task<IActionResult> ThemTramVaoTuyen(
    [FromBody] AddStopDto dto)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được phép thêm trạm vào tuyến."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu không hợp lệ."
                });
            }

            if (dto.RouteId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "RouteId không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    dto.StopCode))
            {
                return BadRequest(new
                {
                    message =
                        "Mã trạm không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    dto.StopName))
            {
                return BadRequest(new
                {
                    message =
                        "Tên trạm không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    dto.StopAddress))
            {
                return BadRequest(new
                {
                    message =
                        "Địa chỉ trạm không được để trống."
                });
            }

            if (dto.DistanceKm < 0)
            {
                return BadRequest(new
                {
                    message =
                        "Khoảng cách không được âm."
                });
            }

            var route =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x => x.Id == dto.RouteId);

            if (route == null)
            {
                return NotFound(new
                {
                    message =
                        "Tuyến xe không tồn tại."
                });
            }

            var currentStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId == dto.RouteId)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            // =====================================================
            // KIỂM TRA DỮ LIỆU ROUTE STOP HIỆN TẠI
            // Khoảng cách + thời gian phải tăng theo StopOrder
            // =====================================================

            for (
                int i = 0;
                i < currentStops.Count;
                i++)
            {
                var current =
                    currentStops[i];

                if (i == 0)
                {
                    if (current.DistanceFromStartKm != 0 ||
                        current.TravelTimeFromStartMinutes != 0)
                    {
                        return Conflict(new
                        {
                            message =
                                "Dữ liệu RouteStop hiện tại không hợp lệ: trạm đầu tiên phải có khoảng cách và thời gian bằng 0."
                        });
                    }

                    continue;
                }

                var previous =
                    currentStops[i - 1];

                if (current.DistanceFromStartKm <=
                    previous.DistanceFromStartKm)
                {
                    return Conflict(new
                    {
                        message =
                            "Dữ liệu RouteStop hiện tại không hợp lệ: khoảng cách phải tăng theo thứ tự trạm."
                    });
                }

                if (current.TravelTimeFromStartMinutes <=
                    previous.TravelTimeFromStartMinutes)
                {
                    return Conflict(new
                    {
                        message =
                            "Dữ liệu RouteStop hiện tại không hợp lệ: thời gian phải tăng theo thứ tự trạm."
                    });
                }
            }

            // =====================================================
            // CHUẨN HÓA STOP CODE
            // =====================================================

            var stopCode =
                dto.StopCode
                    .Trim()
                    .ToUpperInvariant();

            var stopName =
                dto.StopName.Trim();

            var stopAddress =
                dto.StopAddress.Trim();

            // =====================================================
            // TÌM / TẠO TRẠM
            // =====================================================

            var stop =
                await _context.BusStops
                    .FirstOrDefaultAsync(
                        x =>
                            x.StopCode ==
                            stopCode);

            if (stop == null)
            {
                stop =
                    new BusStop
                    {
                        StopCode =
                            stopCode,

                        Name =
                            stopName,

                        Address =
                            stopAddress,

                        Description =
                            string.IsNullOrWhiteSpace(
                                dto.StopDescription)
                                ? null
                                : dto.StopDescription.Trim(),

                        IsActive =
                            true
                    };

                _context.BusStops.Add(stop);

                await _context.SaveChangesAsync();
            }
            else
            {
                stop.Name =
                    stopName;

                stop.Address =
                    stopAddress;

                stop.Description =
                    string.IsNullOrWhiteSpace(
                        dto.StopDescription)
                        ? null
                        : dto.StopDescription.Trim();

                stop.IsActive =
                    true;

                await _context.SaveChangesAsync();
            }

            // =====================================================
            // KHÔNG CHO TRÙNG TRẠM TRÊN CÙNG TUYẾN
            // =====================================================

            if (currentStops.Any(
                    x => x.StopId == stop.Id))
            {
                return BadRequest(new
                {
                    message =
                        "Trạm này đã tồn tại trong tuyến."
                });
            }

            // =====================================================
            // XÁC ĐỊNH STOP ORDER
            // =====================================================

            var targetOrder =
                dto.StopOrder;

            if (targetOrder <= 0)
            {
                targetOrder =
                    currentStops.Count + 1;
            }

            if (targetOrder >
                currentStops.Count + 1)
            {
                targetOrder =
                    currentStops.Count + 1;
            }

            // =====================================================
            // GIÁ TRỊ TÍCH LŨY MÀ FRONTEND GỬI
            // =====================================================

            var finalDistance =
                Math.Round(
                    dto.DistanceKm,
                    2);

            var finalTravelMinutes =
                dto.TravelTimeFromStartMinutes > 0
                    ? dto.TravelTimeFromStartMinutes
                    : Math.Max(
                        1,
                        (int)Math.Round(
                            finalDistance * 1.5));

            // =====================================================
            // KIỂM TRA VỚI TRẠM TRƯỚC
            // =====================================================

            var previousStop =
                currentStops
                    .FirstOrDefault(
                        x =>
                            x.StopOrder ==
                            targetOrder - 1);

            if (previousStop != null)
            {
                if (finalDistance <=
                    previousStop.DistanceFromStartKm)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Khoảng cách phải lớn hơn khoảng cách của trạm trước ({previousStop.DistanceFromStartKm:0.##} km)."
                    });
                }

                if (finalTravelMinutes <=
                    previousStop.TravelTimeFromStartMinutes)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Thời gian phải lớn hơn thời gian của trạm trước ({previousStop.TravelTimeFromStartMinutes} phút)."
                    });
                }
            }

            // =====================================================
            // KIỂM TRA VỚI TRẠM SAU
            // =====================================================

            var nextStop =
                currentStops
                    .FirstOrDefault(
                        x =>
                            x.StopOrder ==
                            targetOrder);

            if (nextStop != null)
            {
                if (finalDistance >=
                    nextStop.DistanceFromStartKm)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Khoảng cách phải nhỏ hơn khoảng cách của trạm sau ({nextStop.DistanceFromStartKm:0.##} km)."
                    });
                }

                if (finalTravelMinutes >=
                    nextStop.TravelTimeFromStartMinutes)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Thời gian phải nhỏ hơn thời gian của trạm sau ({nextStop.TravelTimeFromStartMinutes} phút)."
                    });
                }
            }

            // =====================================================
            // NẾU LÀ TRẠM ĐẦU
            // =====================================================

            if (targetOrder == 1 &&
                currentStops.Count > 0)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến đã có trạm xuất phát. Không thể thêm trạm mới vào vị trí đầu tiên."
                });
            }

            // =====================================================
            // DỜI THỨ TỰ CÁC TRẠM PHÍA SAU
            // =====================================================

            foreach (
                var item in currentStops
                    .Where(x =>
                        x.StopOrder >=
                        targetOrder)
                    .OrderByDescending(x =>
                        x.StopOrder))
            {
                item.StopOrder++;
            }

            // =====================================================
            // TẠO ROUTE STOP
            //
            // DistanceFromStartKm:
            //   khoảng cách TÍCH LŨY từ đầu tuyến
            //
            // TravelTimeFromStartMinutes:
            //   thời gian TÍCH LŨY từ đầu tuyến
            // =====================================================

            var routeStop =
                new RouteStop
                {
                    RouteId =
                        dto.RouteId,

                    StopId =
                        stop.Id,

                    StopOrder =
                        targetOrder,

                    DistanceFromStartKm =
                        finalDistance,

                    TravelTimeFromStartMinutes =
                        finalTravelMinutes
                };

            _context.RouteStops.Add(
                routeStop);

            // =====================================================
            // CẬP NHẬT TỔNG TUYẾN
            // Luôn lấy từ trạm cuối cùng
            // =====================================================

            var allDistances =
                currentStops
                    .Select(x =>
                        x.DistanceFromStartKm)
                    .Append(finalDistance)
                    .ToList();

            route.TotalDistanceKm =
                allDistances.Max();

            var allTimes =
                currentStops
                    .Select(x =>
                        x.TravelTimeFromStartMinutes)
                    .Append(finalTravelMinutes)
                    .ToList();

            route.EstimatedDuration =
                TimeSpan.FromMinutes(
                    allTimes.Max());

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Thêm trạm thành công.",

                stopId =
                    stop.Id,

                assignedOrder =
                    targetOrder,

                distanceFromStartKm =
                    finalDistance,

                travelTimeFromStartMinutes =
                    finalTravelMinutes,

                totalDistanceKm =
                    route.TotalDistanceKm,

                estimatedDurationMinutes =
                    (int)
                    route.EstimatedDuration
                        .TotalMinutes
            });
        }

        // =========================================================
        // DELETE:
        // api/TuyenXe/xoa-tram-khoi-tuyen
        // Chỉ MANAGER
        // =========================================================
        [HttpDelete("xoa-tram-khoi-tuyen")]
        public async Task<IActionResult> XoaTramKhoiTuyen(
    [FromQuery] int routeId,
    [FromQuery] int stopId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được phép xóa trạm khỏi tuyến."
                });
            }

            var route =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x => x.Id == routeId);

            if (route == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tuyến."
                });
            }

            var routeStop =
                await _context.RouteStops
                    .FirstOrDefaultAsync(
                        x =>
                            x.RouteId == routeId &&
                            x.StopId == stopId);

            if (routeStop == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy trạm trong tuyến."
                });
            }

            var deletedOrder =
                routeStop.StopOrder;

            _context.RouteStops.Remove(
                routeStop);

            var trailingStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId == routeId &&
                        x.StopOrder > deletedOrder)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            foreach (var item in trailingStops)
            {
                item.StopOrder--;
            }

            await _context.SaveChangesAsync();

            // =====================================================
            // CHUẨN HÓA LẠI THỨ TỰ + TỔNG QUAN TUYẾN
            // =====================================================

            var remainingStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId == routeId)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            for (
                int i = 0;
                i < remainingStops.Count;
                i++)
            {
                remainingStops[i].StopOrder =
                    i + 1;
            }

            if (remainingStops.Count == 0)
            {
                route.TotalDistanceKm =
                    0;

                route.EstimatedDuration =
                    TimeSpan.Zero;
            }
            else
            {
                var lastStop =
                    remainingStops
                        .OrderByDescending(
                            x => x.StopOrder)
                        .First();

                route.TotalDistanceKm =
                    lastStop.DistanceFromStartKm;

                route.EstimatedDuration =
                    TimeSpan.FromMinutes(
                        lastStop.TravelTimeFromStartMinutes);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã xóa trạm khỏi tuyến và chuẩn hóa lại dữ liệu tuyến."
            });
        }

        // =========================================================
        // PUT: api/TuyenXe/1
        // Chỉ MANAGER
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] BusRoute route)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Manager được phép sửa tuyến."
                });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (route == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ."
                });
            }

            var tuyen =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tuyến xe."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    route.RouteCode))
            {
                return BadRequest(new
                {
                    message =
                        "Mã tuyến không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    route.RouteName))
            {
                return BadRequest(new
                {
                    message =
                        "Tên tuyến không được để trống."
                });
            }

            var normalizedCode =
                route.RouteCode.Trim().ToUpper();

            var duplicatedCode =
                await _context.BusRoutes
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.RouteCode == normalizedCode);

            if (duplicatedCode)
            {
                return BadRequest(new
                {
                    message =
                        "Mã tuyến đã tồn tại."
                });
            }

            if (route.Fare < 0)
            {
                return BadRequest(new
                {
                    message =
                        "Giá vé không được âm."
                });
            }

            if (route.TotalDistanceKm < 0)
            {
                return BadRequest(new
                {
                    message =
                        "Tổng chiều dài tuyến không được âm."
                });
            }

            tuyen.RouteCode =
                normalizedCode;

            tuyen.RouteName =
                route.RouteName.Trim();

            tuyen.StartPoint =
                route.StartPoint?.Trim() ?? "";

            tuyen.EndPoint =
                route.EndPoint?.Trim() ?? "";

            tuyen.Fare =
                route.Fare;

            tuyen.TotalDistanceKm =
                route.TotalDistanceKm;

            tuyen.EstimatedDuration =
                route.EstimatedDuration;

            tuyen.IsActive =
                route.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Cập nhật tuyến xe thành công.",
                data = tuyen
            });
        }

        // =========================================================
        // DELETE: api/TuyenXe/1
        // Chỉ MANAGER
        //
        // Nếu tuyến đã được dùng bởi chuyến xe:
        // -> Không xóa cứng
        // -> Chuyển sang ngừng hoạt động
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Manager được phép xóa tuyến."
                });
            }

            var tuyen =
                await _context.BusRoutes
                    .Include(x => x.RouteStops)
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (tuyen == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tuyến xe cần xóa."
                });
            }

            var hasTrips =
                await _context.Trips
                    .AnyAsync(t =>
                        t.RouteId == id);

            if (hasTrips)
            {
                tuyen.IsActive = false;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Tuyến đã được chuyển sang trạng thái Ngừng hoạt động vì đã có dữ liệu lịch trình."
                });
            }

            if (tuyen.RouteStops != null &&
                tuyen.RouteStops.Any())
            {
                _context.RouteStops
                    .RemoveRange(
                        tuyen.RouteStops);
            }

            _context.BusRoutes.Remove(tuyen);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Xóa tuyến xe thành công."
            });
        }
    }
}