const params = new URLSearchParams(window.location.search);
const seats = (params.get("seats") || "A03").split(",").filter(Boolean);
const total = Number(params.get("total")) || seats.length * 120000;

const money = value => new Intl.NumberFormat("vi-VN").format(value) + "đ";
document.getElementById("seatText").textContent = seats.join(", ");
document.getElementById("priceText").textContent = money(total);
document.getElementById("totalText").textContent = money(total);
document.getElementById("payAmount").textContent = money(total);

const notes = {
    VNPay: "Bạn sẽ được chuyển tới giao diện thanh toán của VNPay để thực hiện giao dịch.",
    MoMo: "Bạn sẽ được chuyển tới giao diện thanh toán của MoMo để thực hiện giao dịch.",
    ZaloPay: "Bạn sẽ được chuyển tới giao diện thanh toán của ZaloPay để thực hiện giao dịch.",
    Bank: "Bạn sẽ nhập thông tin thẻ hoặc chọn ngân hàng hỗ trợ để thực hiện giao dịch."
};

document.querySelectorAll(".method").forEach(method => {
    method.addEventListener("click", () => {
        document.querySelectorAll(".method").forEach(item => item.classList.remove("active"));
        method.classList.add("active");
        method.querySelector("input").checked = true;
        document.querySelector("#methodNote p").textContent = notes[method.querySelector("input").value];
    });
});

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

document.getElementById("payBtn").addEventListener("click", () => {
    const method = document.querySelector("input[name='paymentMethod']:checked").value;
    const query = new URLSearchParams({
        status: "success",
        seats: seats.join(","),
        total: String(total),
        method
    });
    window.location.href = `../ket-qua-thanh-toan/index.html?${query.toString()}`;
});
