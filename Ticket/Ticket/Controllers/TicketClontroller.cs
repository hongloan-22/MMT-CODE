using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Ticket.Data;              // ✅ THÊM: để dùng ApplicationDbContext
using Ticket.DTOs;
using Ticket.Services;

namespace Ticket.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ApplicationDbContext _context;   // ✅ THÊM

        public TicketsController(ITicketService ticketService, ApplicationDbContext context)  // ✅ THÊM param
        {
            _ticketService = ticketService;
            _context = context;                            // ✅ THÊM
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelTicket(int id, [FromBody] CancelTicket request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "Vui lòng nhập lý do hủy vé." });

            var isSuccess = await _ticketService.CancelTicketAsync(id, userId, request.Reason);

            if (!isSuccess)
                return BadRequest(new { message = "Không thể hủy vé." });

            return Ok(new { message = "Hủy vé thành công." });
        }

        // ✅ Endpoint fallback: lấy vé Booked mới nhất của user
        [HttpGet("latest-booked")]
        public async Task<IActionResult> GetLatestBooked([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "userId là bắt buộc" });

            var ticket = await _context.Tickets
                .Where(t => t.UserId == userId && (int)t.Status == 1)
                .OrderByDescending(t => t.Id)
                .Select(t => new { ticketId = t.Id })   // ✅ Chỉ dùng field chắc chắn có
                .FirstOrDefaultAsync();

            if (ticket == null)
                return NotFound(new { message = "Không có vé đang hiệu lực" });

            return Ok(ticket);
        }
    }
}