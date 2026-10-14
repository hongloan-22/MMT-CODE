const params = new URLSearchParams(window.location.search);
const status = params.get("status") || "success";
const seats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || seats.length * 120000;
const method = params.get("method") || "VNPay";
const money = value => new Intl.NumberFormat("vi-VN").format(value) + "đ";

document.getElementById("seatNumber").textContent = seats.join(", ");
document.getElementById("paymentAmount").textContent = money(total);
document.getElementById("paymentMethod").textContent = method === "Bank" ? "Thẻ ngân hàng" : method;
document.getElementById("ticketBtn").href = `../ve/index.html?${new URLSearchParams({seats:seats.join(","),total:String(total)}).toString()}`;

// US-64: Hiển thị mã tham chiếu nội bộ thực (từ backend) hoặc mã fake nếu không có
const transRef = params.get("transRef") || sessionStorage.getItem("lastTransRef") || null;
document.getElementById("transactionCode").textContent = transRef
    ? transRef
    : `SBGD-${new Date().toISOString().slice(0,10).replaceAll("-","")}-${String(Math.floor(Math.random()*99999)+1).padStart(5,"0")}`;

if (status === "failed") {
    const card = document.getElementById("resultCard");
    card.classList.add("failed");
    document.getElementById("resultIcon").textContent = "!";
    document.getElementById("resultEyebrow").textContent = "THANH TOÁN THẤT BẠI";
    document.getElementById("resultTitle").textContent = "Không thể hoàn tất thanh toán";
    document.getElementById("resultMessage").textContent = "Giao dịch chưa thành công. Bạn có thể quay lại bước thanh toán để thử lại.";
    document.getElementById("next-step");
    document.getElementById("ticketBtn").textContent = "Thanh toán lại";
    document.getElementById("ticketBtn").href = `../thanh-toan/index.html?${new URLSearchParams({seats:seats.join(","),total:String(total)}).toString()}`;
    document.querySelector(".next-step strong").textContent = "Ghế vẫn đang được giữ tạm thời";
    document.querySelector(".next-step p").textContent = "Vui lòng quay lại bước thanh toán và chọn phương thức khác nếu cần.";
}
