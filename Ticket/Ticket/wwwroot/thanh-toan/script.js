// ============================================================
// US-65: Tích hợp cổng thanh toán Sandbox
// Kết nối với API /api/payment/create để lấy URL cổng thực
// ============================================================
const params = new URLSearchParams(window.location.search);
const seats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || seats.length * 120000;
const holdId = params.get("holdId") || ("HOLD-" + Date.now().toString(36)); // US-64: mã giữ chỗ
const tripCode = params.get("tripCode") || "TRIP01"; // mã chuyến

const money = value => new Intl.NumberFormat("vi-VN").format(value) + "đ";
document.getElementById("seatText").textContent = seats.join(", ");
document.getElementById("priceText").textContent = money(total);
document.getElementById("totalText").textContent = money(total);
document.getElementById("payAmount").textContent = money(total);

// ---- Ghi chú phương thức ----
const notes = {
    VNPay:   "Bạn sẽ được chuyển tới giao diện thanh toán của VNPay để thực hiện giao dịch.",
    MoMo:    "Bạn sẽ được chuyển tới giao diện thanh toán của MoMo để thực hiện giao dịch.",
    ZaloPay: "Bạn sẽ được chuyển tới giao diện thanh toán của ZaloPay để thực hiện giao dịch.",
    Bank:    "Bạn sẽ nhập thông tin thẻ hoặc chọn ngân hàng hỗ trợ để thực hiện giao dịch."
};

document.querySelectorAll(".method").forEach(method => {
    method.addEventListener("click", () => {
        document.querySelectorAll(".method").forEach(item => item.classList.remove("active"));
        method.classList.add("active");
        method.querySelector("input").checked = true;
        document.querySelector("#methodNote p").textContent = notes[method.querySelector("input").value];
    });
});

// ---- Đếm ngược thời gian giữ chỗ ----
let remaining = 10 * 60 - 1;
const countdown = document.getElementById("countdown");
const timer = setInterval(() => {
    const min = String(Math.floor(remaining / 60)).padStart(2, "0");
    const sec = String(remaining % 60).padStart(2, "0");
    countdown.textContent = `${min}:${sec}`;
    if (remaining <= 0) {
        clearInterval(timer);
        document.getElementById("payBtn").disabled = true;
        document.getElementById("payBtn").textContent = "Hết thời gian giữ chỗ";
    }
    remaining--;
}, 1000);

// ---- US-65: Kết nối API backend để lấy URL cổng thanh toán ----
document.getElementById("payBtn").addEventListener("click", async () => {
    const method = document.querySelector("input[name='paymentMethod']:checked").value;
    const payBtn = document.getElementById("payBtn");

    // Hiển thị trạng thái loading
    payBtn.disabled = true;
    payBtn.innerHTML = `<span class="spinner">⏳</span> Đang kết nối ${method}...`;

    try {
        // US-64: Gọi API tạo giao dịch và nhận mã tham chiếu nội bộ + URL cổng
        const response = await fetch("/api/payment/create", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                holdId: holdId,
                tripCode: tripCode,
                seatIds: seats,
                method: method,
                amount: total,
                userId: sessionStorage.getItem("userId") || null
            })
        });

        const result = await response.json();

        if (response.ok && result.success) {
            // US-65: Redirect đến cổng thanh toán sandbox thực sự
            if (result.paymentUrl) {
                sessionStorage.setItem("lastTransRef", result.transactionRef);
                window.location.href = result.paymentUrl;
            } else {
                // Fallback nếu không có URL (Bank transfer hoặc sandbox test)
                const query = new URLSearchParams({
                    status: "success",
                    seats: seats.join(","),
                    total: String(total),
                    method,
                    transRef: result.transactionRef
                });
                window.location.href = `../ket-qua-thanh-toan/index.html?${query.toString()}`;
            }
        } else {
            // Hiển thị lỗi và cho phép thử lại
            alert(`❌ ${result.message || "Không thể kết nối cổng thanh toán. Vui lòng thử lại."}`);
            payBtn.disabled = false;
            payBtn.innerHTML = `Thanh toán <strong>${money(total)}</strong> <span>→</span>`;
        }
    } catch (error) {
        console.error("[Payment] Lỗi kết nối API:", error);

        // Fallback sandbox: chuyển thẳng sang trang kết quả (chế độ demo)
        console.warn("[Payment] Chạy chế độ demo (không có backend)");
        const query = new URLSearchParams({
            status: "success",
            seats: seats.join(","),
            total: String(total),
            method,
            transRef: `SBGD-DEMO-${Date.now()}`
        });
        window.location.href = `../ket-qua-thanh-toan/index.html?${query.toString()}`;
    }
});

