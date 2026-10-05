// 1. Đọc tham số truyền từ trang chọn ghế sang
const params = new URLSearchParams(window.location.search);
const tripId = params.get("tripId") || "TRIP01";
const holdId = params.get("holdId") || "";
const seatListStr = params.get("seats") || "A01";
const totalStr = params.get("total") || "120000";
const expiresAtStr = params.get("expiresAt") || "";

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN").format(Number(value) || 0) + "đ";
}

document.addEventListener("DOMContentLoaded", () => {
    // 2. Điền thông tin ghế và tiền vào đúng ID của HTML
    const seatText = document.getElementById("seatText");
    const priceText = document.getElementById("priceText");
    const totalText = document.getElementById("totalText");
    const payAmount = document.getElementById("payAmount");

    if (seatText && seatListStr) {
        seatText.textContent = seatListStr.split(",").join(", ");
    }

    const formattedMoney = formatMoney(totalStr);
    if (priceText) priceText.textContent = formattedMoney;
    if (totalText) totalText.textContent = formattedMoney;
    if (payAmount) payAmount.textContent = formattedMoney;

    // 3. Xử lý chuyển đổi qua lại giữa các phương thức thanh toán
    const methodLabels = document.querySelectorAll(".method-list .method");
    const methodNote = document.getElementById("methodNote");

    methodLabels.forEach(label => {
        label.addEventListener("click", () => {
            methodLabels.forEach(l => l.classList.remove("active"));
            label.classList.add("active");

            const radio = label.querySelector('input[type="radio"]');
            if (radio) {
                radio.checked = true;
                if (methodNote) {
                    const p = methodNote.querySelector("p");
                    if (p) p.textContent = `Bạn sẽ được chuyển tới giao diện thanh toán của ${radio.value} để thực hiện giao dịch.`;
                }
            }
        });
    });

    // 4. Xử lý nút THANH TOÁN (payBtn)
    const payBtn = document.getElementById("payBtn");
    if (payBtn) {
        payBtn.addEventListener("click", (e) => {
            e.preventDefault();

            const selectedMethod = document.querySelector('input[name="paymentMethod"]:checked')?.value || "VNPay";

            payBtn.disabled = true;
            payBtn.innerHTML = `Đang kết nối cổng ${selectedMethod}...`;

            // Demo Sprint 2: Xác nhận thành công và điều hướng sang trang vé hoàn tất (Bước 3)
           

                const forwardParams = new URLSearchParams({
                    tripId: tripId,
                    holdId: holdId,
                    seats: seatListStr,
                    total: totalStr,
                    paymentMethod: selectedMethod,
                    status: "SUCCESS"
                });

                // Chuyển sang trang hoàn tất đặt vé / vé QR (bước 3 trên thanh progress)
                window.location.href = `../ket-qua-thanh-toan/index.html?${forwardParams.toString()}`;
            }, 800);
        }
    

    // 5. Đếm ngược 10 phút giữ chỗ chuẩn
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