const params = new URLSearchParams(window.location.search);
const selectedSeats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || 120000;

const ticket = {
    code: "SB-20260930-00126",
    route: "Tuyến 02: Hà Nội - Thái Nguyên",
    date: "30/09/2026",
    seats: selectedSeats,
    passenger: "Nguyễn Văn A",
    amount: total
};

const qrPayload = JSON.stringify({
    maVe: ticket.code,
    tuyen: ticket.route,
    ngayDi: ticket.date,
    ghe: ticket.seats,
    hanhKhach: ticket.passenger
});

function createQr() {
    const qr = document.getElementById("qrcode");

    if (typeof QRCode === "undefined") {
        qr.innerHTML = '<div class="qr-error">Không tải được thư viện QR.<br>Vui lòng kiểm tra kết nối mạng.</div>';
        return;
    }

    new QRCode(qr, {
        text: qrPayload,
        width: 186,
        height: 186,
        colorDark: "#172033",
        colorLight: "#ffffff",
        correctLevel: QRCode.CorrectLevel.M
    });
}

document.getElementById("printBtn").addEventListener("click", () => {
    window.print();
});

document.getElementById("copyBtn").addEventListener("click", async (event) => {
    try {
        await navigator.clipboard.writeText(ticket.code);
        event.currentTarget.textContent = "✓ Đã sao chép";
        setTimeout(() => event.currentTarget.textContent = "Sao chép mã vé", 1800);
    } catch {
        alert(`Mã vé: ${ticket.code}`);
    }
});

document.getElementById("cancelBtn").addEventListener("click", () => {
    const confirmed = confirm("Bạn muốn gửi yêu cầu hủy vé này?\n\nMã vé: " + ticket.code);
    if (confirmed) {
        alert("Đã ghi nhận yêu cầu hủy vé. Đây là giao diện demo, chưa kết nối API.");
    }
});

createQr();


// Đồng bộ dữ liệu demo từ màn chọn ghế.
document.getElementById("seatNumber").textContent = ticket.seats.join(", ");
document.querySelector(".ticket-payment > strong").textContent =
    new Intl.NumberFormat("vi-VN").format(ticket.amount) + "đ";
