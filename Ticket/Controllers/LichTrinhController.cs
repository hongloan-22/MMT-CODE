using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LichTrinhController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;
        private readonly IRecurringScheduleGeneratorService _generator;

        public LichTrinhController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization,
            IRecurringScheduleGeneratorService generator)
        {
            _context = context;
            _authorization = authorization;
            _generator = generator;
        }

        [HttpGet]
        public async Task<IActionResult> GetLichTrinh()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Bạn không có quyền xem dữ liệu lịch trình quản lý."
                    });
            }

            try
            {
                var list =
                    await _context.TripSchedules
                        .AsNoTracking()
                        .Include(x => x.BusStop)
                        .Include(x => x.Trip)
                            .ThenInclude(t => t!.Route)
                        .OrderBy(x => x.TripId)
                        .ThenBy(x => x.StopOrder)
                        .ToListAsync();

                var result =
                    list.Select(s => new
                    {
                        id = s.Id,
                        tripId = s.TripId,
                        stopId = s.StopId,
                        stopOrder = s.StopOrder,

                        arrivalTime =
                            s.ArrivalTime.ToString(@"hh\:mm"),

                        departureTime =
                            s.DepartureTime.ToString(@"hh\:mm"),

                        status = s.Status,

                        busStop =
                            s.BusStop == null
                                ? null
                                : new
                                {
                                    id = s.BusStop.Id,
                                    stopCode = s.BusStop.StopCode,
                                    name = s.BusStop.Name,
                                    address = s.BusStop.Address
                                },

                        trip =
                            s.Trip == null
                                ? null
                                : new
                                {
                                    id = s.Trip.Id,
                                    tripCode = s.Trip.TripCode,
                                    routeId = s.Trip.RouteId,
                                    tripDate =
                                        s.Trip.TripDate
                                            .ToString("yyyy-MM-dd"),
                                    departureTime =
                                        s.Trip.DepartureTime
                                            .ToString(@"hh\:mm"),
                                    arrivalTime =
                                        s.Trip.ArrivalTime.HasValue
                                            ? s.Trip.ArrivalTime.Value
                                                .ToString(@"hh\:mm")
                                            : null,
                                    status = s.Trip.Status,
                                    isActive = s.Trip.IsActive
                                }
                    });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = ex.Message
                    });
            }
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetTrips()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Bạn không có quyền xem danh sách chuyến quản lý."
                    });
            }

            var trips =
                await _context.Trips
                    .AsNoTracking()
                    .Include(x => x.Route)
                    .Include(x => x.Bus)
                    .Include(x => x.RecurringSchedule)
                    .OrderBy(x => x.TripDate)
                    .ThenBy(x => x.DepartureTime)
                    .ToListAsync();

            var result =
                trips.Select(t => new
                {
                    id = t.Id,
                    tripCode = t.TripCode,
                    routeId = t.RouteId,
                    routeCode = t.Route?.RouteCode,
                    routeName = t.Route?.RouteName,

                    tripDate =
                        t.TripDate
                            .ToString("yyyy-MM-dd"),

                    departureTime =
                        t.DepartureTime
                            .ToString(@"hh\:mm"),

                    arrivalTime =
                        t.ArrivalTime.HasValue
                            ? t.ArrivalTime.Value
                                .ToString(@"hh\:mm")
                            : null,

                    status = t.Status,
                    isActive = t.IsActive,
                    totalSeats = t.TotalSeats,
                    availableSeats = t.AvailableSeats,
                    bookedSeats = t.BookedSeats,

                    bus =
                        t.Bus == null
                            ? null
                            : new
                            {
                                id = t.Bus.Id,
                                busCode = t.Bus.BusCode,
                                licensePlate =
                                    t.Bus.LicensePlate
                            },

                    recurringScheduleId =
                        t.RecurringScheduleId
                });

            return Ok(result);
        }

        [HttpGet("buses")]
        public async Task<IActionResult> GetBuses()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Bạn không có quyền xem danh sách xe."
                    });
            }

            var buses =
                await _context.Buses
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.Seats.Any(s => s.IsActive))
                    .OrderBy(x => x.BusCode)
                    .Select(x => new
                    {
                        id = x.Id,
                        busCode = x.BusCode,
                        licensePlate = x.LicensePlate,
                        seatCount = x.Seats.Count(s => s.IsActive)
                    })
                    .ToListAsync();

            return Ok(buses);
        }

        [HttpGet("recurring")]
        public async Task<IActionResult> GetRecurringSchedules()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Bạn không có quyền xem lịch chạy định kỳ."
                    });
            }

            var schedules =
                await _context.RecurringSchedules
                    .AsNoTracking()
                    .Include(x => x.Route)
                    .Include(x => x.Bus)
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.StartDate)
                    .ThenBy(x => x.DepartureTime)
                    .ToListAsync();

            var result =
                schedules.Select(x => new
                {
                    id = x.Id,
                    routeId = x.RouteId,
                    routeCode = x.Route?.RouteCode,
                    routeName = x.Route?.RouteName,
                    busId = x.BusId,
                    busCode = x.Bus?.BusCode,
                    licensePlate = x.Bus?.LicensePlate,
                    scheduleType = x.ScheduleType,
                    daysOfWeek = x.DaysOfWeek,

                    startDate =
                        x.StartDate
                            .ToString("yyyy-MM-dd"),

                    endDate =
                        x.EndDate.HasValue
                            ? x.EndDate.Value
                                .ToString("yyyy-MM-dd")
                            : null,

                    departureTime =
                        x.DepartureTime
                            .ToString(@"hh\:mm"),

                    isActive = x.IsActive,

                    generatedTripCount =
                        x.Trips.Count(t => t.IsActive)
                });

            return Ok(result);
        }

        public class CreateRecurringScheduleRequest
        {
            public int RouteId { get; set; }
            public int BusId { get; set; }

            public string ScheduleType { get; set; }
                = "DAILY";

            public string? DaysOfWeek { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime? EndDate { get; set; }

            public string DepartureTime { get; set; }
                = "08:00";

            public bool IsActive { get; set; }
                = true;
        }

        [HttpPost("recurring")]
        public async Task<IActionResult> CreateRecurringSchedule(
            [FromBody] CreateRecurringScheduleRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Chỉ Manager được phép lập lịch trình."
                    });
            }

            if (req == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ."
                });
            }

            if (req.RouteId <= 0)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn tuyến."
                });
            }

            if (req.BusId <= 0)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn xe."
                });
            }

            var type =
                req.ScheduleType
                    .Trim()
                    .ToUpperInvariant();

            if (type != "DAILY" &&
                type != "WEEKLY")
            {
                return BadRequest(new
                {
                    message = "Loại lịch không hợp lệ."
                });
            }

            if (!TimeSpan.TryParse(
                    req.DepartureTime,
                    out var departureTime))
            {
                return BadRequest(new
                {
                    message =
                        "Giờ khởi hành không hợp lệ."
                });
            }

            var startDate =
                req.StartDate == default
                    ? DateTime.Today
                    : req.StartDate.Date;

            if (startDate < DateTime.Today)
            {
                return BadRequest(new
                {
                    message =
                        "Ngày bắt đầu không được ở quá khứ."
                });
            }

            DateTime? endDate =
                req.EndDate?.Date;

            if (endDate.HasValue &&
                endDate.Value < startDate)
            {
                return BadRequest(new
                {
                    message =
                        "Ngày kết thúc phải sau ngày bắt đầu."
                });
            }

            if (type == "WEEKLY")
            {
                if (string.IsNullOrWhiteSpace(
                        req.DaysOfWeek))
                {
                    return BadRequest(new
                    {
                        message =
                            "Vui lòng chọn ít nhất một thứ."
                    });
                }

                var validDays =
                    req.DaysOfWeek
                        .Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                            int.TryParse(
                                x.Trim(),
                                out var day)
                                ? day
                                : -1)
                        .Where(day =>
                            day >= 0 &&
                            day <= 6)
                        .Distinct()
                        .OrderBy(day => day)
                        .ToList();

                if (validDays.Count == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Ngày trong tuần không hợp lệ."
                    });
                }

                req.DaysOfWeek =
                    string.Join(
                        ",",
                        validDays);
            }
            else
            {
                req.DaysOfWeek = null;
            }

            var route =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            req.RouteId);

            if (route == null)
            {
                return NotFound(new
                {
                    message =
                        "Tuyến xe không tồn tại."
                });
            }

            if (!route.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến đang ngừng hoạt động."
                });
            }

            var bus =
                await _context.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            req.BusId);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Xe không tồn tại."
                });
            }

            if (!bus.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Xe đang ngừng hoạt động."
                });
            }

            if (!bus.Seats.Any(x => x.IsActive))
            {
                return BadRequest(new
                {
                    message =
                        "Xe chưa có ghế hoạt động."
                });
            }

            var routeStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId ==
                        req.RouteId)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            if (routeStops.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến chưa có trạm dừng."
                });
            }

            var duplicate =
                await _context.RecurringSchedules
                    .AnyAsync(x =>
                        x.IsActive &&
                        x.RouteId ==
                            req.RouteId &&
                        x.BusId ==
                            req.BusId &&
                        x.ScheduleType ==
                            type &&
                        x.DepartureTime ==
                            departureTime &&
                        x.StartDate ==
                            startDate);

            if (duplicate)
            {
                return BadRequest(new
                {
                    message =
                        "Lịch cố định này đã tồn tại."
                });
            }

            var schedule =
                new RecurringSchedule
                {
                    RouteId =
                        req.RouteId,

                    BusId =
                        req.BusId,

                    ScheduleType =
                        type,

                    DaysOfWeek =
                        req.DaysOfWeek,

                    StartDate =
                        startDate,

                    EndDate =
                        endDate,

                    DepartureTime =
                        departureTime,

                    IsActive =
                        req.IsActive,

                    CreatedAt =
                        DateTime.UtcNow,

                    UpdatedAt =
                        DateTime.UtcNow
                };

            _context.RecurringSchedules
                .Add(schedule);

            await _context.SaveChangesAsync();

            await _generator
                .GenerateUpcomingTripsAsync(30);

            return Ok(new
            {
                message =
                    "Đã tạo lịch cố định và tự sinh chuyến.",

                scheduleId =
                    schedule.Id
            });
        }

        public class UpdateRecurringScheduleRequest
        {
            public int RouteId { get; set; }
            public int BusId { get; set; }

            public string ScheduleType { get; set; }
                = "DAILY";

            public string? DaysOfWeek { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime? EndDate { get; set; }

            public string DepartureTime { get; set; }
                = "08:00";

            public bool IsActive { get; set; }
                = true;
        }

        [HttpPut("recurring/{id:int}")]
        public async Task<IActionResult> UpdateRecurringSchedule(
            int id,
            [FromBody] UpdateRecurringScheduleRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Chỉ Manager được phép sửa lịch trình."
                    });
            }

            if (req == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu không hợp lệ."
                });
            }

            var schedule =
                await _context.RecurringSchedules
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy lịch cố định."
                });
            }

            if (!TimeSpan.TryParse(
                    req.DepartureTime,
                    out var departureTime))
            {
                return BadRequest(new
                {
                    message =
                        "Giờ khởi hành không hợp lệ."
                });
            }

            var startDate =
                req.StartDate.Date;

            DateTime? endDate =
                req.EndDate?.Date;

            if (endDate.HasValue &&
                endDate.Value < startDate)
            {
                return BadRequest(new
                {
                    message =
                        "Ngày kết thúc phải sau ngày bắt đầu."
                });
            }

            var type =
                req.ScheduleType
                    .Trim()
                    .ToUpperInvariant();

            if (type != "DAILY" &&
                type != "WEEKLY")
            {
                return BadRequest(new
                {
                    message =
                        "Loại lịch không hợp lệ."
                });
            }

            string? daysOfWeek = null;

            if (type == "WEEKLY")
            {
                if (string.IsNullOrWhiteSpace(
                        req.DaysOfWeek))
                {
                    return BadRequest(new
                    {
                        message =
                            "Vui lòng chọn ít nhất một thứ."
                    });
                }

                var validDays =
                    req.DaysOfWeek
                        .Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                            int.TryParse(
                                x.Trim(),
                                out var day)
                                ? day
                                : -1)
                        .Where(day =>
                            day >= 0 &&
                            day <= 6)
                        .Distinct()
                        .OrderBy(day => day)
                        .ToList();

                if (validDays.Count == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Thứ chạy không hợp lệ."
                    });
                }

                daysOfWeek =
                    string.Join(
                        ",",
                        validDays);
            }

            var route =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            req.RouteId);

            if (route == null)
            {
                return NotFound(new
                {
                    message =
                        "Tuyến không tồn tại."
                });
            }

            if (!route.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến đang ngừng hoạt động."
                });
            }

            var bus =
                await _context.Buses
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            req.BusId);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Xe không tồn tại."
                });
            }

            if (!bus.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Xe đang ngừng hoạt động."
                });
            }

            var routeStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId ==
                        req.RouteId)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            if (routeStops.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến chưa có trạm dừng."
                });
            }

            var duplicate =
                await _context.RecurringSchedules
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.IsActive &&
                        x.RouteId ==
                            req.RouteId &&
                        x.BusId ==
                            req.BusId &&
                        x.ScheduleType ==
                            type &&
                        x.DepartureTime ==
                            departureTime &&
                        x.StartDate ==
                            startDate);

            if (duplicate)
            {
                return BadRequest(new
                {
                    message =
                        "Lịch cố định này đã tồn tại."
                });
            }

            var futureTrips =
                await _context.Trips
                    .Where(x =>
                        x.RecurringScheduleId ==
                            id &&
                        x.TripDate >=
                            DateTime.Today &&
                        x.IsActive)
                    .ToListAsync();

            var futureTripIds =
                futureTrips
                    .Select(x => x.Id)
                    .ToList();

            if (futureTripIds.Count > 0)
            {
                var bookedTripIds =
                    await _context.Bookings
                        .Where(x =>
                            futureTripIds.Contains(
                                x.TripId))
                        .Select(x =>
                            x.TripId)
                        .Distinct()
                        .ToListAsync();

                if (bookedTripIds.Count > 0)
                {
                    return Conflict(new
                    {
                        message =
                            "Không thể sửa lịch vì đã có chuyến tương lai có đơn đặt vé. " +
                            "Hãy xử lý các đơn đặt vé trước."
                    });
                }
            }

            schedule.RouteId =
                req.RouteId;

            schedule.BusId =
                req.BusId;

            schedule.ScheduleType =
                type;

            schedule.DaysOfWeek =
                daysOfWeek;

            schedule.StartDate =
                startDate;

            schedule.EndDate =
                endDate;

            schedule.DepartureTime =
                departureTime;

            schedule.IsActive =
                req.IsActive;

            schedule.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (!schedule.IsActive)
            {
                foreach (var trip in futureTrips)
                {
                    trip.Status =
                        "CANCELLED";

                    trip.IsActive =
                        false;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Đã ngừng lịch cố định và hủy các chuyến tương lai."
                });
            }

            foreach (var trip in futureTrips)
            {
                var shouldRun =
                    ShouldRunOnDate(
                        schedule,
                        trip.TripDate);

                if (!shouldRun)
                {
                    trip.Status =
                        "CANCELLED";

                    trip.IsActive =
                        false;

                    continue;
                }

                var maxTravelMinutes =
                    routeStops.Max(
                        x =>
                            x.TravelTimeFromStartMinutes);

                var arrivalTime =
                    departureTime.Add(
                        TimeSpan.FromMinutes(
                            Math.Max(
                                1,
                                maxTravelMinutes)));

                trip.RouteId =
                    req.RouteId;

                trip.BusId =
                    req.BusId;

                trip.DepartureTime =
                    departureTime;

                trip.ArrivalTime =
                    arrivalTime;

                trip.TotalSeats =
                    bus.SeatCount;

                trip.AvailableSeats =
                    bus.SeatCount;

                trip.BookedSeats =
                    0;

                trip.Status =
                    "ACTIVE";

                trip.IsActive =
                    true;

                trip.TripCode =
                    $"SCH{id:D4}-" +
                    $"{trip.TripDate:yyyyMMdd}-" +
                    $"{departureTime:hhmm}";

                var oldTripSchedules =
                    await _context.TripSchedules
                        .Where(x =>
                            x.TripId ==
                            trip.Id)
                        .ToListAsync();

                _context.TripSchedules
                    .RemoveRange(
                        oldTripSchedules);

                for (
                    int index = 0;
                    index < routeStops.Count;
                    index++)
                {
                    var routeStop =
                        routeStops[index];

                    var arrival =
                        departureTime.Add(
                            TimeSpan.FromMinutes(
                                routeStop
                                    .TravelTimeFromStartMinutes));

                    var isLast =
                        index ==
                        routeStops.Count - 1;

                    var departure =
                        isLast
                            ? arrival
                            : arrival.Add(
                                TimeSpan.FromMinutes(5));

                    _context.TripSchedules.Add(
                        new TripSchedule
                        {
                            TripId =
                                trip.Id,

                            StopId =
                                routeStop.StopId,

                            StopOrder =
                                routeStop.StopOrder,

                            ArrivalTime =
                                arrival,

                            DepartureTime =
                                departure,

                            Status =
                                "SCHEDULED"
                        });
                }
            }

            await _context.SaveChangesAsync();

            await _generator
                .GenerateUpcomingTripsAsync(30);

            return Ok(new
            {
                message =
                    "Đã cập nhật lịch cố định và đồng bộ các chuyến tương lai."
            });
        }

        private static bool ShouldRunOnDate(
            RecurringSchedule schedule,
            DateTime date)
        {
            if (!schedule.IsActive)
                return false;

            if (date.Date <
                schedule.StartDate.Date)
            {
                return false;
            }

            if (schedule.EndDate.HasValue &&
                date.Date >
                schedule.EndDate.Value.Date)
            {
                return false;
            }

            var type =
                schedule.ScheduleType
                    .Trim()
                    .ToUpperInvariant();

            if (type == "DAILY")
                return true;

            if (type != "WEEKLY")
                return false;

            if (string.IsNullOrWhiteSpace(
                    schedule.DaysOfWeek))
            {
                return false;
            }

            var selectedDays =
                schedule.DaysOfWeek
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                        int.TryParse(
                            x.Trim(),
                            out var day)
                            ? day
                            : -1)
                    .ToHashSet();

            return selectedDays.Contains(
                (int)date.DayOfWeek);
        }

        [HttpDelete("recurring/{id:int}")]
        public async Task<IActionResult>
            DeleteRecurringSchedule(int id)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Chỉ Manager được phép xóa lịch trình."
                    });
            }

            var schedule =
                await _context.RecurringSchedules
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy lịch cố định."
                });
            }

            schedule.IsActive =
                false;

            schedule.UpdatedAt =
                DateTime.UtcNow;

            var futureTrips =
                await _context.Trips
                    .Where(x =>
                        x.RecurringScheduleId ==
                            id &&
                        x.TripDate >=
                            DateTime.Today &&
                        x.IsActive)
                    .ToListAsync();

            foreach (var trip in futureTrips)
            {
                trip.Status =
                    "CANCELLED";

                trip.IsActive =
                    false;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã ngừng lịch cố định và hủy các chuyến tương lai."
            });
        }

        [HttpPost("recurring/{id:int}/generate")]
        public async Task<IActionResult>
            GenerateRecurringSchedule(
                int id)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Chỉ Manager được phép thực hiện."
                    });
            }

            var schedule =
                await _context.RecurringSchedules
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == id &&
                            x.IsActive);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy lịch cố định đang hoạt động."
                });
            }

            await _generator
                .GenerateUpcomingTripsAsync(30);

            return Ok(new
            {
                message =
                    "Đã cập nhật các chuyến sắp chạy."
            });
        }

        public class UpdateTripRequest
        {
            public int RouteId { get; set; }

            public int BusId { get; set; }

            public DateTime TripDate { get; set; }

            public string DepartureTime { get; set; }
                = "08:00";

            public string? TripCode { get; set; }

            public string Status { get; set; }
                = "ACTIVE";

            public bool IsActive { get; set; }
                = true;
        }
        public class CreateTripRequest
        {
            public int RouteId { get; set; }
            public int BusId { get; set; }
            public DateTime TripDate { get; set; }
            public string DepartureTime { get; set; } = "08:00";
            public string? TripCode { get; set; }
            public string? Note { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost("trip")]
        public async Task<IActionResult> CreateTrip(
            [FromBody] CreateTripRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "ADMIN",
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được phép tạo chuyến."
                });
            }

            if (req == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ."
                });
            }

            if (req.RouteId <= 0 || req.BusId <= 0)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn tuyến và xe."
                });
            }

            var tripDate = req.TripDate.Date;

            if (tripDate < DateTime.Today)
            {
                return BadRequest(new
                {
                    message = "Ngày chạy không được ở quá khứ."
                });
            }

            if (!TimeSpan.TryParse(
                    req.DepartureTime,
                    out var departureTime))
            {
                return BadRequest(new
                {
                    message =
                        "Giờ khởi hành không hợp lệ."
                });
            }

            if (!string.IsNullOrWhiteSpace(req.TripCode))
            {
                var requestedTripCode =
                    req.TripCode.Trim();

                var duplicateTripCode =
                    await _context.Trips.AnyAsync(x =>
                        x.TripCode == requestedTripCode);

                if (duplicateTripCode)
                {
                    return Conflict(new
                    {
                        message =
                            "Mã chuyến đã tồn tại."
                    });
                }
            }

            var route =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x => x.Id == req.RouteId);

            if (route == null)
            {
                return NotFound(new
                {
                    message = "Tuyến không tồn tại."
                });
            }

            if (!route.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến đang ngừng hoạt động."
                });
            }

            var bus =
                await _context.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(
                        x => x.Id == req.BusId);

            if (bus == null)
            {
                return NotFound(new
                {
                    message = "Xe không tồn tại."
                });
            }

            if (!bus.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Xe đang ngừng hoạt động."
                });
            }

            if (!bus.Seats.Any(x => x.IsActive))
            {
                return BadRequest(new
                {
                    message =
                        "Xe chưa có ghế hoạt động."
                });
            }

            var routeStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId == req.RouteId)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            if (routeStops.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến chưa có trạm dừng."
                });
            }

            var conflict =
                await _context.Trips
                    .AnyAsync(x =>
                        x.BusId == req.BusId &&
                        x.TripDate == tripDate &&
                        x.DepartureTime == departureTime &&
                        x.IsActive);

            if (conflict)
            {
                return Conflict(new
                {
                    message =
                        "Xe đã có chuyến khác cùng ngày và giờ khởi hành."
                });
            }

            var maxTravelMinutes =
                routeStops.Max(
                    x => x.TravelTimeFromStartMinutes);

            var arrivalTime =
                departureTime.Add(
                    TimeSpan.FromMinutes(
                        Math.Max(
                            1,
                            maxTravelMinutes)));

            var tripCode =
                string.IsNullOrWhiteSpace(
                    req.TripCode)
                    ? $"TRP-{tripDate:yyyyMMdd}-{departureTime:hhmm}-{Guid.NewGuid():N}"[..24]
                    : req.TripCode.Trim();

            var codeExists =
                await _context.Trips
                    .AnyAsync(
                        x => x.TripCode == tripCode);

            if (codeExists)
            {
                return Conflict(new
                {
                    message =
                        "Mã chuyến đã tồn tại."
                });
            }

            var trip =
                new Trip
                {
                    RouteId =
                        req.RouteId,

                    BusId =
                        req.BusId,

                    RecurringScheduleId =
                        null,

                    TripCode =
                        tripCode,

                    TripDate =
                        tripDate,

                    DepartureTime =
                        departureTime,

                    ArrivalTime =
                        arrivalTime,

                    Status =
                        req.IsActive
                            ? "ACTIVE"
                            : "CANCELLED",

                    TotalSeats =
                        bus.SeatCount,

                    AvailableSeats =
                        bus.SeatCount,

                    BookedSeats =
                        0,

                    IsActive =
                        req.IsActive,

                    Note =
                        string.IsNullOrWhiteSpace(
                            req.Note)
                            ? null
                            : req.Note.Trim()
                };

            _context.Trips.Add(trip);

            await _context.SaveChangesAsync();

            foreach (var routeStop in routeStops)
            {
                var arrival =
                    departureTime.Add(
                        TimeSpan.FromMinutes(
                            routeStop.TravelTimeFromStartMinutes));

                var isLast =
                    routeStop.StopOrder ==
                    routeStops.Max(x => x.StopOrder);

                var departure =
                    isLast
                        ? arrival
                        : arrival.Add(
                            TimeSpan.FromMinutes(5));

                _context.TripSchedules.Add(
                    new TripSchedule
                    {
                        TripId =
                            trip.Id,

                        StopId =
                            routeStop.StopId,

                        StopOrder =
                            routeStop.StopOrder,

                        ArrivalTime =
                            arrival,

                        DepartureTime =
                            departure,

                        Status =
                            req.IsActive
                                ? "SCHEDULED"
                                : "CANCELLED"
                    });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã tạo lịch không cố định.",

                tripId =
                    trip.Id,

                tripCode =
                    trip.TripCode
            });
        }

        [HttpPut("trip/{tripId:int}")]
        public async Task<IActionResult>
            UpdateTrip(
                int tripId,
                [FromBody]
                UpdateTripRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Chỉ Manager được phép sửa lịch trình."
                    });
            }

            var trip =
                await _context.Trips
                    .FirstOrDefaultAsync(
                        x => x.Id == tripId);

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy chuyến xe."
                });
            }

            if (trip.RecurringScheduleId.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "Đây là chuyến được sinh từ lịch cố định. " +
                        "Hãy sửa lịch cố định thay vì sửa từng chuyến."
                });
            }

            var route =
                await _context.BusRoutes
                    .FirstOrDefaultAsync(
                        x => x.Id == req.RouteId);

            if (route == null)
            {
                return NotFound(new
                {
                    message =
                        "Tuyến không tồn tại."
                });
            }

            if (!route.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến đang ngừng hoạt động."
                });
            }

            var bus =
                await _context.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(
                        x => x.Id == req.BusId);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Xe không tồn tại."
                });
            }

            if (!bus.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Xe đang ngừng hoạt động."
                });
            }

            var activeSeatCount =
                bus.Seats.Count(x => x.IsActive);

            if (activeSeatCount <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Xe chưa có ghế hoạt động."
                });
            }

            if (!TimeSpan.TryParse(
                    req.DepartureTime,
                    out var departureTime))
            {
                return BadRequest(new
                {
                    message =
                        "Giờ khởi hành không hợp lệ."
                });
            }

            var routeStops =
                await _context.RouteStops
                    .Where(x =>
                        x.RouteId ==
                            req.RouteId)
                    .OrderBy(x =>
                        x.StopOrder)
                    .ToListAsync();

            if (routeStops.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "Tuyến chưa có trạm dừng."
                });
            }

            var maxTravelMinutes =
                routeStops.Max(
                    x =>
                        x.TravelTimeFromStartMinutes);

            trip.RouteId =
                req.RouteId;

            trip.BusId =
                req.BusId;

            trip.TripDate =
                req.TripDate.Date;

            trip.DepartureTime =
                departureTime;

            trip.ArrivalTime =
                departureTime.Add(
                    TimeSpan.FromMinutes(
                        Math.Max(
                            1,
                            maxTravelMinutes)));

            trip.Status =
                string.IsNullOrWhiteSpace(
                    req.Status)
                    ? "ACTIVE"
                    : req.Status
                        .Trim()
                        .ToUpperInvariant();

            trip.IsActive =
                req.IsActive;

            trip.TotalSeats =
                activeSeatCount;

            if (trip.BookedSeats <= 0)
            {
                trip.AvailableSeats =
                    activeSeatCount;
                trip.BookedSeats = 0;
            }
            else
            {
                trip.AvailableSeats =
                    Math.Max(
                        0,
                        activeSeatCount - trip.BookedSeats);
            }

            if (!string.IsNullOrWhiteSpace(
                    req.TripCode))
            {
                trip.TripCode =
                    req.TripCode.Trim();
            }

            var oldSchedules =
                await _context.TripSchedules
                    .Where(x =>
                        x.TripId == tripId)
                    .ToListAsync();

            _context.TripSchedules
                .RemoveRange(oldSchedules);

            var newSchedules =
                new List<TripSchedule>();

            for (
                int index = 0;
                index < routeStops.Count;
                index++)
            {
                var routeStop =
                    routeStops[index];

                var arrival =
                    departureTime.Add(
                        TimeSpan.FromMinutes(
                            routeStop
                                .TravelTimeFromStartMinutes));

                var isLast =
                    index ==
                    routeStops.Count - 1;

                var departure =
                    isLast
                        ? arrival
                        : arrival.Add(
                            TimeSpan.FromMinutes(5));

                newSchedules.Add(
                    new TripSchedule
                    {
                        TripId =
                            tripId,

                        StopId =
                            routeStop.StopId,

                        StopOrder =
                            routeStop.StopOrder,

                        ArrivalTime =
                            arrival,

                        DepartureTime =
                            departure,

                        Status =
                            "SCHEDULED"
                    });
            }

            _context.TripSchedules
                .AddRange(newSchedules);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã cập nhật chuyến."
            });
        }

        [HttpDelete("trip/{tripId:int}")]
        public async Task<IActionResult>
            DeleteTrip(int tripId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "ADMIN", "MANAGER"))
            {
                return StatusCode(
                    403,
                    new
                    {
                        message =
                            "Chỉ Manager được phép xóa lịch trình."
                    });
            }

            var trip =
                await _context.Trips
                    .FirstOrDefaultAsync(
                        x => x.Id == tripId);

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy chuyến xe."
                });
            }

            if (trip.RecurringScheduleId.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "Không thể xóa riêng chuyến sinh tự động. " +
                        "Hãy sửa hoặc ngừng lịch cố định."
                });
            }

            var schedules =
                await _context.TripSchedules
                    .Where(x =>
                        x.TripId == tripId)
                    .ToListAsync();

            _context.TripSchedules
                .RemoveRange(schedules);

            _context.Trips.Remove(trip);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã xóa chuyến."
            });
        }
    }
}