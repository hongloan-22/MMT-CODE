using System;

namespace Ticket.Service
{
    public interface IQrCodeService
    {
        string GenerateQrCodeBase64(string payloadText);
    }

    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeBase64(string payloadText)
        {
            if (string.IsNullOrEmpty(payloadText)) return string.Empty;

            // Mã hóa chuỗi dữ liệu sang URL Safe
            string encodedData = Uri.EscapeDataString(payloadText);

            // Sinh link ảnh QR trực tiếp từ API Google / QRServer
            return $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={encodedData}";
        }
    }
}