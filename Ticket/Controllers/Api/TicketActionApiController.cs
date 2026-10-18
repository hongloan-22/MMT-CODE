using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticket.DTOs;
using Ticket.Service;

namespace Ticket.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketActionsApiController : ControllerBase
    {
        private readonly RefundService _refundService;
        private readonly ExchangeService _exchangeService;

        public TicketActionsApiController(RefundService refundService, ExchangeService exchangeService)
        {
            _refundService = refundService;
            _exchangeService = exchangeService;
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelTicket([FromBody] CancelTicketDto request)
        {
            var userId = Request.Headers["X-User-Id"].ToString();
            var result = await _refundService.CancelTicketAsync(request.TicketId, userId, request.Reason);
            return Ok(result);
        }

        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeTicket([FromBody] ExchangeTicketDto request)
        {
            var userId = Request.Headers["X-User-Id"].ToString();
            var result = await _exchangeService.ExchangeTicketAsync(request.OldTicketId, request.NewTripId, request.NewSeatNumber, userId);
            
            if (!result) return BadRequest(new { Success = false, Message = "Đổi vé thất bại." });
            return Ok(new { Success = true, Message = "Đổi vé thành công." });
        }
    }
}