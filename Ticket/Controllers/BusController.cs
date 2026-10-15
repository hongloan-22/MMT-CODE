using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;

        public BusController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization)
        {
            _context = context;
            _authorization = authorization;
        }

        // =====================================================
        // GET: api/Bus
        // ADMIN + MANAGER
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetBuses()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền quản lý đội xe."
                });
            }

            var buses =
                await _context.Buses
                    .AsNoTracking()
                    .Include(x => x.StaffAssignments)
                        .ThenInclude(x => x.User)
                    .OrderBy(x => x.BusCode)
                    .ToListAsync();

            var result =
                buses.Select(bus => new
                {
                    id = bus.Id,

                    busCode =
                        bus.BusCode,

                    licensePlate =
                        bus.LicensePlate,

                    seatCount =
                        bus.SeatCount,

                    isActive =
                        bus.IsActive,

                    driver =
                        bus.StaffAssignments
                            .Where(x =>
                                x.IsActive &&
                                x.AssignmentType == "DRIVER" &&
                                x.User != null)
                            .Select(x => new
                            {
                                assignmentId = x.Id,
                                userId = x.UserId,
                                fullName = x.User!.FullName,
                                email = x.User.Email,
                                phone = x.User.PhoneEncrypted
                            })
                            .FirstOrDefault(),

                    attendants =
                        bus.StaffAssignments
                            .Where(x =>
                                x.IsActive &&
                                x.AssignmentType == "ATTENDANT" &&
                                x.User != null)
                            .Select(x => new
                            {
                                assignmentId = x.Id,
                                userId = x.UserId,
                                fullName = x.User!.FullName,
                                email = x.User.Email,
                                phone = x.User.PhoneEncrypted
                            })
                            .ToList(),

                    totalStaff =
                        bus.StaffAssignments.Count(
                            x => x.IsActive)
                });

            return Ok(result);
        }

        // =====================================================
        // GET: api/Bus/active-for-schedule
        // Danh sách xe đang hoạt động và có ghế hoạt động
        // =====================================================

        [HttpGet("active-for-schedule")]
        public async Task<IActionResult> GetActiveBusesForSchedule()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền xem danh sách xe lập lịch."
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

        // =====================================================
        // GET: api/Bus/{id}
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBus(
            int id)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền xem đội xe."
                });
            }

            var bus =
                await _context.Buses
                    .AsNoTracking()
                    .Include(x => x.StaffAssignments)
                        .ThenInclude(x => x.User)
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy xe."
                });
            }

            return Ok(new
            {
                id = bus.Id,
                busCode = bus.BusCode,
                licensePlate = bus.LicensePlate,
                seatCount = bus.SeatCount,
                isActive = bus.IsActive,

                staff =
                    bus.StaffAssignments
                        .Where(x => x.IsActive)
                        .Select(x => new
                        {
                            assignmentId = x.Id,
                            userId = x.UserId,
                            fullName =
                                x.User != null
                                    ? x.User.FullName
                                    : "",
                            email =
                                x.User != null
                                    ? x.User.Email
                                    : "",
                            assignmentType =
                                x.AssignmentType
                        })
                        .ToList()
            });
        }

        // =====================================================
        // GET: api/Bus/staff
        //
        // Lấy toàn bộ người đang có Role = DRIVER
        // =====================================================

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Bạn không có quyền xem danh sách nhân sự xe."
                });
            }

            var staff =
                await _context.Users
                    .AsNoTracking()
                    .Include(x => x.Role)
                    .Include(x => x.BusStaffAssignments)
                        .ThenInclude(x => x.Bus)
                    .Where(
                        x =>
                            x.IsActive &&
                            x.Status == "ACTIVE" &&
                            x.Role != null &&
                            x.Role.RoleCode == "DRIVER")
                    .OrderBy(x => x.FullName)
                    .ToListAsync();

            var result =
                staff.Select(user =>
                {
                    var assignment =
                        user.BusStaffAssignments
                            .FirstOrDefault(
                                x => x.IsActive);

                    return new
                    {
                        userId = user.UserId,

                        fullName =
                            user.FullName,

                        email =
                            user.Email,

                        phone =
                            user.PhoneEncrypted,

                        roleCode =
                            user.Role!.RoleCode,

                        roleName =
                            user.Role.RoleName,

                        assignment =
                            assignment == null
                                ? null
                                : new
                                {
                                    id =
                                        assignment.Id,

                                    busId =
                                        assignment.BusId,

                                    busCode =
                                        assignment.Bus != null
                                            ? assignment.Bus.BusCode
                                            : "",

                                    assignmentType =
                                        assignment.AssignmentType
                                }
                    };
                });

            return Ok(result);
        }

        // =====================================================
        // CREATE BUS
        // POST: api/Bus
        // =====================================================

        public class CreateBusRequest
        {
            public string BusCode { get; set; } =
                string.Empty;

            public string LicensePlate { get; set; } =
                string.Empty;

            public int SeatCount { get; set; } = 21;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBus(
            [FromBody]
            CreateBusRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được tạo xe."
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

            var busCode =
                req.BusCode
                    .Trim()
                    .ToUpperInvariant();

            var licensePlate =
                req.LicensePlate
                    .Trim()
                    .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(busCode))
            {
                return BadRequest(new
                {
                    message =
                        "Mã xe không được để trống."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    licensePlate))
            {
                return BadRequest(new
                {
                    message =
                        "Biển số xe không được để trống."
                });
            }

            if (req.SeatCount < 5 ||
                req.SeatCount > 100)
            {
                return BadRequest(new
                {
                    message =
                        "Số ghế phải từ 5 đến 100."
                });
            }

            if (await _context.Buses.AnyAsync(
                    x => x.BusCode == busCode))
            {
                return Conflict(new
                {
                    message =
                        "Mã xe đã tồn tại."
                });
            }

            if (await _context.Buses.AnyAsync(
                    x =>
                        x.LicensePlate ==
                        licensePlate))
            {
                return Conflict(new
                {
                    message =
                        "Biển số xe đã tồn tại."
                });
            }

            var bus =
                new Bus
                {
                    BusCode =
                        busCode,

                    LicensePlate =
                        licensePlate,

                    SeatCount =
                        req.SeatCount,

                    IsActive =
                        true
                };

            _context.Buses.Add(bus);

            await _context.SaveChangesAsync();

            // =================================================
            // TẠO GHẾ
            // =================================================

            var seats =
                new List<BusSeat>();

            var seatIndex = 1;

            while (seatIndex <=
                   req.SeatCount)
            {
                var row =
                    (seatIndex - 1) / 4 + 1;

                var column =
                    (seatIndex - 1) % 4;

                var columnCode =
                    ((char)('A' + column))
                    .ToString();

                seats.Add(
                    new BusSeat
                    {
                        BusId =
                            bus.Id,

                        SeatCode =
                            $"{columnCode}{row:00}",

                        SeatType =
                            "NORMAL",

                        IsActive =
                            true
                    });

                seatIndex++;
            }

            _context.BusSeats.AddRange(
                seats);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Tạo xe thành công.",

                busId =
                    bus.Id,

                busCode =
                    bus.BusCode
            });
        }

        // =====================================================
        // UPDATE BUS
        // PUT: api/Bus/{id}
        // =====================================================

        public class UpdateBusRequest
        {
            public string BusCode { get; set; } =
                string.Empty;

            public string LicensePlate { get; set; } =
                string.Empty;

            public bool IsActive { get; set; } = true;
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBus(
            int id,
            [FromBody]
            UpdateBusRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được sửa xe."
                });
            }

            var bus =
                await _context.Buses
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy xe."
                });
            }

            var busCode =
                req.BusCode.Trim()
                    .ToUpperInvariant();

            var licensePlate =
                req.LicensePlate.Trim()
                    .ToUpperInvariant();

            var duplicateCode =
                await _context.Buses.AnyAsync(
                    x =>
                        x.Id != id &&
                        x.BusCode == busCode);

            if (duplicateCode)
            {
                return Conflict(new
                {
                    message =
                        "Mã xe đã tồn tại."
                });
            }

            var duplicatePlate =
                await _context.Buses.AnyAsync(
                    x =>
                        x.Id != id &&
                        x.LicensePlate ==
                            licensePlate);

            if (duplicatePlate)
            {
                return Conflict(new
                {
                    message =
                        "Biển số xe đã tồn tại."
                });
            }

            bus.BusCode =
                busCode;

            bus.LicensePlate =
                licensePlate;

            bus.IsActive =
                req.IsActive;

            if (!bus.IsActive)
            {
                var assignments =
                    await _context.BusStaffAssignments
                        .Where(
                            x =>
                                x.BusId == id &&
                                x.IsActive)
                        .ToListAsync();

                foreach (var assignment in assignments)
                {
                    assignment.IsActive =
                        false;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Cập nhật xe thành công."
            });
        }

        // =====================================================
        // DEACTIVATE BUS
        // DELETE: api/Bus/{id}
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBus(
            int id)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được phép ngừng xe."
                });
            }

            var bus =
                await _context.Buses
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy xe."
                });
            }

            bus.IsActive =
                false;

            var assignments =
                await _context.BusStaffAssignments
                    .Where(
                        x =>
                            x.BusId == id &&
                            x.IsActive)
                    .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive =
                    false;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã ngừng hoạt động xe."
            });
        }

        // =====================================================
        // ASSIGN STAFF
        // POST: api/Bus/{busId}/staff
        // =====================================================

        public class AssignStaffRequest
        {
            public string UserId { get; set; } =
                string.Empty;

            public string AssignmentType { get; set; } =
                "DRIVER";
        }

        [HttpPost("{busId:int}/staff")]
        public async Task<IActionResult> AssignStaff(
            int busId,
            [FromBody]
            AssignStaffRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được phân công nhân sự."
                });
            }

            var bus =
                await _context.Buses
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == busId &&
                            x.IsActive);

            if (bus == null)
            {
                return NotFound(new
                {
                    message =
                        "Xe không tồn tại hoặc đang ngừng hoạt động."
                });
            }

            var type =
                req.AssignmentType
                    .Trim()
                    .ToUpperInvariant();

            if (type != "DRIVER" &&
                type != "ATTENDANT")
            {
                return BadRequest(new
                {
                    message =
                        "Loại phân công không hợp lệ."
                });
            }

            var user =
                await _context.Users
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(
                        x =>
                            x.UserId ==
                                req.UserId &&
                            x.IsActive &&
                            x.Status ==
                                "ACTIVE");

            if (user == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy nhân sự."
                });
            }

            if (user.Role == null ||
                user.Role.RoleCode != "DRIVER")
            {
                return BadRequest(new
                {
                    message =
                        "Chỉ tài khoản được phân quyền DRIVER mới được phân công lên xe."
                });
            }

            var existingUserAssignment =
                await _context.BusStaffAssignments
                    .FirstOrDefaultAsync(
                        x =>
                            x.UserId ==
                                user.UserId &&
                            x.IsActive);

            if (existingUserAssignment != null)
            {
                return Conflict(new
                {
                    message =
                        "Nhân sự này đã được phân công cho một xe khác."
                });
            }

            if (type == "DRIVER")
            {
                var existingDriver =
                    await _context.BusStaffAssignments
                        .AnyAsync(
                            x =>
                                x.BusId == busId &&
                                x.AssignmentType ==
                                    "DRIVER" &&
                                x.IsActive);

                if (existingDriver)
                {
                    return Conflict(new
                    {
                        message =
                            "Xe này đã có tài xế."
                    });
                }
            }

            var assignment =
                new BusStaffAssignment
                {
                    BusId =
                        busId,

                    UserId =
                        user.UserId,

                    AssignmentType =
                        type,

                    AssignedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true
                };

            _context.BusStaffAssignments.Add(
                assignment);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    type == "DRIVER"
                        ? "Đã phân công tài xế cho xe."
                        : "Đã phân công phụ xe cho xe.",

                assignmentId =
                    assignment.Id
            });
        }

        // =====================================================
        // REMOVE STAFF
        // DELETE: api/Bus/{busId}/staff/{assignmentId}
        // =====================================================

        [HttpDelete(
            "{busId:int}/staff/{assignmentId:int}")]
        public async Task<IActionResult> RemoveStaff(
            int busId,
            int assignmentId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request,
                    "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message =
                        "Chỉ Admin hoặc Manager được thay đổi phân công."
                });
            }

            var assignment =
                await _context.BusStaffAssignments
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                                assignmentId &&
                            x.BusId ==
                                busId &&
                            x.IsActive);

            if (assignment == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy phân công."
                });
            }

            assignment.IsActive =
                false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Đã hủy phân công."
            });
        }
    }
}