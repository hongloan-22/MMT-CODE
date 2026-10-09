// ==========================================
// 1. ĐỌC DỮ LIỆU ĐỘNG TỪ URL
// ==========================================
const params = new URLSearchParams(window.location.search);
const tripId = params.get("tripId") || "TRIP02";
const holdId = params.get("holdId") || "";
const selectedSeats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || 120000;
const paymentMethod = params.get("paymentMethod") || "VNPAY";

const routeDataMap = {
    "TRIP01": {
        route: "Tuyến 01: Bến xe Gia Lâm - Bến xe Yên Nghĩa",
        routeName: "Tuyến 01",
        fromCity: "Hà Nội",
        fromStation: "Bến xe Gia Lâm",
        toCity: "Hà Nội",
        toStation: "Bến xe Yên Nghĩa",
        depTime: "08:00",
        arrTime: "09:30",
        duration: "~ 1 giờ 30 phút"
    },
    "TRIP02": {
        route: "Tuyến 02: Bến xe Mỹ Đình - Thái Nguyên",
        routeName: "Tuyến 02",
        fromCity: "Hà Nội",
        fromStation: "Bến xe Mỹ Đình",
        toCity: "Thái Nguyên",
        toStation: "Cổng Trường ĐH ICTU",
        depTime: "06:30",
        arrTime: "08:00",
        duration: "~ 1 giờ 30 phút"
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
// 2. CẬP NHẬT GIAO DIỆN HTML ĐỘNG
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
// 3. XỬ LÝ VẼ MÃ QR (CĂN GIỮA CHUẨN XÁC)
// ==========================================
function renderTicketQr() {
    const qrImg = document.getElementById("qrDirectImg");
    if (!qrImg) return;

    const qrContent = encodeURIComponent(`SMARTBUS|${ticket.code}|${ticket.seats.join(",")}|${ticket.amount}`);
    qrImg.src = `https://quickchart.io/qr?text=${qrContent}&size=180`;
}

// ==========================================
// 4. SỰ KIỆN NÚT BẤM (IN, SAO CHÉP, HỦY)
// ==========================================
document.getElementById("printBtn")?.addEventListener("click", () => {
    window.print();
});

document.getElementById("copyBtn")?.addEventListener("click", async (event) => {
    try {
        await navigator.clipboard.writeText(ticket.code);
        event.currentTarget.textContent = "✓ Đã sao chép";
        setTimeout(() => event.currentTarget.textContent = "Sao chép mã vé", 1800);
    } catch {
        alert(`Mã vé: ${ticket.code}`);
    }
});

document.getElementById("cancelBtn")?.addEventListener("click", () => {
    if (confirm(`Bạn có chắc chắn muốn yêu cầu hủy vé: ${ticket.code}?`)) {
        alert("Đã gửi yêu cầu hủy vé thành công đến ban quản trị!");
    }
});