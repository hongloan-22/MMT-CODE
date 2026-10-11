const params = new URLSearchParams(window.location.search);
const status = params.get("status") || "success";
const seats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || seats.length * 120000;
const method = params.get("paymentMethod") || params.get("method") || "VNPay";
const money = value => new Intl.NumberFormat("vi-VN").format(value) + "đ";

document.getElementById("seatNumber").textContent = seats.join(", ");
document.getElementById("paymentAmount").textContent = money(total);
document.getElementById("paymentMethod").textContent = method === "Bank" ? "Thẻ ngân hàng" : method;

// Mã tham chiếu
const transRef = params.get("transRef") || sessionStorage.getItem("lastTransactionCode") || null;
document.getElementById("transactionCode").textContent = transRef
    ? transRef
    : `SBGD-${new Date().toISOString().slice(0, 10).replaceAll("-", "")}-${String(Math.floor(Math.random() * 99999) + 1).padStart(5, "0")}`;

// ============================================================
// ✅ Tự động fetch ticketId mới nhất của user → gắn vào link "Xem vé"
// ============================================================
async function buildTicketLink() {
    const ticketBtn = document.getElementById("ticketBtn");

    // Đọc userId từ sessionStorage hoặc userSession
    let userId = sessionStorage.getItem("lastUserId") || "";
    if (!userId) {
        try {
            const s = JSON.parse(localStorage.getItem("userSession") || "{}");
            userId = s.userId || "";
        } catch { }
    }

    let ticketId = "";

    if (userId && status !== "failed") {
        try {
            const r = await fetch(`/api/tickets/latest-booked?userId=${encodeURIComponent(userId)}`);
            if (r.ok) {
                const d = await r.json();
                ticketId = d.ticketId || "";
                console.log("✅ [ket-qua] Lấy được ticketId:", ticketId);
            } else {
                console.warn("⚠️ [ket-qua] Không lấy được ticketId, status:", r.status);
            }
        } catch (e) {
            console.warn("⚠️ [ket-qua] Fetch ticketId lỗi:", e);
        }
    }

    // Build URL
    const veParams = new URLSearchParams({
        seats: seats.join(","),
        total: String(total),
        holdId: params.get("holdId") || sessionStorage.getItem("lastHoldId") || "",
        tripId: params.get("tripId") || sessionStorage.getItem("lastTripId") || ""
    });
    if (ticketId) {
        veParams.set("ticketId", ticketId);
        sessionStorage.setItem("lastTicketId", ticketId);
    }

    ticketBtn.href = `../ve/index.html?${veParams.toString()}`;
    console.log("✅ [ket-qua] Link xem vé:", ticketBtn.href);
}

buildTicketLink();

// ============================================================
// Trạng thái thất bại
// ============================================================
if (status === "failed") {
    const card = document.getElementById("resultCard");
    card.classList.add("failed");
    document.getElementById("resultIcon").textContent = "!";
    document.getElementById("resultEyebrow").textContent = "THANH TOÁN THẤT BẠI";
    document.getElementById("resultTitle").textContent = "Không thể hoàn tất thanh toán";
    document.getElementById("resultMessage").textContent = "Giao dịch chưa thành công. Bạn có thể quay lại bước thanh toán để thử lại.";

    document.getElementById("ticketBtn").textContent = "Thanh toán lại";
    document.getElementById("ticketBtn").href = `../thanh-toan/index.html?${new URLSearchParams({
        seats: seats.join(","),
        total: String(total),
        holdId: params.get("holdId") || sessionStorage.getItem("lastHoldId") || "",
        tripId: params.get("tripId") || sessionStorage.getItem("lastTripId") || ""
    }).toString()}`;

    document.querySelector(".next-step strong").textContent = "Ghế vẫn đang được giữ tạm thời";
    document.querySelector(".next-step p").textContent = "Vui lòng quay lại bước thanh toán và chọn phương thức khác nếu cần.";
}