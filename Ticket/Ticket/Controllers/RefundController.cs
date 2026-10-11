using Microsoft.AspNetCore.Mvc;
using Ticket.DTOs;
using Ticket.Services;

namespace Ticket.Controllers
{
    [ApiController]
    [Route("api/refunds")]
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly ILogger<RefundController> _logger;

        public RefundController(IRefundService refundService, ILogger<RefundController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        // US-109: User tạo yêu cầu hoàn tiền
        // TODO: Thêm [Authorize] + lấy userId từ JWT khi auth sẵn sàng
        [HttpPost("request")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRefundRequestDto dto)
        {
            // ⚠️ TẠM: nhận userId từ body. Sửa thành JWT khi auth xong.
            if (string.IsNullOrEmpty(dto.UserId))
                return BadRequest(new { success = false, message = "Thiếu userId" });

            try
            {
                var refund = await _refundService.CreateRefundRequestAsync(
                    dto.TicketId, dto.UserId, dto.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Tạo yêu cầu hoàn tiền thành công",
                    data = new
                    {
                        refund.Id,
                        refund.TicketId,
                        refund.Amount,
                        refund.Fee,
                        Status = refund.Status.ToString(),
                        refund.CreatedAt
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // ✅ Không dùng Forbid() vì chưa setup auth scheme
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // US-110: Admin xem danh sách chờ duyệt
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var refunds = await _refundService.GetPendingRefundsAsync();
            return Ok(new { success = true, count = refunds.Count, data = refunds });
        }

        // US-110: Admin duyệt
        // TODO: Thêm [Authorize(Roles = "ADMIN")] khi auth sẵn sàng
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromQuery] string adminUserId)
        {
            if (string.IsNullOrEmpty(adminUserId))
                return BadRequest(new { success = false, message = "Thiếu adminUserId" });

            try
            {
                var ok = await _refundService.ApproveRefundAsync(id, adminUserId);
                return ok
                    ? Ok(new { success = true, message = "Duyệt hoàn tiền thành công" })
                    : BadRequest(new { success = false, message = "Duyệt thất bại" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // US-110: Admin từ chối
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id,
            [FromQuery] string adminUserId,
            [FromBody] RejectRefundDto dto)
        {
            if (string.IsNullOrEmpty(adminUserId))
                return BadRequest(new { success = false, message = "Thiếu adminUserId" });

            try
            {
                var ok = await _refundService.RejectRefundAsync(id, adminUserId, dto.Reason);
                return ok
                    ? Ok(new { success = true, message = "Đã từ chối yêu cầu hoàn tiền" })
                    : BadRequest(new { success = false, message = "Từ chối thất bại" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var refund = await _refundService.GetByIdAsync(id);
            return refund == null
                ? NotFound(new { success = false, message = $"Không tìm thấy refund #{id}" })
                : Ok(new { success = true, data = refund });
        }

        // US-109: User xem refund của mình
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRefunds([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { success = false, message = "Thiếu userId" });

            var refunds = await _refundService.GetByUserIdAsync(userId);
            return Ok(new { success = true, count = refunds.Count, data = refunds });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var refunds = await _refundService.GetAllAsync();
            return Ok(new { success = true, count = refunds.Count, data = refunds });
        }
    }
}