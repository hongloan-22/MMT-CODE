// ==========================================
// 0. USER & TICKET CONTEXT
// ==========================================
const params = new URLSearchParams(window.location.search);
const tripId = params.get("tripId") || "TRIP02";
const holdId = params.get("holdId") || "";

// ✅ Đọc userId từ userSession
let userId = null;
try {
    const s = JSON.parse(localStorage.getItem("userSession") || "{}");
    userId = s.userId || null;
} catch (e) { console.warn("Parse userSession lỗi:", e); }

// ✅ Đọc ticketId: URL → sessionStorage
let ticketId = parseInt(params.get("ticketId")) || parseInt(params.get("id")) || null;
if (!ticketId) {
    const stored = sessionStorage.getItem("lastTicketId");
    if (stored) ticketId = parseInt(stored) || null;
}

// ==========================================
// 1. ĐỌC DỮ LIỆU ĐỘNG TỪ URL
// ==========================================
const selectedSeats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || 120000;
const paymentMethod = params.get("paymentMethod") || "VNPAY";

const routeDataMap = {
    "TRIP01": {
        route: "Tuyến 01: Bến xe Gia Lâm - Bến xe Yên Nghĩa",
        routeName: "Tuyến 01",
        fromCity: "Hà Nội", fromStation: "Bến xe Gia Lâm",
        toCity: "Hà Nội", toStation: "Bến xe Yên Nghĩa",
        depTime: "08:00", arrTime: "09:30", duration: "~ 1 giờ 30 phút"
    },
    "TRIP02": {
        route: "Tuyến 02: Bến xe Mỹ Đình - Thái Nguyên",
        routeName: "Tuyến 02",
        fromCity: "Hà Nội", fromStation: "Bến xe Mỹ Đình",
        toCity: "Thái Nguyên", toStation: "Cổng Trường ĐH ICTU",
        depTime: "06:30", arrTime: "08:00", duration: "~ 1 giờ 30 phút"
    }
};

const currentTrip = routeDataMap[tripId] || routeDataMap["TRIP02"];
const ticketCode = holdId ? `SB-${holdId.substring(0, 10).toUpperCase()}` : `SB-${Date.now().toString().slice(-8)}`;

const ticket = {
    code: ticketCode,
    route: currentTrip.route,
    date: new Date().toLocaleDateString("vi-VN"),
    seats: selectedSeats,
    passenger: "Nguyễn Văn A",
    amount: total,
    method: paymentMethod
};

// ==========================================
// 2. CẬP NHẬT GIAO DIỆN
// ==========================================
document.addEventListener("DOMContentLoaded", () => {
    const codeEl = document.getElementById("ticketCode");
    const qrTextEl = document.getElementById("qrText");
    if (codeEl) codeEl.textContent = ticket.code;
    if (qrTextEl) qrTextEl.textContent = ticket.code;

    const routeStations = document.querySelectorAll(".route-station");
    if (routeStations.length >= 2) {
        routeStations[0].querySelector(".time").textContent = currentTrip.depTime;
        routeStations[0].querySelector("strong").textContent = currentTrip.fromCity;
        routeStations[0].querySelector("small").textContent = currentTrip.fromStation;
        routeStations[1].querySelector(".time").textContent = currentTrip.arrTime;
        routeStations[1].querySelector("strong").textContent = currentTrip.toCity;
        routeStations[1].querySelector("small").textContent = currentTrip.toStation;
    }

    const durationEl = document.querySelector(".duration");
    if (durationEl) durationEl.textContent = currentTrip.duration;

    const infoItems = document.querySelectorAll(".ticket-info-grid .info-item strong");
    if (infoItems.length >= 6) {
        infoItems[0].textContent = ticket.date;
        infoItems[1].textContent = currentTrip.routeName;
        infoItems[2].textContent = ticket.seats.join(", ");
    }

    const paymentDesc = document.querySelector(".ticket-payment span");
    const paymentAmount = document.querySelector(".ticket-payment strong:last-child");
    if (paymentDesc) paymentDesc.innerHTML = `Thanh toán qua <strong>${ticket.method}</strong>`;
    if (paymentAmount) paymentAmount.textContent = new Intl.NumberFormat("vi-VN").format(ticket.amount) + "đ";

    renderTicketQr();
});

// ==========================================
// 3. VẼ QR
// ==========================================
function renderTicketQr() {
    const qrImg = document.getElementById("qrDirectImg");
    if (!qrImg) return;
    const qrContent = encodeURIComponent(`SMARTBUS|${ticket.code}|${ticket.seats.join(",")}|${ticket.amount}`);
    qrImg.src = `https://quickchart.io/qr?text=${qrContent}&size=180`;
}

// ==========================================
// 4. NÚT BẤM
// ==========================================
document.getElementById("printBtn")?.addEventListener("click", () => window.print());

document.getElementById("copyBtn")?.addEventListener("click", async (event) => {
    try {
        await navigator.clipboard.writeText(ticket.code);
        event.currentTarget.textContent = "✓ Đã sao chép";
        setTimeout(() => event.currentTarget.textContent = "Sao chép mã vé", 1800);
    } catch { alert(`Mã vé: ${ticket.code}`); }
});

document.getElementById("cancelBtn")?.addEventListener("click", async () => {
    // Check login
    if (!userId) {
        alert("Vui lòng đăng nhập lại!");
        window.location.href = "/auth/dangnhap.html";
        return;
    }

    // ✅ Fallback: nếu chưa có ticketId → gọi BE lấy vé Booked mới nhất
    if (!ticketId) {
        try {
            const r = await fetch(`/api/tickets/latest-booked?userId=${encodeURIComponent(userId)}`);
            if (r.ok) {
                const d = await r.json();
                ticketId = d.ticketId;
                console.log("✅ [ve] Fallback lấy ticketId:", ticketId);
            } else if (r.status === 404) {
                alert("❌ Bạn không có vé nào đang hiệu lực để hủy.");
                return;
            }
        } catch (e) { console.warn("Fallback fetch lỗi:", e); }
    }

    // Fallback cuối — prompt
    if (!ticketId) {
        const input = prompt("Nhập ID vé cần hủy (VD: 16):");
        if (!input) return;
        ticketId = parseInt(input);
        if (isNaN(ticketId)) { alert("❌ ID vé không hợp lệ"); return; }
    }

    if (!confirm(`Bạn có chắc chắn muốn hủy vé: ${ticket.code}?`)) return;

    const reason = prompt("Nhập lý do hủy vé:", "Không đi được");
    if (reason === null) return;

    try {
        const res = await fetch('/api/refunds/request', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                ticketId: ticketId,
                userId: userId,
                reason: reason || "Không có lý do"
            })
        });

        const data = await res.json();

        if (res.ok && data.success) {
            alert(
                `✅ Yêu cầu hoàn tiền đã gửi!\n\n` +
                `Mã vé: ${ticket.code}\n` +
                `Số tiền hoàn: ${Number(data.data.amount).toLocaleString('vi-VN')}đ\n` +
                `Phí hủy: ${Number(data.data.fee).toLocaleString('vi-VN')}đ\n` +
                `Trạng thái: ${data.data.status}\n\n` +
                `Vui lòng chờ admin duyệt.`
            );
            sessionStorage.removeItem("lastTicketId");
        } else {
            alert(`❌ ${data.message || "Không thể hủy vé"}`);
        }
    } catch (err) {
        console.error(err);
        alert("❌ Không kết nối được server. Vui lòng thử lại.");
    }
});