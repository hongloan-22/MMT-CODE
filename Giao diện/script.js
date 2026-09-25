const fromInput = document.getElementById("from");
const toInput = document.getElementById("to");
const departureInput = document.getElementById("departure");
const returnInput = document.getElementById("returnDate");
const clearReturn = document.getElementById("clearReturn");
const swapBtn = document.getElementById("swapBtn");
const searchBtn = document.getElementById("searchBtn");
const formMessage = document.getElementById("formMessage");
const loginBtn = document.getElementById("loginBtn");

// Không cho chọn ngày trong quá khứ
const today = new Date();
const todayString =
    today.getFullYear() + "-" +
    String(today.getMonth() + 1).padStart(2, "0") + "-" +
    String(today.getDate()).padStart(2, "0");

departureInput.min = todayString;
returnInput.min = todayString;

// Mặc định ngày đi là ngày tiếp theo nếu người dùng chưa chọn
const tomorrow = new Date();
tomorrow.setDate(today.getDate() + 1);
const tomorrowString =
    tomorrow.getFullYear() + "-" +
    String(tomorrow.getMonth() + 1).padStart(2, "0") + "-" +
    String(tomorrow.getDate()).padStart(2, "0");

departureInput.value = tomorrowString;

// Khi đổi ngày đi, ngày về không được nhỏ hơn ngày đi
departureInput.addEventListener("change", () => {
    returnInput.min = departureInput.value;

    if (returnInput.value && returnInput.value < departureInput.value) {
        returnInput.value = "";
        clearReturn.style.display = "none";
    }
});

// Hiện nút xóa khi có ngày về
returnInput.addEventListener("change", () => {
    clearReturn.style.display = returnInput.value ? "block" : "none";
});

// Xóa ngày về
clearReturn.addEventListener("click", () => {
    returnInput.value = "";
    clearReturn.style.display = "none";
});

// Đổi điểm đi / điểm đến
swapBtn.addEventListener("click", () => {
    const temp = fromInput.value;
    fromInput.value = toInput.value;
    toInput.value = temp;
});

// Tìm chuyến
searchBtn.addEventListener("click", () => {
    formMessage.textContent = "";

    const from = fromInput.value.trim();
    const to = toInput.value.trim();
    const departure = departureInput.value;
    const returnDate = returnInput.value;

    if (!from || !to) {
        formMessage.textContent = "Vui lòng nhập nơi xuất phát và nơi đến.";
        return;
    }

    if (!departure) {
        formMessage.textContent = "Vui lòng chọn ngày đi.";
        return;
    }

    if (from.toLowerCase() === to.toLowerCase()) {
        formMessage.textContent = "Nơi xuất phát và nơi đến không được giống nhau.";
        return;
    }

    if (returnDate && returnDate < departure) {
        formMessage.textContent = "Ngày về phải bằng hoặc sau ngày đi.";
        return;
    }

    const formatDate = (dateString) => {
        const [year, month, day] = dateString.split("-");
        return `${day}/${month}/${year}`;
    };

    const message = returnDate
        ? `Đang tìm chuyến ${from} → ${to} từ ${formatDate(departure)} đến ${formatDate(returnDate)}...`
        : `Đang tìm chuyến ${from} → ${to} ngày ${formatDate(departure)}...`;

    formMessage.style.color = "#1685e8";
    formMessage.textContent = message;

    // Demo: sau 800ms thông báo kết quả
    setTimeout(() => {
        alert(
            "SMARTBUS\n\n" +
            `Tuyến: ${from} → ${to}\n` +
            `Ngày đi: ${formatDate(departure)}\n` +
            (returnDate ? `Ngày về: ${formatDate(returnDate)}\n` : "") +
            "\nĐây là giao diện demo. Phần danh sách chuyến xe có thể kết nối với Backend/API sau."
        );
    }, 800);
});

// Nút đăng nhập demo
loginBtn.addEventListener("click", () => {
    alert("Trang Đăng nhập Smartbus sẽ được mở tại đây.");
});

// Xóa màu thông báo khi người dùng nhập lại
[fromInput, toInput, departureInput, returnInput].forEach(input => {
    input.addEventListener("input", () => {
        formMessage.textContent = "";
        formMessage.style.color = "#e25858";
    });
});
