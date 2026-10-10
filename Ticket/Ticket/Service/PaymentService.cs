using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ticket.Models;

namespace Ticket.Service
{
    // ============================================================
    // US-64 + US-65: PaymentService
    // - US-64: Tạo CSDL giao dịch, sinh mã tham chiếu nội bộ
    // - US-65: Tích hợp sandbox VNPay, MoMo, ZaloPay
    // ============================================================

    public class PaymentService : IPaymentService
    {
        // In-memory store (tương tự SeatBookingService - phù hợp dự án học thuật)
        private static readonly ConcurrentDictionary<string, Payment> _payments = new();
        private static int _sequenceCounter = 0;
        private static readonly object _paymentCreationLock = new();

        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly ISeatBookingService _seatBookingService;

        // ===========================================================
        // CẤU HÌNH SANDBOX CÁC CỔNG THANH TOÁN
        // Giá trị lấy từ appsettings.json hoặc biến môi trường
        // ===========================================================

        // VNPay Sandbox
        private string VNPayTmnCode => _config["Payment:VNPay:TmnCode"] ?? "SANDBOX_TMN_CODE";
        private string VNPayHashSecret => _config["Payment:VNPay:HashSecret"] ?? "SANDBOX_HASH_SECRET";
        private string VNPayPaymentUrl => _config["Payment:VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private string VNPayReturnUrl => _config["Payment:VNPay:ReturnUrl"] ?? "https://localhost:5001/api/payment/vnpay/return";

        // MoMo Sandbox
        private string MoMoPartnerCode => _config["Payment:MoMo:PartnerCode"] ?? "MOMO_SANDBOX";
        private string MoMoAccessKey => _config["Payment:MoMo:AccessKey"] ?? "MOMO_ACCESS_KEY";
        private string MoMoSecretKey => _config["Payment:MoMo:SecretKey"] ?? "MOMO_SECRET_KEY";
        private string MoMoPaymentUrl => _config["Payment:MoMo:PaymentUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
        private string MoMoReturnUrl => _config["Payment:MoMo:ReturnUrl"] ?? "https://localhost:5001/api/payment/momo/return";
        private string MoMoIpnUrl => _config["Payment:MoMo:IpnUrl"] ?? "https://localhost:5001/api/payment/momo/ipn";

        // ZaloPay Sandbox
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

        // ============================================================
        // US-64: Sinh mã tham chiếu nội bộ
        // Định dạng: SBGD-YYYYMMDD-NNNNN
        // ============================================================
        private static string GenerateTransactionRef()
        {
            int seq = Interlocked.Increment(ref _sequenceCounter);
            string date = DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd"); // Giờ VN
            return $"SBGD-{date}-{seq:D5}";
        }

        // ============================================================
        // US-64: Tạo giao dịch và lưu CSDL
        // ============================================================
        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.HoldId))
            {
                return new CreatePaymentResponseDto { Success = false, Message = "HoldId không được để trống." };
            }

            // Chặn tạo nhiều giao dịch cho cùng một lượt giữ chỗ khi giao dịch trước vẫn đang xử lý/đã thành công.
            // Lock bảo đảm kiểm tra + tạo bản ghi không bị race trong nhiều request đồng thời.
            lock (_paymentCreationLock)
            {
                var existing = _payments.Values.FirstOrDefault(p =>
                    string.Equals(p.HoldId, request.HoldId, StringComparison.OrdinalIgnoreCase) &&
                    p.Status is PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.Success);
                if (existing != null)
                {
                    return new CreatePaymentResponseDto
                    {
                        Success = existing.Status != PaymentStatus.Success,
                        TransactionRef = existing.TransactionRef,
                        Method = existing.Method.ToString(),
                        PaymentUrl = existing.ReturnUrl,
                        ExpiresAt = existing.ExpiresAt,
                        Message = existing.Status == PaymentStatus.Success
                            ? "Lượt giữ chỗ này đã được thanh toán thành công; không tạo giao dịch hoặc vé mới."
                            : "Đã có giao dịch đang chờ xử lý cho lượt giữ chỗ này; sử dụng giao dịch hiện có."
                    };
                }
            }

            // Xác định phương thức thanh toán
            if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            {
                return new CreatePaymentResponseDto
                {
                    Success = false,
                    Message = $"Phương thức thanh toán '{request.Method}' không hợp lệ. Chấp nhận: VNPay, MoMo, ZaloPay, Bank."
                };
            }

            // Sinh mã tham chiếu nội bộ (US-64)
            string transactionRef = GenerateTransactionRef();
            string gatewayOrderId = transactionRef.Replace("-", ""); // Mã gửi cho cổng TT (không có dấu gạch ngang)

            // Tạo bản ghi Payment (US-64)
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
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Giao dịch hết hạn sau 15 phút
                UserIpAddress = request.ClientIp
            };

            lock (_paymentCreationLock)
            {
                // Kiểm tra lần hai bên trong lock để tránh hai request đồng thời cùng tạo giao dịch.
                var existing = _payments.Values.FirstOrDefault(p =>
                    string.Equals(p.HoldId, request.HoldId, StringComparison.OrdinalIgnoreCase) &&
                    p.Status is PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.Success);
                if (existing != null)
                {
                    return new CreatePaymentResponseDto
                    {
                        Success = existing.Status != PaymentStatus.Success,
                        TransactionRef = existing.TransactionRef,
                        Method = existing.Method.ToString(),
                        PaymentUrl = existing.ReturnUrl,
                        ExpiresAt = existing.ExpiresAt,
                        Message = "Đã có giao dịch cho lượt giữ chỗ này; không tạo giao dịch trùng."
                    };
                }
                _payments[transactionRef] = payment;
            }

            _logger.LogInformation("[Payment US-64] Đã tạo giao dịch {TransRef} | Method={Method} | Amount={Amount}đ | HoldId={HoldId}",
                transactionRef, method, request.Amount, request.HoldId);

            // Tạo URL cổng thanh toán theo phương thức (US-65)
            string paymentUrl = method switch
            {
                PaymentMethod.VNPay => await BuildVNPayUrlAsync(payment),
                PaymentMethod.MoMo => await BuildMoMoUrlAsync(payment),
                PaymentMethod.ZaloPay => await BuildZaloPayUrlAsync(payment),
                PaymentMethod.BankTransfer => BuildBankTransferInfo(payment),
                _ => string.Empty
            };

            // Cập nhật trạng thái sang Processing
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
        // US-64: Truy vấn trạng thái giao dịch
        // ============================================================
        public Task<PaymentStatusResponseDto?> GetPaymentStatusAsync(string transactionRef)
        {
            if (!_payments.TryGetValue(transactionRef, out var payment))
                return Task.FromResult<PaymentStatusResponseDto?>(null);

            // Tự động cập nhật sang Cancelled nếu hết hạn
            if (payment.IsExpired)
            {
                payment.Status = PaymentStatus.Cancelled;
                payment.Note = "Hết thời gian thanh toán";
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
        // US-65: Xử lý callback từ VNPay Sandbox
        // Tài liệu: https://sandbox.vnpayment.vn/apis/docs/
        // ============================================================
        public Task<bool> ProcessVNPayCallbackAsync(VNPayCallbackDto callback)
        {
            if (callback.vnp_TxnRef == null) return Task.FromResult(false);

            // Tìm giao dịch theo GatewayOrderId
            var payment = _payments.Values.FirstOrDefault(p => p.GatewayOrderId == callback.vnp_TxnRef);
            if (payment == null)
            {
                _logger.LogWarning("[VNPay Callback] Không tìm thấy giao dịch với vnp_TxnRef={TxnRef}", callback.vnp_TxnRef);
                return Task.FromResult(false);
            }

            // Xác thực chữ ký (HMAC-SHA512) - US-65 bảo mật
            bool isValidSignature = VerifyVNPaySignature(callback, VNPayHashSecret);
            if (!isValidSignature)
            {
                _logger.LogWarning("[VNPay Callback] Chữ ký không hợp lệ cho giao dịch {TransRef}", payment.TransactionRef);
                return Task.FromResult(false);
            }

            lock (payment)
            {
                // Idempotency: callback lặp sau thành công không được cập nhật trạng thái hay xác nhận Booking lần nữa.
                if (payment.Status == PaymentStatus.Success)
                {
                    _logger.LogInformation("[Payment] Bỏ qua callback lặp cho giao dịch đã thành công {TransRef}", payment.TransactionRef);
                    return Task.FromResult(true);
                }

                // Cập nhật thông tin từ VNPay
                payment.GatewayTransactionId = callback.vnp_TransactionNo;
                payment.GatewayResponseCode = callback.vnp_ResponseCode;
                payment.GatewayResponseMessage = callback.vnp_ResponseCode == "00" ? "Giao dịch thành công" : $"Lỗi mã {callback.vnp_ResponseCode}";
                payment.GatewaySignature = callback.vnp_SecureHash;
                payment.CompletedAt = DateTime.UtcNow;

                // Cập nhật trạng thái
                payment.Status = (callback.vnp_ResponseCode == "00" && callback.vnp_TransactionStatus == "00")
                    ? PaymentStatus.Success
                    : PaymentStatus.Failed;

                CompleteBookingIfPaid(payment);
            }

            _logger.LogInformation("[VNPay Callback] Giao dịch {TransRef} -> {Status} | VNPay TxnNo={TxnNo}",
                payment.TransactionRef, payment.Status, callback.vnp_TransactionNo);

            return Task.FromResult(true);
        }

        // ============================================================
        // US-65: Xử lý callback từ MoMo Sandbox
        // Tài liệu: https://developers.momo.vn/v3/docs/payment/api/result-handling/
        // ============================================================
        public Task<bool> ProcessMoMoCallbackAsync(MoMoCallbackDto callback)
        {
            if (callback.orderId == null) return Task.FromResult(false);

            var payment = _payments.Values.FirstOrDefault(p => p.GatewayOrderId == callback.orderId);
            if (payment == null)
            {
                _logger.LogWarning("[MoMo Callback] Không tìm thấy giao dịch với orderId={OrderId}", callback.orderId);
                return Task.FromResult(false);
            }

            // Xác thực chữ ký HMAC-SHA256 từ MoMo
            bool isValidSignature = VerifyMoMoSignature(callback, MoMoSecretKey);
            if (!isValidSignature)
            {
                _logger.LogWarning("[MoMo Callback] Chữ ký không hợp lệ cho giao dịch {TransRef}", payment.TransactionRef);
                return Task.FromResult(false);
            }

            lock (payment)
            {
                // Idempotency: callback lặp sau thành công không được cập nhật trạng thái hay xác nhận Booking lần nữa.
                if (payment.Status == PaymentStatus.Success)
                {
                    _logger.LogInformation("[Payment] Bỏ qua callback lặp cho giao dịch đã thành công {TransRef}", payment.TransactionRef);
                    return Task.FromResult(true);
                }

                payment.GatewayTransactionId = callback.transId.ToString();
                payment.GatewayResponseCode = callback.resultCode.ToString();
                payment.GatewayResponseMessage = callback.message;
                payment.GatewaySignature = callback.signature;
                payment.CompletedAt = DateTime.UtcNow;

                // resultCode = 0 là thành công (MoMo)
                payment.Status = callback.resultCode == 0
                    ? PaymentStatus.Success
                    : PaymentStatus.Failed;

                CompleteBookingIfPaid(payment);
            }

            _logger.LogInformation("[MoMo Callback] Giao dịch {TransRef} -> {Status} | MoMo TransId={TransId}",
                payment.TransactionRef, payment.Status, callback.transId);

            return Task.FromResult(true);
        }

        // ============================================================
        // US-65: Xử lý callback từ ZaloPay Sandbox
        // Tài liệu: https://docs.zalopay.vn/v2/
        // ============================================================
        public Task<bool> ProcessZaloPayCallbackAsync(ZaloPayCallbackDto callback)
        {
            if (callback.data == null || callback.mac == null) return Task.FromResult(false);

            // Xác thực MAC (HMAC-SHA256 với Key2)
            string computedMac = ComputeHmacSha256(callback.data, ZaloPayKey2);
            if (!computedMac.Equals(callback.mac, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[ZaloPay Callback] MAC không hợp lệ");
                return Task.FromResult(false);
            }

            // Parse data JSON
            ZaloPayCallbackData? data;
            try
            {
                data = JsonSerializer.Deserialize<ZaloPayCallbackData>(callback.data);
            }
            catch
            {
                _logger.LogError("[ZaloPay Callback] Không parse được data JSON");
                return Task.FromResult(false);
            }

            if (data?.app_trans_id == null) return Task.FromResult(false);

            // ZaloPay dùng app_trans_id dạng YYMMDD_OrderId
            // Ta tìm theo phần sau dấu "_"
            string orderIdPart = data.app_trans_id.Contains('_')
                ? data.app_trans_id.Split('_')[1]
                : data.app_trans_id;

            var payment = _payments.Values.FirstOrDefault(p =>
                p.GatewayOrderId != null && p.GatewayOrderId.Contains(orderIdPart, StringComparison.OrdinalIgnoreCase));

            if (payment == null)
            {
                _logger.LogWarning("[ZaloPay Callback] Không tìm thấy giao dịch với app_trans_id={TransId}", data.app_trans_id);
                return Task.FromResult(false);
            }

            lock (payment)
            {
                // Idempotency: callback lặp sau thành công không được cập nhật trạng thái hay xác nhận Booking lần nữa.
                if (payment.Status == PaymentStatus.Success)
                {
                    _logger.LogInformation("[Payment] Bỏ qua callback lặp cho giao dịch đã thành công {TransRef}", payment.TransactionRef);
                    return Task.FromResult(true);
                }

                payment.GatewayTransactionId = data.zp_trans_id.ToString();
                payment.GatewayResponseCode = callback.type.ToString();
                payment.GatewayResponseMessage = callback.type == 1 ? "Thanh toán thành công" : "Thanh toán thất bại";
                payment.GatewaySignature = callback.mac;
                payment.CompletedAt = DateTime.UtcNow;

                // type = 1 là thành công (ZaloPay)
                payment.Status = callback.type == 1
                    ? PaymentStatus.Success
                    : PaymentStatus.Failed;

                CompleteBookingIfPaid(payment);
            }

            _logger.LogInformation("[ZaloPay Callback] Giao dịch {TransRef} -> {Status} | ZaloPay ZpTransId={ZpTransId}",
                payment.TransactionRef, payment.Status, data.zp_trans_id);

            return Task.FromResult(true);
        }

        // ============================================================
        // Hủy giao dịch
        // ============================================================
        // Xác nhận đặt vé sau khi callback thanh toán đã được xác thực.
        private void CompleteBookingIfPaid(Payment payment)
        {
            if (payment.Status != PaymentStatus.Success)
                return;

            if (string.IsNullOrWhiteSpace(payment.HoldId))
            {
                _logger.LogError("[Payment] Giao dịch {TransRef} thành công nhưng thiếu HoldId.", payment.TransactionRef);
                return;
            }

            bool confirmed = _seatBookingService.ConfirmHold(payment.HoldId, out string message);
            if (confirmed)
                _logger.LogInformation("[Booking] {TransRef}: {Message}", payment.TransactionRef, message);
            else
                _logger.LogError("[Booking] Không thể xác nhận HoldId={HoldId} sau khi thanh toán {TransRef} thành công: {Message}",
                    payment.HoldId, payment.TransactionRef, message);
        }

        public Task<bool> CancelPaymentAsync(string transactionRef, string reason = "")
        {
            if (!_payments.TryGetValue(transactionRef, out var payment))
                return Task.FromResult(false);

            if (payment.IsFinished)
                return Task.FromResult(false);

            payment.Status = PaymentStatus.Cancelled;
            payment.CompletedAt = DateTime.UtcNow;
            payment.Note = string.IsNullOrWhiteSpace(reason) ? "Đã huỷ giao dịch" : reason;

            _logger.LogInformation("[Payment] Đã huỷ giao dịch {TransRef} | Lý do: {Reason}", transactionRef, reason);
            return Task.FromResult(true);
        }

        // ============================================================
        // US-65: Xây dựng URL thanh toán VNPay Sandbox
        // Theo chuẩn VNPay HMAC-SHA512
        // ============================================================
        private Task<string> BuildVNPayUrlAsync(Payment payment)
        {
            string orderInfo = $"Thanh toan ve xe SmartBus - {payment.TransactionRef}";
            string amount = ((long)(payment.Amount * 100)).ToString(); // VNPay yêu cầu nhân 100
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

            // Xây dựng chuỗi query để ký
            string queryString = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            string rawSignature = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={kv.Value}"));

            // Ký HMAC-SHA512
            string secureHash = ComputeHmacSha512(rawSignature, VNPayHashSecret);

            string url = $"{VNPayPaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";
            return Task.FromResult(url);
        }

        // ============================================================
        // US-65: Xây dựng URL thanh toán MoMo Sandbox
        // Theo chuẩn MoMo API v2 (HMAC-SHA256)
        // ============================================================
        private async Task<string> BuildMoMoUrlAsync(Payment payment)
        {
            string requestId = Guid.NewGuid().ToString("N");
            string orderInfo = $"Thanh toan ve xe SmartBus - {payment.TransactionRef}";
            long amount = (long)payment.Amount;

            // Chuỗi raw để ký theo đúng thứ tự MoMo yêu cầu
            string rawSignature = $"accessKey={MoMoAccessKey}" +
                                  $"&amount={amount}" +
                                  $"&extraData=" +
                                  $"&ipnUrl={MoMoIpnUrl}" +
                                  $"&orderId={payment.GatewayOrderId}" +
                                  $"&orderInfo={orderInfo}" +
                                  $"&partnerCode={MoMoPartnerCode}" +
                                  $"&redirectUrl={MoMoReturnUrl}" +
                                  $"&requestId={requestId}" +
                                  $"&requestType=payWithMethod";

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

                _logger.LogWarning("[MoMo] Không lấy được payUrl. Response: {Response}", responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MoMo] Lỗi khi gọi API sandbox");
            }

            // Fallback: Trả về URL sandbox demo nếu không gọi được API
            return $"https://test-payment.momo.vn/pay?orderId={payment.GatewayOrderId}&amount={amount}";
        }

        // ============================================================
        // US-65: Xây dựng URL thanh toán ZaloPay Sandbox
        // Theo chuẩn ZaloPay API v2 (HMAC-SHA256)
        // ============================================================
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

            // Chuỗi raw để ký: app_id|app_trans_id|app_user|amount|app_time|embed_data|item
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

                _logger.LogWarning("[ZaloPay] Không lấy được order_url. Response: {Response}", responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ZaloPay] Lỗi khi gọi API sandbox");
            }

            // Fallback: Trả về URL sandbox demo nếu không gọi được API
            return $"https://sb-openapi.zalopay.vn/pay?app_trans_id={appTransId}";
        }

        // ============================================================
        // Thông tin chuyển khoản ngân hàng (Bank Transfer)
        // ============================================================
        private static string BuildBankTransferInfo(Payment payment)
        {
            return $"bank://transfer?account=9704229239874057&bank=NCB" +
                   $"&amount={(long)payment.Amount}" +
                   $"&note=SBGD{payment.GatewayOrderId}" +
                   $"&name=SmartBus";
        }

        // ============================================================
        // Tiện ích: Xác thực chữ ký VNPay (HMAC-SHA512)
        // ============================================================
        private static bool VerifyVNPaySignature(VNPayCallbackDto callback, string hashSecret)
        {
            // Thu thập tất cả tham số trừ vnp_SecureHash
            var sortedParams = new SortedDictionary<string, string>();
            var props = typeof(VNPayCallbackDto).GetProperties();
            foreach (var prop in props)
            {
                if (prop.Name == nameof(VNPayCallbackDto.vnp_SecureHash)) continue;
                var val = prop.GetValue(callback)?.ToString();
                if (!string.IsNullOrEmpty(val))
                    sortedParams[prop.Name] = val;
            }

            string rawData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            string computedHash = ComputeHmacSha512(rawData, hashSecret);
            return computedHash.Equals(callback.vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // Tiện ích: Xác thực chữ ký MoMo (HMAC-SHA256)
        // ============================================================
        private static bool VerifyMoMoSignature(MoMoCallbackDto callback, string secretKey)
        {
            string rawSignature = $"accessKey={callback.partnerCode}" +
                                  $"&amount={callback.amount}" +
                                  $"&extraData={callback.extraData}" +
                                  $"&message={callback.message}" +
                                  $"&orderId={callback.orderId}" +
                                  $"&orderInfo={callback.orderInfo}" +
                                  $"&orderType={callback.orderType}" +
                                  $"&partnerCode={callback.partnerCode}" +
                                  $"&payType={callback.payType}" +
                                  $"&requestId={callback.requestId}" +
                                  $"&responseTime={callback.responseTime}" +
                                  $"&resultCode={callback.resultCode}" +
                                  $"&transId={callback.transId}";

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
