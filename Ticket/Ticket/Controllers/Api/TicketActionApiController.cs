using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticket.DTOs;
using Ticket.Service;
using Ticket.Services;

namespace Ticket.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketActionsApiController : ControllerBase
    {
        private readonly RefundService _refundService;
        private readonly TicketExchangeService _exchangeService;

        public TicketActionsApiController(RefundService refundService, TicketExchangeService exchangeService)
        {
            _refundService = refundService;
            _exchangeService = exchangeService;
        }

        // Endpoint cho US-112: Hủy vé & Hoàn tiền
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelTicket([FromBody] CancelTicketDto request)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _refundService.CancelTicketAsync(request.TicketId, userId, request.Reason);

            return BadRequest(result);
        }

        // Endpoint cho US-113: Đổi vé & Tính chênh lệch
        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeTicket([FromBody] ExchangeTicketDto request)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _exchangeService.ExchangeTicketAsync(request, userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}