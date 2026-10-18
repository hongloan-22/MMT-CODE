
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.Services;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripStaffAssignmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BackendAuthorizationService _authorization;

        public TripStaffAssignmentsController(
            ApplicationDbContext context,
            BackendAuthorizationService authorization)
        {
            _context = context;
            _authorization = authorization;
        }

        // =====================================================
        // DTO
        // =====================================================

        public class AssignTripStaffRequest
        {
            public string UserId { get; set; } = string.Empty;
        }

        // =====================================================
        // GET: api/TripStaffAssignments/staff?assignmentType=DRIVER
        //
        // Lấy danh sách nhân sự có thể phân công.
        // =====================================================

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff(
            [FromQuery] string assignmentType = "DRIVER")
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền phân công nhân sự."
                });
            }

            var type = NormalizeType(assignmentType);

            if (type == null)
            {
                return BadRequest(new
                {
                    message = "Loại phân công phải là DRIVER hoặc ATTENDANT."
                });
            }

            // Hệ thống hiện tại có RoleCode DRIVER,
            // chưa có RoleCode ATTENDANT.
            // Vì vậy, cả hai loại phân công đều dùng danh sách
            // tài khoản DRIVER cho đến khi bổ sung vai trò ATTENDANT.
            var staff = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .Where(x =>
                    x.IsActive &&
                    x.Status == "ACTIVE" &&
                    x.Role != null &&
                    (
                        x.Role.RoleCode == "DRIVER" ||
                        (
                            type == "ATTENDANT" &&
                            x.Role.RoleCode == "ATTENDANT"
                        )
                    ))
                .OrderBy(x => x.FullName)
                .Select(x => new
                {
                    userId = x.UserId,
                    fullName = x.FullName,
                    email = x.Email,
                    roleCode = x.Role!.RoleCode,
                    roleName = x.Role.RoleName
                })
                .ToListAsync();

            return Ok(staff);
        }

        // =====================================================
        // GET: api/TripStaffAssignments/trips/{tripId}
        //
        // Xem thông tin chuyến và nhân sự đã phân công.
        // =====================================================

        [HttpGet("trips/{tripId:int}")]
        public async Task<IActionResult> GetTripAssignments(
            int tripId)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền xem phân công."
                });
            }

            var trip = await _context.Trips
                .AsNoTracking()
                .Include(x => x.Route)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.Id == tripId);

            if (trip == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chuyến xe."
                });
            }

            var assignments = await _context.TripStaffAssignments
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x!.Role)
                .Where(x =>
                    x.TripId == tripId &&
                    x.IsActive)
                .OrderBy(x => x.AssignmentType)
                .ToListAsync();

            return Ok(new
            {
                trip = new
                {
                    id = trip.Id,
                    tripCode = trip.TripCode,
                    routeId = trip.RouteId,
                    routeCode = trip.Route?.RouteCode,
                    routeName = trip.Route?.RouteName,
                    tripDate = trip.TripDate.ToString("yyyy-MM-dd"),
                    departureTime = trip.DepartureTime.ToString(@"hh\:mm"),
                    arrivalTime = trip.ArrivalTime.HasValue
                        ? trip.ArrivalTime.Value.ToString(@"hh\:mm")
                        : null,
                    busId = trip.BusId,
                    busCode = trip.Bus?.BusCode,
                    licensePlate = trip.Bus?.LicensePlate,
                    status = trip.Status,
                    isActive = trip.IsActive
                },

                assignments = assignments.Select(x => new
                {
                    id = x.Id,
                    tripId = x.TripId,
                    userId = x.UserId,
                    fullName = x.User?.FullName,
                    email = x.User?.Email,
                    roleCode = x.User?.Role?.RoleCode,
                    assignmentType = x.AssignmentType,
                    assignedAt = x.AssignedAt,
                    isActive = x.IsActive
                })
            });
        }

        // =====================================================
        // PUT: api/TripStaffAssignments/trips/{tripId}/{type}
        //
        // Phân công hoặc thay thế nhân sự cho một vai trò
        // trên một chuyến cụ thể.
        //
        // Ví dụ:
        // PUT /api/TripStaffAssignments/trips/1/DRIVER
        // PUT /api/TripStaffAssignments/trips/1/ATTENDANT
        // =====================================================

        [HttpPut("trips/{tripId:int}/{assignmentType}")]
        public async Task<IActionResult> AssignStaff(
            int tripId,
            string assignmentType,
            [FromBody] AssignTripStaffRequest req)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền phân công nhân sự."
                });
            }

            var type = NormalizeType(assignmentType);

            if (type == null)
            {
                return BadRequest(new
                {
                    message = "Loại phân công phải là DRIVER hoặc ATTENDANT."
                });
            }

            var userId = req.UserId?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn nhân sự."
                });
            }

            // Lấy thông tin chuyến, tuyến và lịch các điểm dừng.
            var trip = await _context.Trips
                .Include(x => x.Route)
                .Include(x => x.Schedules)
                .FirstOrDefaultAsync(x => x.Id == tripId);

            if (trip == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chuyến xe."
                });
            }

            if (!trip.IsActive ||
                string.Equals(
                    trip.Status, "CANCELLED",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    trip.Status, "CANCELED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Không thể phân công cho chuyến đã ngừng hoạt động hoặc bị hủy."
                });
            }

            // Tính thời gian bắt đầu và kết thúc của chuyến.
            if (!TryGetTripInterval(trip, out var start, out var end))
            {
                return BadRequest(new
                {
                    message =
                        "Không thể kiểm tra trùng lịch. " +
                        "Chuyến cần có giờ đến, thời lượng tuyến " +
                        "hoặc lịch trình điểm dừng hợp lệ."
                });
            }

            // Kiểm tra nhân sự tồn tại và đang hoạt động.
            var user = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    x.Status == "ACTIVE");

            if (user == null || user.Role == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy nhân sự đang hoạt động."
                });
            }

            var roleCode = user.Role.RoleCode.Trim().ToUpperInvariant();

            // Tương thích với danh sách vai trò hiện tại.
            // Khi chưa có role ATTENDANT, tài khoản DRIVER
            // được sử dụng cho cả loại phân công phụ xe.
            var validRole =
                type == "DRIVER"
                    ? roleCode == "DRIVER"
                    : roleCode == "DRIVER" ||
                      roleCode == "ATTENDANT";

            if (!validRole)
            {
                return BadRequest(new
                {
                    message = type == "DRIVER"
                        ? "Nhân sự phải có vai trò DRIVER."
                        : "Nhân sự phải có vai trò DRIVER hoặc ATTENDANT."
                });
            }

            // Không cho cùng một người đảm nhận cả hai vai trò
            // trên cùng một chuyến.
            var sameTripOtherRole =
                await _context.TripStaffAssignments.AnyAsync(x =>
                    x.TripId == tripId &&
                    x.UserId == userId &&
                    x.IsActive &&
                    x.AssignmentType != type);

            if (sameTripOtherRole)
            {
                return Conflict(new
                {
                    message =
                        "Nhân sự này đã được phân công một vai trò khác trên cùng chuyến."
                });
            }

            // Kiểm tra thời gian làm việc của nhân sự trên
            // những chuyến khác đang được phân công.
            var otherAssignments =
                await _context.TripStaffAssignments
                    .Include(x => x.Trip)
                        .ThenInclude(x => x!.Route)
                    .Include(x => x.Trip)
                        .ThenInclude(x => x!.Schedules)
                    .Where(x =>
                        x.UserId == userId &&
                        x.IsActive &&
                        x.TripId != tripId)
                    .ToListAsync();

            foreach (var assignment in otherAssignments)
            {
                var otherTrip = assignment.Trip;

                if (otherTrip == null ||
                    !otherTrip.IsActive ||
                    string.Equals(
                        otherTrip.Status, "CANCELLED",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        otherTrip.Status, "CANCELED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryGetTripInterval(
                        otherTrip,
                        out var otherStart,
                        out var otherEnd))
                {
                    return Conflict(new
                    {
                        message =
                            $"Không thể xác minh lịch của chuyến " +
                            $"{otherTrip.TripCode}. Hãy bổ sung " +
                            "thời gian kết thúc chuyến trước khi phân công."
                    });
                }

                // Hai khoảng thời gian giao nhau:
                // start < otherEnd && otherStart < end.
                if (start < otherEnd && otherStart < end)
                {
                    return Conflict(new
                    {
                        message =
                            $"Nhân sự {user.FullName} đã có lịch " +
                            $"trên chuyến {otherTrip.TripCode} " +
                            $"({otherStart:dd/MM/yyyy HH:mm} - " +
                            $"{otherEnd:dd/MM/yyyy HH:mm}). " +
                            "Vui lòng chọn nhân sự khác."
                    });
                }
            }

            // Nếu cùng người đã được phân công vào đúng vai trò
            // của chuyến này thì không tạo bản ghi trùng.
            var currentAssignment =
                await _context.TripStaffAssignments
                    .FirstOrDefaultAsync(x =>
                        x.TripId == tripId &&
                        x.AssignmentType == type &&
                        x.IsActive);

            if (currentAssignment != null &&
                currentAssignment.UserId == userId)
            {
                return Ok(new
                {
                    message = "Nhân sự này đã được phân công cho chuyến.",
                    assignmentId = currentAssignment.Id
                });
            }

            // Nếu vai trò này đang có người khác đảm nhận,
            // ngừng phân công cũ rồi ghi nhận người mới.
            if (currentAssignment != null)
            {
                currentAssignment.IsActive = false;
            }

            var newAssignment = new TripStaffAssignment
            {
                TripId = tripId,
                UserId = userId,
                AssignmentType = type,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.TripStaffAssignments.Add(newAssignment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        "Không thể lưu phân công. " +
                        "Có thể vai trò này vừa được phân công " +
                        "hoặc dữ liệu bị trùng. Vui lòng tải lại."
                });
            }

            return Ok(new
            {
                message = type == "DRIVER"
                    ? "Đã phân công tài xế cho chuyến."
                    : "Đã phân công phụ xe cho chuyến.",
                assignmentId = newAssignment.Id,
                tripId = tripId,
                userId = userId,
                assignmentType = type
            });
        }

        // =====================================================
        // DELETE: api/TripStaffAssignments/trips/{tripId}/{type}
        //
        // Bỏ phân công một vai trò trên một chuyến.
        // =====================================================

        [HttpDelete("trips/{tripId:int}/{assignmentType}")]
        public async Task<IActionResult> RemoveStaff(
            int tripId,
            string assignmentType)
        {
            if (!await _authorization.HasAnyRoleAsync(
                    Request, "MANAGER"))
            {
                return StatusCode(403, new
                {
                    message = "Bạn không có quyền thay đổi phân công."
                });
            }

            var type = NormalizeType(assignmentType);

            if (type == null)
            {
                return BadRequest(new
                {
                    message = "Loại phân công không hợp lệ."
                });
            }

            var assignment =
                await _context.TripStaffAssignments
                    .FirstOrDefaultAsync(x =>
                        x.TripId == tripId &&
                        x.AssignmentType == type &&
                        x.IsActive);

            if (assignment == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy phân công đang hoạt động."
                });
            }

            assignment.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã bỏ phân công nhân sự.",
                tripId = tripId,
                assignmentType = type
            });
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string? NormalizeType(string? value)
        {
            var type = value?.Trim().ToUpperInvariant();

            return type == "DRIVER" || type == "ATTENDANT"
                ? type
                : null;
        }

        // Tính khoảng thời gian chuyến xe.
        //
        // Ưu tiên:
        // 1. Giờ đến cụ thể của Trip.
        // 2. Giờ đến tại điểm dừng cuối cùng.
        // 3. Thời lượng dự kiến của tuyến.
        private static bool TryGetTripInterval(
            Trip trip,
            out DateTime start,
            out DateTime end)
        {
            start = trip.TripDate.Date.Add(trip.DepartureTime);
            end = start;

            if (trip.ArrivalTime.HasValue)
            {
                end = trip.TripDate.Date.Add(
                    trip.ArrivalTime.Value);

                if (end <= start)
                {
                    end = end.AddDays(1);
                }

                return end > start;
            }

            var lastStop = trip.Schedules?
                .OrderByDescending(x => x.StopOrder)
                .FirstOrDefault();

            if (lastStop != null &&
                lastStop.ArrivalTime > TimeSpan.Zero)
            {
                end = trip.TripDate.Date.Add(
                    lastStop.ArrivalTime);

                if (end <= start)
                {
                    end = end.AddDays(1);
                }

                if (end > start)
                {
                    return true;
                }
            }

            if (trip.Route != null &&
                trip.Route.EstimatedDuration > TimeSpan.Zero)
            {
                end = start.Add(
                    trip.Route.EstimatedDuration);

                return end > start;
            }

            return false;
        }
    }
}
