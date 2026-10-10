using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace Ticket.Service
{
    // ============================================================
    // SPRINT 3 - US06: PaymentService (EF/SQLite)
    // - Tao CSDL giao dich, sinh ma tham chieu noi bo.
    // - Tich hop sandbox VNPay, MoMo, ZaloPay + tien mat.
    // - Chot/nha ghe (SeatHold) theo ket qua thanh toan.
    // - Idempotent voi callback lap.
    // ============================================================

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISeatBookingService _seatBooking;

        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly ISeatBookingService _seatBookingService; // [US-103] Inject SeatBookingService
        // ===========================================================
        // CAU HINH SANDBOX CAC CONG THANH TOAN
        // ===========================================================

        // VNPay Sandbox

        // VNPay Sandbox
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
        public PaymentService(
            ApplicationDbContext context,
            ISeatBookingService seatBooking,
            IConfiguration config,
            ILogger<PaymentService> logger)

        public PaymentService(IConfiguration config, ILogger<PaymentService> logger)
        {
            _context = context;
            _seatBooking = seatBooking;
            _config = config;
            _logger = logger;
            _seatBookingService = seatBookingService;
        // ============================================================
        // Sinh ma tham chieu noi bo: SBGD-YYYYMMDD-NNNNN
        // ============================================================
        private async Task<string> GenerateTransactionCodeAsync()
        // Định dạng: SBGD-YYYYMMDD-NNNNN
            string date = DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd"); // Gio VN
            string prefix = $"SBGD-{date}-";

            string? lastCode = await _context.PaymentTransactions
                .Where(p => p.TransactionCode.StartsWith(prefix))
                .OrderByDescending(p => p.TransactionCode)
                .Select(p => p.TransactionCode)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (lastCode != null)
            {
                string tail = lastCode.Substring(prefix.Length);
                if (int.TryParse(tail, out var n))
                {
                    seq = n + 1;
            int seq = Interlocked.Increment(ref _sequenceCounter);
            string date = DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd"); // Giờ VN
            return $"SBGD-{date}-{seq:D5}";
        }
            }

            return $"{prefix}{seq:D5}";
        }

        // ============================================================
        // US-64 & US-103: Kiểm tra giữ ghế & Tạo giao dịch
            // Xac dinh phuong thuc thanh toan (ho tro alias "Bank")
            if (!TryParseMethod(request.Method, out var method))
        {
            // Xác định phương thức thanh toán
            if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            {
                return new CreatePaymentResponseDto
                {
                    Success = false,
                    Message = $"Phương thức thanh toán '{request.Method}' không hợp lệ. Chấp nhận: VNPay, MoMo, ZaloPay, BankTransfer, Cash."
            string transactionCode = await GenerateTransactionCodeAsync();
            string orderCode = transactionCode.Replace("-", ""); // Ma gui cho cong TT (khong dau gach ngang)

            var payment = new PaymentTransaction
            string transactionRef = GenerateTransactionRef();
            string gatewayOrderId = transactionRef.Replace("-", ""); // Mã gửi cho cổng TT (không có dấu gạch ngang)

            // Tạo bản ghi Payment (US-64)
            var payment = new Payment
            {
                TransactionCode = transactionCode,
                OrderCode = orderCode,
                Provider = method.ToString(),
                HoldId = request.HoldId,
                TripCode = request.TripCode,
                SeatIds = string.Join(",", request.SeatIds),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Giao dich het han sau 15 phut
                Method = method,
                Amount = request.Amount,
                Status = PaymentStatus.Pending, // [US-97] Trạng thái PENDING
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Giao dịch hết hạn sau 15 phút
                UserIpAddress = request.ClientIp
            };

            // Phuong thuc tien mat: thanh cong ngay, khong qua cong thanh toan
            if (method == PaymentMethod.Cash)
            {
                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                payment.CompletedAt = DateTime.UtcNow;
                payment.Note = "Thanh toán tiền mặt tại quầy - thành công ngay";

                if (_seatBooking.ConfirmHold(payment.HoldId, out var holdMessage))
                {
                    _logger.LogInformation("[Payment] Da chot ghe cho Cash {Code} | {Msg}", transactionCode, holdMessage);
                }
                _context.PaymentTransactions.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[Payment] Da tao giao dich tien mat {Code} | Amount={Amount}đ | HoldId={HoldId}",
                    transactionCode, request.Amount, request.HoldId);

                return new CreatePaymentResponseDto
                {
                    Success = true,
                    TransactionCode = transactionCode,
                    Method = method.ToString(),
                    Status = ToStatusString(payment.Status),
                    PaymentUrl = null,
                    ExpiresAt = payment.ExpiresAt,
                    Message = $"Đã tạo giao dịch tiền mặt {transactionCode} và xác nhận thành công."
                };
            }

            // Tao URL cong thanh toan theo phuong thuc
                {
                    _logger.LogWarning("[Payment] Cash {Code} khong chot duoc ghe: {Msg}", transactionCode, holdMessage);
                }

            // Tạo URL cổng thanh toán theo phương thức (US-65)
            string paymentUrl = method switch
            {
                PaymentMethod.VNPay => BuildVNPayUrl(payment),
                PaymentMethod.MoMo => await BuildMoMoUrlAsync(payment),
                PaymentMethod.ZaloPay => await BuildZaloPayUrlAsync(payment),
                PaymentMethod.BankTransfer => BuildBankTransferInfo(payment),
                _ => string.Empty
            };

            payment.Status = PaymentStatus.Processing;
            payment.ReturnUrl = paymentUrl;

            _context.PaymentTransactions.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Payment] Da tao giao dich {Code} | Method={Method} | Amount={Amount}đ | HoldId={HoldId}",
                transactionCode, method, request.Amount, request.HoldId);

            return new CreatePaymentResponseDto
            {
                Success = true,
                TransactionCode = transactionCode,
                Method = method.ToString(),
                Status = ToStatusString(payment.Status),
                PaymentUrl = paymentUrl,
                ExpiresAt = payment.ExpiresAt,
                Message = $"Đã tạo giao dịch {transactionCode}. Chuyển hướng đến {method} để thanh toán."
            };
        }

        // ============================================================
        // US-97 & US-103: Truy vấn trạng thái & Tự động hủy nếu hết hạn
        // ============================================================
            if (payment == null)
                return null;

            // Tu dong cap nhat sang Cancelled neu het han
            if (payment.IsExpired)
            var payment = await _context.PaymentTransactions
                payment.Status = PaymentStatus.Cancelled;
                payment.CompletedAt = DateTime.UtcNow;

                _seatBooking.ReleaseHold(payment.HoldId, out _);
                await _context.SaveChangesAsync();
            // Tự động cập nhật sang Cancelled nếu hết hạn
            if (payment.IsExpired)
            {
                payment.Status = PaymentStatus.Cancelled;
                payment.Note = "Hết thời gian thanh toán";
            }

            return new PaymentStatusResponseDto
            {
                TransactionCode = payment.TransactionCode,
                Status = ToStatusString(payment.Status),
                Method = payment.Method.ToString(),
                Amount = payment.Amount,
                HoldId = payment.HoldId,
                GatewayTransactionId = payment.ProviderTransactionId,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt,
                Message = GetStatusMessage(payment.Status)
            };
        }

        // ============================================================
        // US-65, US-97, US-100: Callback VNPay
            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.OrderCode == callback.vnp_TxnRef);
            if (payment == null)
            {
                _logger.LogWarning("[VNPay Callback] Khong tim thay giao dich voi vnp_TxnRef={TxnRef}", callback.vnp_TxnRef);
                return false;
            }
            if (payment == null)
            // Xac thuc chu ky (HMAC-SHA512)
            if (!VerifyVNPaySignature(callback, VNPayHashSecret))
            {
                _logger.LogWarning("[VNPay Callback] Chu ky khong hop le cho giao dich {Code}", payment.TransactionCode);
                return false;
            }
            bool isValidSignature = VerifyVNPaySignature(callback, VNPayHashSecret);
            // Idempotency: callback lap cho giao dich da ket thuc -> bo qua
            if (payment.IsFinished)
            {
                _logger.LogInformation("[VNPay Callback] Giao dich {Code} da ket thuc ({Status}), bo qua callback lap.",
                    payment.TransactionCode, payment.Status);
                return true;
            }

            payment.ProviderTransactionId = callback.vnp_TransactionNo;
                _logger.LogWarning("[VNPay Callback] Chữ ký không hợp lệ cho giao dịch {TransRef}", payment.TransactionRef);
                return Task.FromResult(false);
            }

            // Cập nhật thông tin từ VNPay
            payment.GatewayTransactionId = callback.vnp_TransactionNo;
            bool isSuccess = callback.vnp_ResponseCode == "00" && callback.vnp_TransactionStatus == "00";
            ApplyFinalStatus(payment, isSuccess);
            payment.CallbackCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[VNPay Callback] Giao dich {Code} -> {Status} | TxnNo={TxnNo}",
                payment.TransactionCode, payment.Status, callback.vnp_TransactionNo);
            payment.Status = (callback.vnp_ResponseCode == "00" && callback.vnp_TransactionStatus == "00")
                ? PaymentStatus.Success
                : PaymentStatus.Failed;

            _logger.LogInformation("[VNPay Callback] Giao dịch {TransRef} -> {Status} | VNPay TxnNo={TxnNo}",
                payment.TransactionRef, payment.Status, callback.vnp_TransactionNo);

            return true;
        }

        // ============================================================
            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.OrderCode == callback.orderId);
            if (payment == null)
            {
                _logger.LogWarning("[MoMo Callback] Khong tim thay giao dich voi orderId={OrderId}", callback.orderId);
                return false;
            }

            if (!VerifyMoMoSignature(callback, MoMoSecretKey))
            {
                _logger.LogWarning("[MoMo Callback] Chu ky khong hop le cho giao dich {Code}", payment.TransactionCode);
                return false;
            }
            var payment = _payments.Values.FirstOrDefault(p => p.GatewayOrderId == callback.orderId);
            if (payment.IsFinished)
            {
                _logger.LogInformation("[MoMo Callback] Giao dich {Code} da ket thuc ({Status}), bo qua callback lap.",
                    payment.TransactionCode, payment.Status);
                return true;
            }
            // Xác thực chữ ký HMAC-SHA256 từ MoMo
            bool isValidSignature = VerifyMoMoSignature(callback, MoMoSecretKey);
            if (!isValidSignature)
            {
                _logger.LogWarning("[MoMo Callback] Chữ ký không hợp lệ cho giao dịch {TransRef}", payment.TransactionRef);
                return Task.FromResult(false);
            }

            payment.ProviderTransactionId = callback.transId.ToString();
            payment.GatewayResponseCode = callback.resultCode.ToString();
            await _context.SaveChangesAsync();
            payment.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("[MoMo Callback] Giao dich {Code} -> {Status} | TransId={TransId}",
                payment.TransactionCode, payment.Status, callback.transId);
            ApplyFinalStatus(payment, callback.resultCode == 0);

            // resultCode = 0 là thành công (MoMo)
            payment.Status = callback.resultCode == 0
                ? PaymentStatus.Success
                : PaymentStatus.Failed;

            _logger.LogInformation("[MoMo Callback] Giao dịch {TransRef} -> {Status} | MoMo TransId={TransId}",
                payment.TransactionRef, payment.Status, callback.transId);

            return true;
        }
            if (!computedMac.Equals(callback.mac, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[ZaloPay Callback] MAC khong hop le");
                return false;
            }
        {
            if (callback.data == null || callback.mac == null) return false;
            try
            {
                data = JsonSerializer.Deserialize<ZaloPayCallbackData>(callback.data);
            }
            catch
            {
                _logger.LogError("[ZaloPay Callback] Khong parse duoc data JSON");
                return false;
            }
            try
            {
                data = JsonSerializer.Deserialize<ZaloPayCallbackData>(callback.data);
            // ZaloPay dùng app_trans_id dạng YYMMDD_OrderId
            // Ta tìm theo phần sau dấu "_"
            string orderIdPart = data.app_trans_id.Contains('_')
                ? data.app_trans_id.Split('_')[1]
                : data.app_trans_id;

            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.OrderCode != null &&
                                          p.OrderCode.Contains(orderIdPart));
            if (payment == null)
            {
                _logger.LogWarning("[ZaloPay Callback] Khong tim thay giao dich voi app_trans_id={TransId}", data.app_trans_id);
                return false;
            }

            if (payment.IsFinished)
            {
                _logger.LogInformation("[ZaloPay Callback] Giao dich {Code} da ket thuc ({Status}), bo qua callback lap.",
                    payment.TransactionCode, payment.Status);
                return true;
            }

            var payment = _payments.Values.FirstOrDefault(p =>
                p.GatewayOrderId != null && p.GatewayOrderId.Contains(orderIdPart, StringComparison.OrdinalIgnoreCase));

            if (payment == null)
            {
                _logger.LogWarning("[ZaloPay Callback] Không tìm thấy giao dịch với app_trans_id={TransId}", data.app_trans_id);
                return Task.FromResult(false);
            }

            await _context.SaveChangesAsync();
            payment.GatewayResponseMessage = callback.type == 1 ? "Thanh toán thành công" : "Thanh toán thất bại";
            _logger.LogInformation("[ZaloPay Callback] Giao dich {Code} -> {Status} | ZpTransId={ZpTransId}",
                payment.TransactionCode, payment.Status, data.zp_trans_id);
            payment.CallbackCount++;

            ApplyFinalStatus(payment, callback.type == 1);

            // type = 1 là thành công (ZaloPay)
            payment.Status = callback.type == 1
                ? PaymentStatus.Success
                : PaymentStatus.Failed;

            _logger.LogInformation("[ZaloPay Callback] Giao dịch {TransRef} -> {Status} | ZaloPay ZpTransId={ZpTransId}",
                payment.TransactionRef, payment.Status, data.zp_trans_id);

            return true;
        }

        // ============================================================
        // US-101: Người dùng hủy thanh toán
        // ============================================================
        public async Task<bool> CancelPaymentAsync(string transactionCode, string reason = "")
        {
            payment.Note = string.IsNullOrWhiteSpace(reason) ? "Đã huỷ giao dịch" : reason;
            _seatBooking.ReleaseHold(payment.HoldId, out _);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Payment] Da huy giao dich {Code} | Ly do: {Reason}", transactionCode, reason);
            return true;
        }

        // ============================================================
        // Ap dung trang thai ket thuc + chot/nha ghe
        // ============================================================
        private void ApplyFinalStatus(PaymentTransaction payment, bool isSuccess)
        {
            if (isSuccess)
            {
                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                .FirstOrDefaultAsync(p => p.TransactionCode == transactionCode);
                if (_seatBooking.ConfirmHold(payment.HoldId, out var message))
                {
                    _logger.LogInformation("[Payment] Da chot ghe cho giao dich {Code} | {Msg}", payment.TransactionCode, message);
                }
                else
                {
                    _logger.LogWarning("[Payment] Khong chot duoc ghe cho giao dich {Code}: {Msg}", payment.TransactionCode, message);
                }
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                _seatBooking.ReleaseHold(payment.HoldId, out _);
            }
                return false;

            if (payment.IsFinished)
                return false;

            payment.Status = PaymentStatus.Cancelled; // [US-97] CANCELLED
        private string BuildVNPayUrl(PaymentTransaction payment)
            payment.Note = string.IsNullOrWhiteSpace(reason) ? "Đã huỷ giao dịch" : reason;
            string orderInfo = $"Thanh toan ve xe SmartBus - {payment.TransactionCode}";
            string amount = ((long)(payment.Amount * 100)).ToString(); // VNPay yeu cau nhan 100
            return Task.FromResult(true);
        }

        // ============================================================
        // HELPER METHOD: Xử lý giải phóng ghế dựa trên trạng thái thanh toán
        // (Giải quyết US-97, US-100, US-101, US-103, US-107)
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
                { "vnp_TxnRef", payment.OrderCode! },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
            return $"{VNPayPaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";
                { "vnp_ReturnUrl", VNPayReturnUrl },
                { "vnp_IpAddr", payment.UserIpAddress ?? "127.0.0.1" },
        // ============================================================
        // US-65: Xây dựng URL thanh toán MoMo Sandbox
        // Theo chuẩn MoMo API v2 (HMAC-SHA256)
        // ============================================================
        private async Task<string> BuildMoMoUrlAsync(PaymentTransaction payment)
            string rawSignature = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={kv.Value}"));
            string secureHash = ComputeHmacSha512(rawSignature, VNPayHashSecret);

            string url = $"{VNPayPaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";
            return Task.FromResult(url);
            // Chuỗi raw để ký theo đúng thứ tự MoMo yêu cầu
            string rawSignature = $"accessKey={MoMoAccessKey}" +
                                  $"&amount={amount}" +
                                  $"&extraData=" +
                                  $"&ipnUrl={MoMoIpnUrl}" +
                                  $"&orderId={payment.OrderCode}" +
                                  $"&orderInfo={orderInfo}" +
                                  $"&partnerCode={MoMoPartnerCode}" +
                                  $"&redirectUrl={MoMoReturnUrl}" +
                                  $"&requestId={requestId}" +
                                  $"&requestType=payWithMethod";

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
                orderId = payment.OrderCode,
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

                _logger.LogWarning("[MoMo] Khong lay duoc payUrl. Response: {Response}", responseBody);
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(MoMoPaymentUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
            return $"https://test-payment.momo.vn/pay?orderId={payment.OrderCode}&amount={amount}";
                {
                    return payUrl.GetString() ?? string.Empty;
        // ============================================================
        // US-65: Xây dựng URL thanh toán ZaloPay Sandbox
        // Theo chuẩn ZaloPay API v2 (HMAC-SHA256)
        // ============================================================
        private async Task<string> BuildZaloPayUrlAsync(PaymentTransaction payment)
            {
                _logger.LogError(ex, "[MoMo] Loi khi goi API sandbox");
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
            string appTransId = $"{DateTime.UtcNow.AddHours(7):yyMMdd}_{payment.OrderCode}";
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
                description = $"SmartBus - Thanh toan cho don hang {payment.TransactionCode}",
                embed_data = embedData,
                item = item,
                callback_url = ZaloPayCallbackUrl,
                mac = mac
            };


                _logger.LogWarning("[ZaloPay] Khong lay duoc order_url. Response: {Response}", responseBody);
                using var client = new HttpClient();
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ZaloPayPaymentUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("order_url", out var orderUrl))
                {
        // ============================================================
        // Thông tin chuyển khoản ngân hàng (Bank Transfer)
        // ============================================================
        private static string BuildBankTransferInfo(PaymentTransaction payment)
            }
            return $"bank://transfer?account=9704229239874057&bank=NCB" +
                   $"&amount={(long)payment.Amount}" +
                   $"&note=SBGD{payment.OrderCode}" +
                   $"&name=SmartBus";

            return $"https://sb-openapi.zalopay.vn/pay?app_trans_id={appTransId}";
        // ============================================================
        // Tiện ích: Xác thực chữ ký VNPay (HMAC-SHA512)
        // ============================================================
        private static bool TryParseMethod(string raw, out PaymentMethod method)
        {
            method = PaymentMethod.VNPay;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string normalized = raw.Trim();
            if (normalized.Equals("Bank", StringComparison.OrdinalIgnoreCase))
                normalized = "BankTransfer";

            return Enum.TryParse(normalized, true, out method);
        }

        private static string ToStatusString(PaymentStatus status) => status.ToString().ToUpperInvariant();

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
            PaymentStatus.Unknown => "Không xác định, chờ đối soát",
            _ => "Không xác định"
        };
    }
}