using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ticket.Models;
using Ticket.Data;

namespace Ticket.Controllers
{
    // Yêu cầu bắt buộc người dùng phải đăng nhập mới được vào Controller này
    [Authorize]
    public class SmartBusController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // Bổ sung DbContext

        public SmartBusController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context) // Inject DbContext
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserIndex()
        {
            var users = await _userManager.Users.ToListAsync();
            return View("User/Index", users);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new SelectList(new[] { "Admin", "Quản lý", "Tài xế", "Hành khách" });
            return View("User/Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserId = model.UserId,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }

                    await _userManager.AddToRoleAsync(user, model.Role);
                    TempData["Success"] = $"Tạo tài khoản thành công với vai trò {model.Role}!";
                    return RedirectToAction(nameof(UserIndex));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(new[] { "Admin", "Quản lý", "Tài xế", "Hành khách" }, model.Role);
            return View("User/Create", model);
        }

        [Authorize(Roles = "Admin,Quản lý")]
        public IActionResult RouteIndex()
        {
            // Logic hiển thị danh sách tuyến xe
            return View("Route/Index");
        }

        [Authorize(Roles = "Admin,Quản lý")]
        public IActionResult ManageSchedules()
        {
            // Logic lập lịch trình & phân công điều xe
            return View("Route/ManageSchedules");
        }

        [Authorize(Roles = "Admin,Tài xế")]
        public IActionResult ScanQR()
        {
            // Màn hình quét mã QR soát vé
            return View("Driver/ScanQR");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Tài xế")]
        public IActionResult ProcessTicketScan(string qrCode)
        {
            // Logic xác thực mã QR vé xe
            return Json(new { success = true, message = "Soát vé thành công!" });
        }

        [Authorize(Roles = "Hành khách")]
        public IActionResult BookTicket()
        {
            // Màn hình tra cứu và đặt vé
            return View("Passenger/BookTicket");
        }

        [Authorize(Roles = "Hành khách")]
        public IActionResult MyTickets()
        {
            // Xem danh sách vé điện tử dạng mã QR đã mua
            return View("Passenger/MyTickets");
        }

        // =========================================================================
        // US-34: BỔ SUNG CÁC API TRUY VẤN DANH SÁCH TUYẾN / CHUYẾN PHÙ HỢP
        // =========================================================================

        // 1. Lấy danh sách tuyến xe đang hoạt động
        [HttpGet]
        [AllowAnonymous] // Hoặc bỏ nếu bắt buộc phải đăng nhập
        public async Task<IActionResult> GetActiveRoutes()
        {
            var routes = await _context.BusRoutes
                .Where(r => r.IsActive)
                .Select(r => new
                {
                    r.Id,
                    r.RouteName,
                    r.TotalDistanceKm,
                    r.EstimatedDuration
                })
                .ToListAsync();

            return Json(routes);
        }

        // 2. Lấy danh sách các chuyến xe (Lịch trình) theo ID Tuyến
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetSchedulesByRoute(int routeId)
        {
            var schedules = await _context.BusSchedules
                .Where(s => s.BusRouteId == routeId && s.DepartureTime >= DateTime.Now)
                .OrderBy(s => s.DepartureTime)
                .Select(s => new
                {
                    s.Id,
                    s.BusRouteId,
                    DepartureTime = s.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                    ArrivalTime = s.ArrivalTime.ToString("yyyy-MM-dd HH:mm"),
                    s.AvailableSeats
                })
                .ToListAsync();

            return Json(schedules);
        }
    }
}