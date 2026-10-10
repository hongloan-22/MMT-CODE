using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ticket.Models;

namespace Ticket.Service
{
    // ============================================================
    // US-64, US-65, US-97, US-100, US-101, US-103, US-107
    // ============================================================

    public class PaymentService : IPaymentService
    {
        private static readonly ConcurrentDictionary<string, Payment> _payments = new();
        private static int _sequenceCounter = 0;

        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly ISeatBookingService _seatBookingService; // [US-103] Inject SeatBookingService

        // CẤU HÌNH SANDBOX CÁC CỔNG THANH TOÁN
        private string VNPayTmnCode => _config["Payment:VNPay:TmnCode"] ?? "SANDBOX_TMN_CODE";
        private string VNPayHashSecret => _config["Payment:VNPay:HashSecret"] ?? "SANDBOX_HASH_SECRET";
        private string VNPayPaymentUrl => _config["Payment:VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private string VNPayReturnUrl => _config["Payment:VNPay:ReturnUrl"] ?? "https://localhost:5001/api/payment/vnpay/return";

        private string MoMoPartnerCode => _config["Payment:MoMo:PartnerCode"] ?? "MOMO_SANDBOX";
        private string MoMoAccessKey => _config["Payment:MoMo:AccessKey"] ?? "MOMO_ACCESS_KEY";
        private string MoMoSecretKey => _config["Payment:MoMo:SecretKey"] ?? "MOMO_SECRET_KEY";
        private string MoMoPaymentUrl => _config["Payment:MoMo:PaymentUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
        private string MoMoReturnUrl => _config["Payment:MoMo:ReturnUrl"] ?? "https://localhost:5001/api/payment/momo/return";
        private string MoMoIpnUrl => _config["Payment:MoMo:IpnUrl"] ?? "https://localhost:5001/api/payment/momo/ipn";

        private string ZaloPayAppId => _config["Payment:ZaloPay:AppId"] ?? "2553";
        private string ZaloPayKey1 => _config["Payment:ZaloPay:Key1"] ?? "ZALOPAY_KEY1";
        private string ZaloPayKey2 => _config["Payment:ZaloPay:Key2"] ?? "ZALOPAY_KEY2";
        private string ZaloPayPaymentUrl => _config["Payment:ZaloPay:PaymentUrl"] ?? "https://sb-openapi.zalopay.vn/v2/create";
        private string ZaloPayCallbackUrl => _config["Payment:ZaloPay:CallbackUrl"] ?? "https://localhost:5001/api/payment/zalopay/callback";

        public PaymentService(IConfiguration config, ILogger<PaymentService> logger, ISeatBookingService seatBookingService)
        {
            _config = config;
            _logger = logger;
            _seatBookingService = seatBookingService;
        }

        private static string GenerateTransactionRef()
        {
            int seq = Interlocked.Increment(ref _sequenceCounter);
            string date = DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd");
            return $"SBGD-{date}-{seq:D5}";
        }

        // ============================================================
        // US-64 & US-103: Kiểm tra giữ ghế & Tạo giao dịch
        // ============================================================
        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            // [US-103] Kiểm tra tương tác giữa thanh toán và thời gian giữ ghế
            if (!string.IsNullOrEmpty(request.HoldId))
            {
                var holdStatus = _seatBookingService.CheckHoldStatus(request.HoldId);
                if (holdStatus.Status != "ACTIVE")
                {
                    return new CreatePaymentResponseDto
                    {
                        Success = false,
                        Message = $"Lượt giữ ghế ({request.HoldId}) không hợp lệ hoặc đã hết hạn. Vui lòng chọn ghế lại!"
                    };
                }
            }

            if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            {
                return new CreatePaymentResponseDto
                {
                    Success = false,
                    Message = $"Phương thức thanh toán '{request.Method}' không hợp lệ. Chấp nhận: VNPay, MoMo, ZaloPay, Bank."
                };
            }

            string transactionRef = GenerateTransactionRef();
            string gatewayOrderId = transactionRef.Replace("-", "");

            var payment = new Payment
            {
                TransactionRef = transactionRef,
                GatewayOrderId = gatewayOrderId,
                HoldId = request.HoldId,
                TripCode = request.TripCode,
                SeatIds = string.Join(",", request.SeatIds),
                UserId = request.UserId,
                Method = method,
                Amount = request.Amount,
                Status = PaymentStatus.Pending, // [US-97] Trạng thái PENDING
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserIpAddress = request.ClientIp
            };

            _payments[transactionRef] = payment;

            _logger.LogInformation("[Payment US-64] Đã tạo giao dịch {TransRef} | Method={Method} | Amount={Amount}đ | HoldId={HoldId}",
                transactionRef, method, request.Amount, request.HoldId);

            string paymentUrl = method switch
            {
                PaymentMethod.VNPay => await BuildVNPayUrlAsync(payment),
                PaymentMethod.MoMo => await BuildMoMoUrlAsync(payment),
                PaymentMethod.ZaloPay => await BuildZaloPayUrlAsync(payment),
                PaymentMethod.BankTransfer => BuildBankTransferInfo(payment),
                _ => string.Empty
            };

            payment.Status = PaymentStatus.Processing;
            payment.ReturnUrl = paymentUrl;

            return new CreatePaymentResponseDto
            {
                Success = true,
                TransactionRef = transactionRef,
                Method = method.ToString(),
                PaymentUrl = paymentUrl,
                ExpiresAt = payment.ExpiresAt,
                Message = $"Đã tạo giao dịch {transactionRef}. Chuyển hướng đến {method} để thanh toán."
            };
        }

        // ============================================================
        // US-97 & US-103: Truy vấn trạng thái & Tự động hủy nếu hết hạn
        // ============================================================
        public Task<PaymentStatusResponseDto?> GetPaymentStatusAsync(string transactionRef)
        {
            if (!_payments.TryGetValue(transactionRef, out var payment))
                return Task.FromResult<PaymentStatusResponseDto?>(null);

            // [US-103 & US-100] Tự động giải phóng ghế nếu giao dịch hết hạn
            if (payment.IsExpired && (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Processing))
            {
                payment.Status = PaymentStatus.Cancelled; // [US-97] CANCELLED
                payment.Note = "Hết thời gian thanh toán";
                HandlePaymentStatusResult(payment);
            }

            return Task.FromResult<PaymentStatusResponseDto?>(new PaymentStatusResponseDto
            {
                TransactionRef = payment.TransactionRef,
                Status = payment.Status.ToString(),
                Method = payment.Method.ToString(),
                Amount = payment.Amount,
                GatewayTransactionId = payment.GatewayTransactionId,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt,
                Message = GetStatusMessage(payment.Status)
            });
        }

        // ============================================================
        // US-65, US-97, US-100: Callback VNPay
        // ============================================================
        public Task<bool> ProcessVNPayCallbackAsync(VNPayCallbackDto callback)
        {
            if (callback.vnp_TxnRef == null) return Task.FromResult(false);

            var payment = _payments.Values.FirstOrDefault(p => p.GatewayOrderId == callback.vnp_TxnRef);
            if (payment == null) return Task.FromResult(false);

            bool isValidSignature = VerifyVNPaySignature(callback, VNPayHashSecret);
            if (!isValidSignature) return Task.FromResult(false);

            payment.GatewayTransactionId = callback.vnp_TransactionNo;
            payment.GatewayResponseCode = callback.vnp_ResponseCode;
            payment.GatewayResponseMessage = callback.vnp_ResponseCode == "00" ? "Giao dịch thành công" : $"Lỗi mã {callback.vnp_ResponseCode}";
            payment.GatewaySignature = callback.vnp_SecureHash;
            payment.CompletedAt = DateTime.UtcNow;

            // [US-97] Cập nhật SUCCESS / FAILED
            payment.Status = (callback.vnp_ResponseCode == "00" && callback.vnp_TransactionStatus == "00")
                ? PaymentStatus.Success
                : PaymentStatus.Failed;

            // [US-100] Xử lý hậu thanh toán (Giải phóng ghế nếu thất bại)
            HandlePaymentStatusResult(payment);

            return Task.FromResult(true);
        }

        // ============================================================
        // US-65, US-97, US-100: Callback MoMo
        // ============================================================
        public Task<bool> ProcessMoMoCallbackAsync(MoMoCallbackDto callback)
        {
            if (callback.orderId == null) return Task.FromResult(false);

            var payment = _payments.Values.FirstOrDefault(p => p.GatewayOrderId == callback.orderId);
            if (payment == null) return Task.FromResult(false);

            bool isValidSignature = VerifyMoMoSignature(callback, MoMoSecretKey);
            if (!isValidSignature) return Task.FromResult(false);

            payment.GatewayTransactionId = callback.transId.ToString();
            payment.GatewayResponseCode = callback.resultCode.ToString();
            payment.GatewayResponseMessage = callback.message;
            payment.GatewaySignature = callback.signature;
            payment.CompletedAt = DateTime.UtcNow;

            // [US-97] Cập nhật SUCCESS / FAILED
            payment.Status = callback.resultCode == 0 ? PaymentStatus.Success : PaymentStatus.Failed;

            // [US-100] Giải phóng ghế nếu thất bại
            HandlePaymentStatusResult(payment);

            return Task.FromResult(true);
        }

        // ============================================================
        // US-65, US-97, US-100: Callback ZaloPay
        // ============================================================
        public Task<bool> ProcessZaloPayCallbackAsync(ZaloPayCallbackDto callback)
        {
            if (callback.data == null || callback.mac == null) return Task.FromResult(false);

            string computedMac = ComputeHmacSha256(callback.data, ZaloPayKey2);
            if (!computedMac.Equals(callback.mac, StringComparison.OrdinalIgnoreCase)) return Task.FromResult(false);

            ZaloPayCallbackData? data;
            try { data = JsonSerializer.Deserialize<ZaloPayCallbackData>(callback.data); }
            catch { return Task.FromResult(false); }

            if (data?.app_trans_id == null) return Task.FromResult(false);

            string orderIdPart = data.app_trans_id.Contains('_') ? data.app_trans_id.Split('_')[1] : data.app_trans_id;
            var payment = _payments.Values.FirstOrDefault(p => p.GatewayOrderId != null && p.GatewayOrderId.Contains(orderIdPart, StringComparison.OrdinalIgnoreCase));

            if (payment == null) return Task.FromResult(false);

            payment.GatewayTransactionId = data.zp_trans_id.ToString();
            payment.GatewayResponseCode = callback.type.ToString();
            payment.GatewayResponseMessage = callback.type == 1 ? "Thanh toán thành công" : "Thanh toán thất bại";
            payment.GatewaySignature = callback.mac;
            payment.CompletedAt = DateTime.UtcNow;

            // [US-97] Cập nhật SUCCESS / FAILED
            payment.Status = callback.type == 1 ? PaymentStatus.Success : PaymentStatus.Failed;

            // [US-100] Giải phóng ghế nếu thất bại
            HandlePaymentStatusResult(payment);

            return Task.FromResult(true);
        }

        // ============================================================
        // US-101: Người dùng hủy thanh toán
        // ============================================================
        public Task<bool> CancelPaymentAsync(string transactionRef, string reason = "")
        {
            if (!_payments.TryGetValue(transactionRef, out var payment))
                return Task.FromResult(false);

            if (payment.IsFinished)
                return Task.FromResult(false);

            payment.Status = PaymentStatus.Cancelled; // [US-97] CANCELLED
            payment.CompletedAt = DateTime.UtcNow;
            payment.Note = string.IsNullOrWhiteSpace(reason) ? "Người dùng hủy thanh toán" : reason;

            // [US-101 & US-100] Giải phóng ghế ngay lập tức khi người dùng hủy
            HandlePaymentStatusResult(payment);

            _logger.LogInformation("[Payment US-101] Đã huỷ giao dịch {TransRef} | Lý do: {Reason}", transactionRef, reason);
            return Task.FromResult(true);
        }

        // ============================================================
        // HELPER METHOD: Xử lý giải phóng ghế dựa trên trạng thái thanh toán
        // (Giải quyết US-97, US-100, US-101, US-103, US-107)
        // ============================================================
        private void HandlePaymentStatusResult(Payment payment)
        {
            if (payment.Status == PaymentStatus.Failed || payment.Status == PaymentStatus.Cancelled)
            {
                if (!string.IsNullOrEmpty(payment.HoldId))
                {
                    bool released = _seatBookingService.ReleaseHold(payment.HoldId, out string releaseMsg);
                    _logger.LogInformation("[Payment US-100/101] Thanh toán {TransRef} kết thúc với trạng thái {Status} -> Giải phóng HoldId '{HoldId}': {Msg}",
                        payment.TransactionRef, payment.Status, payment.HoldId, releaseMsg);
                }
            }
            else if (payment.Status == PaymentStatus.Success)
            {
                _logger.LogInformation("[Payment US-97] Thanh toán {TransRef} THÀNH CÔNG cho HoldId '{HoldId}'.",
                    payment.TransactionRef, payment.HoldId);
            }
        }

        // Các hàm phụ trợ dựng URL và verify signature
        private Task<string> BuildVNPayUrlAsync(Payment payment)
        {
            string orderInfo = $"Thanh toan ve xe SmartBus - {payment.TransactionRef}";
            string amount = ((long)(payment.Amount * 100)).ToString();
            string createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
            string expireDate = payment.ExpiresAt.AddHours(7).ToString("yyyyMMddHHmmss");

            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", VNPayTmnCode },
                { "vnp_Amount", amount },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", payment.GatewayOrderId! },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", VNPayReturnUrl },
                { "vnp_IpAddr", payment.UserIpAddress ?? "127.0.0.1" },
                { "vnp_CreateDate", createDate },
                { "vnp_ExpireDate", expireDate }
            };

            string queryString = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            string rawSignature = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={kv.Value}"));
            string secureHash = ComputeHmacSha512(rawSignature, VNPayHashSecret);

            return Task.FromResult($"{VNPayPaymentUrl}?{queryString}&vnp_SecureHash={secureHash}");
        }

        private async Task<string> BuildMoMoUrlAsync(Payment payment)
        {
            string requestId = Guid.NewGuid().ToString("N");
            string orderInfo = $"Thanh toan ve xe SmartBus - {payment.TransactionRef}";
            long amount = (long)payment.Amount;

            string rawSignature = $"accessKey={MoMoAccessKey}&amount={amount}&extraData=&ipnUrl={MoMoIpnUrl}&orderId={payment.GatewayOrderId}&orderInfo={orderInfo}&partnerCode={MoMoPartnerCode}&redirectUrl={MoMoReturnUrl}&requestId={requestId}&requestType=payWithMethod";
            string signature = ComputeHmacSha256(rawSignature, MoMoSecretKey);

            var requestBody = new
            {
                partnerCode = MoMoPartnerCode,
                partnerName = "SmartBus Ticket",
                storeId = "SmartBusStore01",
                requestId = requestId,
                amount = amount,
                orderId = payment.GatewayOrderId,
                orderInfo = orderInfo,
                redirectUrl = MoMoReturnUrl,
                ipnUrl = MoMoIpnUrl,
                lang = "vi",
                requestType = "payWithMethod",
                autoCapture = true,
                extraData = "",
                signature = signature
            };

            try
            {
                using var client = new HttpClient();
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(MoMoPaymentUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("payUrl", out var payUrl))
                {
                    return payUrl.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MoMo] Lỗi khi gọi API sandbox");
            }

            return $"https://test-payment.momo.vn/pay?orderId={payment.GatewayOrderId}&amount={amount}";
        }

        private async Task<string> BuildZaloPayUrlAsync(Payment payment)
        {
            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string appTransId = $"{DateTime.UtcNow.AddHours(7):yyMMdd}_{payment.GatewayOrderId}";
            string embedData = JsonSerializer.Serialize(new { redirecturl = ZaloPayCallbackUrl });
            string item = JsonSerializer.Serialize(new[]
            {
                new { itemid = payment.TripCode, itemname = $"Ve xe {payment.TripCode}", itemprice = (long)payment.Amount, itemquantity = 1 }
            });
            long amount = (long)payment.Amount;

            string rawSignature = $"{ZaloPayAppId}|{appTransId}|{payment.UserId ?? "guest"}|{amount}|{appTime}|{embedData}|{item}";
            string mac = ComputeHmacSha256(rawSignature, ZaloPayKey1);

            var requestBody = new
            {
                app_id = ZaloPayAppId,
                app_user = payment.UserId ?? "guest",
                app_time = appTime,
                amount = amount,
                app_trans_id = appTransId,
                bank_code = "zalopayapp",
                description = $"SmartBus - Thanh toan cho don hang {payment.TransactionRef}",
                embed_data = embedData,
                item = item,
                callback_url = ZaloPayCallbackUrl,
                mac = mac
            };

            try
            {
                using var client = new HttpClient();
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ZaloPayPaymentUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("order_url", out var orderUrl))
                {
                    return orderUrl.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ZaloPay] Lỗi khi gọi API sandbox");
            }

            return $"https://sb-openapi.zalopay.vn/pay?app_trans_id={appTransId}";
        }

        private static string BuildBankTransferInfo(Payment payment)
        {
            return $"bank://transfer?account=9704229239874057&bank=NCB&amount={(long)payment.Amount}&note=SBGD{payment.GatewayOrderId}&name=SmartBus";
        }

        private static bool VerifyVNPaySignature(VNPayCallbackDto callback, string hashSecret)
        {
            var sortedParams = new SortedDictionary<string, string>();
            var props = typeof(VNPayCallbackDto).GetProperties();
            foreach (var prop in props)
            {
                if (prop.Name == nameof(VNPayCallbackDto.vnp_SecureHash)) continue;
                var val = prop.GetValue(callback)?.ToString();
                if (!string.IsNullOrEmpty(val)) sortedParams[prop.Name] = val;
            }

            string rawData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            string computedHash = ComputeHmacSha512(rawData, hashSecret);
            return computedHash.Equals(callback.vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static bool VerifyMoMoSignature(MoMoCallbackDto callback, string secretKey)
        {
            string rawSignature = $"accessKey={callback.partnerCode}&amount={callback.amount}&extraData={callback.extraData}&message={callback.message}&orderId={callback.orderId}&orderInfo={callback.orderInfo}&orderType={callback.orderType}&partnerCode={callback.partnerCode}&payType={callback.payType}&requestId={callback.requestId}&responseTime={callback.responseTime}&resultCode={callback.resultCode}&transId={callback.transId}";
            string computed = ComputeHmacSha256(rawSignature, secretKey);
            return computed.Equals(callback.signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha512(string data, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static string GetStatusMessage(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "Đang chờ thanh toán",
            PaymentStatus.Processing => "Đang xử lý tại cổng thanh toán",
            PaymentStatus.Success => "Thanh toán thành công",
            PaymentStatus.Failed => "Thanh toán thất bại",
            PaymentStatus.Cancelled => "Giao dịch đã bị huỷ",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => "Không xác định"
        };
    }
}