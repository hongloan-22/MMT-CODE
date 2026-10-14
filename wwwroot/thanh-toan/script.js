// ============================================================
// US-65 & US-64: Tích hợp cổng thanh toán Sandbox & Giữ chỗ
// ============================================================

// 1. Đọc tham số truyền từ trang chọn ghế sang
const params = new URLSearchParams(window.location.search);
const tripId = params.get("tripId") || params.get("tripCode") || "TRIP01";
const holdId = params.get("holdId") || "";
const seatListStr = params.get("seats") || "A01";
const seats = seatListStr.split(",").map(s => s.trim()).filter(Boolean);
const totalStr = params.get("total") || "";
const total = Number(totalStr) || (seats.length * 120000);
const expiresAtStr = params.get("expiresAt") || "";

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN").format(Number(value) || 0) + "đ";
}

// ---- Ghi chú phương thức ----
const notes = {
    VNPay: "Bạn sẽ được chuyển tới giao diện thanh toán của VNPay để thực hiện giao dịch.",
    MoMo: "Bạn sẽ được chuyển tới giao diện thanh toán của MoMo để thực hiện giao dịch.",
    ZaloPay: "Bạn sẽ được chuyển tới giao diện thanh toán của ZaloPay để thực hiện giao dịch.",
    Bank: "Bạn sẽ nhập thông tin thẻ hoặc chọn ngân hàng hỗ trợ để thực hiện giao dịch."
};

document.addEventListener("DOMContentLoaded", () => {
    // 2. Điền thông tin ghế và tiền vào đúng ID của HTML
    const seatText = document.getElementById("seatText");
    const priceText = document.getElementById("priceText");
    const totalText = document.getElementById("totalText");
    const payAmount = document.getElementById("payAmount");

    if (seatText && seats.length > 0) {
        seatText.textContent = seats.join(", ");
    }

    const formattedMoney = formatMoney(total);
    if (priceText) priceText.textContent = formattedMoney;
    if (totalText) totalText.textContent = formattedMoney;
    if (payAmount) payAmount.textContent = formattedMoney;

    // 3. Xử lý chuyển đổi qua lại giữa các phương thức thanh toán
    const methodLabels = document.querySelectorAll(".method-list .method, .method");
    const methodNote = document.getElementById("methodNote");

    methodLabels.forEach(label => {
        label.addEventListener("click", () => {
            methodLabels.forEach(l => l.classList.remove("active"));
            label.classList.add("active");

            const radio = label.querySelector('input[type="radio"], input');
            if (radio) {
                radio.checked = true;
                const methodVal = radio.value;
                if (methodNote) {
                    const p = methodNote.querySelector("p");
                    if (p) {
                        p.textContent = notes[methodVal] || `Bạn sẽ được chuyển tới giao diện thanh toán của ${methodVal} để thực hiện giao dịch.`;
                    }
                }
            }
        });
    });

    // 4. Xử lý nút THANH TOÁN (payBtn) - Gọi API Backend Cổng Thanh Toán
    const payBtn = document.getElementById("payBtn");
    if (payBtn) {
        payBtn.addEventListener("click", async (e) => {
            e.preventDefault();

            const selectedRadio = document.querySelector('input[name="paymentMethod"]:checked');
            const selectedMethod = selectedRadio ? selectedRadio.value : "MoMo";

            payBtn.disabled = true;
            payBtn.innerHTML = `<span class="spinner">⏳</span> Đang kết nối ${selectedMethod}...`;

            try {
                // Gọi API tạo giao dịch thanh toán
                const response = await fetch("/api/payment/create", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        holdId: holdId,
                        tripCode: tripId,
                        seatIds: seats,
                        method: selectedMethod,
                        amount: total,
                        userId: sessionStorage.getItem("userId") || null
                    })
                });

                const result = await response.json().catch(() => ({}));

                if (response.ok && (result.success || result.paymentUrl)) {
                    // Nếu Backend trả về URL thanh toán Sandbox (MoMo/VNPay)
                    if (result.paymentUrl) {
                        sessionStorage.setItem("lastTransRef", result.transactionRef || "");
                        window.location.href = result.paymentUrl;
                    } else {
                        // Redirect thẳng trang kết quả nếu là Sandbox mock
                        const query = new URLSearchParams({
                            status: "SUCCESS",
                            seats: seats.join(","),
                            total: String(total),
                            paymentMethod: selectedMethod,
                            holdId: holdId,
                            tripId: tripId,
                            transRef: result.transactionRef || `SBGD-${Date.now()}`
                        });
                        window.location.href = `../ket-qua-thanh-toan/index.html?${query.toString()}`;
                    }
                } else {
                    alert(`❌ ${result.message || "Không thể kết nối cổng thanh toán. Vui lòng thử lại."}`);
                    payBtn.disabled = false;
                    payBtn.innerHTML = `Thanh toán <strong>${formatMoney(total)}</strong> <span>→</span>`;
                }
            } catch (error) {
                console.error("[Payment] Lỗi kết nối API:", error);
                alert("Có lỗi xảy ra khi kết nối máy chủ thanh toán! Vui lòng thử lại.");
                payBtn.disabled = false;
                payBtn.innerHTML = `Thanh toán <strong>${formatMoney(total)}</strong> <span>→</span>`;
            }
        });
    }

    // 5. Đếm ngược 10 phút giữ chỗ
    initCountdownTimer();
});

function initCountdownTimer() {
    let expireTime = 0;
    if (expiresAtStr) {
        expireTime = new Date(expiresAtStr).getTime();
    } else {
        expireTime = new Date().getTime() + 10 * 60 * 1000;
    }

    const countdownEl = document.getElementById("countdown");
    if (!countdownEl) return;

    const timerInterval = setInterval(() => {
        const now = new Date().getTime();
        const distance = expireTime - now;

        if (distance <= 0) {
            clearInterval(timerInterval);
            countdownEl.textContent = "00:00";
            alert("Đã hết thời gian giữ chỗ! Vui lòng chọn lại ghế.");
            window.location.href = `../chon-ghe/index.html?tripId=${tripId}`;
            return;
        }

        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);
        countdownEl.textContent = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }, 1000);
}