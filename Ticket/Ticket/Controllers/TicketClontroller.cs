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
        public async Task<IActionResult> CancelTicket(int id, [FromBody] CancelTicket request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

#pragma warning disable CS8604 // Possible null reference argument.
            var isSuccess = await _ticketService.CancelTicketAsync(id, userId, request.Reason);
#pragma warning restore CS8604 // Possible null reference argument.
            if (!isSuccess) return BadRequest(new { message = "Không thể hủy vé." });

            return Ok(new { message = "Hủy vé thành công." });
        }
    }
}