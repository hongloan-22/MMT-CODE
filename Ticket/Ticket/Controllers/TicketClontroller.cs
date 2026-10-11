using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Ticket.DTOs;
using Ticket.Services;

namespace Ticket.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelTicket(int id, [FromBody] CancelTicketDto request)
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
    }
}