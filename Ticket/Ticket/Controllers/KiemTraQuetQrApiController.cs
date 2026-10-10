using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace Ticket.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KiemTraQuetQrApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KiemTraQuetQrApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // HOA KỲ-128: KIỂM TRA TÍNH HỢP LỆ QUÉT QR, SAI QR, SAI CHUYẾN VÀ ĐÃ SỬ DỤNG
        // Endpoint: POST /api/KiemTraQuetQrApi/KiemTraHopLeQuetQr
        // =========================================================================
        [HttpPost("KiemTraHopLeQuetQr")]
        public async Task<IActionResult> KiemTraHopLeQuetQr([FromBody] KiemTraQuetQrRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MaQrHoacMaVe))
            {
                return BadRequest(new { success = false, message = "Mã QR hoặc dữ liệu quét không được để trống!" });
            }

            // 1. Kiểm tra tồn tại Mã QR trong CSDL (Xử lý trường hợp: SAI QR)
            var veDienTu = await _context.ElectronicTickets
                .Include(t => t.BusSchedule)
                .FirstOrDefaultAsync(t => t.QrCodeData == request.MaQrHoacMaVe || t.TicketCode == request.MaQrHoacMaVe);

            if (veDienTu == null)
            {
                return NotFound(new
                {
                    success = false,
                    maLoi = "SAI_MA_QR",
                    message = "LỖI: Mã QR không hợp lệ hoặc không tồn tại trong hệ thống (Sai mã QR)!"
                });
            }

            // 2. Kiểm tra trạng thái vé (Xử lý trường hợp: VÉ ĐÃ SỬ DỤNG)
            if (veDienTu.IsQrScanned || veDienTu.TransactionStatus == "Used")
            {
                return BadRequest(new
                {
                    success = false,
                    maLoi = "VE_DA_SU_DUNG",
                    thoiGianQuetTruoc = veDienTu.QrScannedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    message = $"LỖI: Vé này ĐÃ ĐƯỢC SỬ DỤNG để soát vé trước đó vào lúc {veDienTu.QrScannedAt:yyyy-MM-dd HH:mm:ss}!"
                });
            }

            // 3. Kiểm tra thông tin chuyến xe (Xử lý trường hợp: SAI CHUYẾN)
            if (request.MaChuyenXeHienTai > 0 && veDienTu.BusScheduleId != request.MaChuyenXeHienTai)
            {
                return BadRequest(new
                {
                    success = false,
                    maLoi = "SAI_CHUYEN_XE",
                    chuyenXeVenTu = veDienTu.BusScheduleId,
                    chuyenXeHienTai = request.MaChuyenXeHienTai,
                    message = $"LỖI: Vé không hợp lệ cho chuyến xe này (Sai chuyến)! Vé này thuộc Chuyến ID: {veDienTu.BusScheduleId}."
                });
            }

            // 4. Nếu tất cả đều hợp lệ -> Đánh dấu vé đã soát thành công (HỢP LỆ)
            veDienTu.IsQrScanned = true;
            veDienTu.QrScannedAt = DateTime.Now;
            veDienTu.TransactionStatus = "Used";

            _context.ElectronicTickets.Update(veDienTu);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                maLoi = "HOP_LE",
                maVe = veDienTu.TicketCode,
                maChuyenXe = veDienTu.BusScheduleId,
                thoiGianSoatVe = veDienTu.QrScannedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                message = "XÁC NHẬN: Quét mã QR thành công! Vé hợp lệ, cho phép hành khách lên xe."
            });
        }
    }

    public class KiemTraQuetQrRequest
    {
        public string MaQrHoacMaVe { get; set; } = string.Empty; // Mã QR hoặc TicketCode quét được
        public int MaChuyenXeHienTai { get; set; } // ID chuyến xe mà tài xế đang soát vé
    }
}