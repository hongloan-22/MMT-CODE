const fromInput = document.getElementById("from");
const toInput = document.getElementById("to");
const departureInput = document.getElementById("departure");
const returnInput = document.getElementById("returnDate");
const clearReturn = document.getElementById("clearReturn");
const swapBtn = document.getElementById("swapBtn");
const searchBtn = document.getElementById("searchBtn");
const formMessage = document.getElementById("formMessage");
const tripList = document.getElementById("tripList");
const resultSummary = document.getElementById("resultSummary");
const resultCount = document.getElementById("resultCount");
const loginBtn = document.getElementById("loginBtn");

const locations = [
    "Bến xe Mỹ Đình",
    "Bến xe Giáp Bát",
    "Bến xe Nước Ngầm",
    "Bến xe Gia Lâm",
    "Thái Nguyên",
    "Hà Nội",
    "Bắc Ninh",
    "Bắc Giang",
    "Hải Phòng",
    "Quảng Ninh"
];

const trips = [
    { operator: "Smartbus Express", code: "SB01", from: "Hà Nội", to: "Thái Nguyên", depart: "06:30", arrive: "08:30", duration: "2 giờ", price: 90000, time: "morning", seats: 12 },
    { operator: "Smartbus Express", code: "SB03", from: "Hà Nội", to: "Thái Nguyên", depart: "09:00", arrive: "11:00", duration: "2 giờ", price: 95000, time: "morning", seats: 8 },
    { operator: "Smartbus", code: "SB08", from: "Hà Nội", to: "Thái Nguyên", depart: "13:30", arrive: "15:30", duration: "2 giờ", price: 110000, time: "afternoon", seats: 18 },
    { operator: "Smartbus", code: "SB11", from: "Hà Nội", to: "Thái Nguyên", depart: "18:00", arrive: "20:00", duration: "2 giờ", price: 120000, time: "evening", seats: 5 }
];

const today = new Date();
const todayString = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;

departureInput.min = todayString;
returnInput.min = todayString;

const tomorrow = new Date(today);
tomorrow.setDate(tomorrow.getDate() + 1);
departureInput.value = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, "0")}-${String(tomorrow.getDate()).padStart(2, "0")}`;

function formatDate(value) {
    const [year, month, day] = value.split("-");
    return `${day}/${month}/${year}`;
}

function setupSuggestions(input, box) {
    input.addEventListener("input", () => {
        const keyword = input.value.trim().toLowerCase();
        box.innerHTML = "";

        if (!keyword) {
            box.style.display = "none";
            return;
        }

        const matches = locations.filter(location => location.toLowerCase().includes(keyword)).slice(0, 5);

        matches.forEach(location => {
            const item = document.createElement("div");
            item.className = "suggestion-item";
            item.textContent = location;
            item.addEventListener("click", () => {
                input.value = location;
                box.style.display = "none";
            });
            box.appendChild(item);
        });

        box.style.display = matches.length ? "block" : "none";
    });
}

setupSuggestions(fromInput, document.getElementById("fromSuggestions"));
setupSuggestions(toInput, document.getElementById("toSuggestions"));

document.addEventListener("click", event => {
    if (!event.target.closest(".suggestion-field")) {
        document.querySelectorAll(".suggestions").forEach(box => box.style.display = "none");
    }
});

departureInput.addEventListener("change", () => {
    returnInput.min = departureInput.value;
    if (returnInput.value && returnInput.value < departureInput.value) {
        returnInput.value = "";
        clearReturn.style.display = "none";
    }
});

returnInput.addEventListener("change", () => {
    clearReturn.style.display = returnInput.value ? "block" : "none";
});

clearReturn.addEventListener("click", () => {
    returnInput.value = "";
    clearReturn.style.display = "none";
});

swapBtn.addEventListener("click", () => {
    const temp = fromInput.value;
    fromInput.value = toInput.value;
    toInput.value = temp;
});

function renderTrips(list) {
    resultCount.textContent = `${list.length} chuyến`;

    if (!list.length) {
        tripList.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">🚌</div>
                <h3>Không tìm thấy chuyến phù hợp</h3>
                <p>Hãy thử thay đổi thời gian hoặc bộ lọc để tìm chuyến khác.</p>
            </div>`;
        return;
    }

    tripList.innerHTML = list.map(trip => `
        <article class="trip-card">
            <div class="trip-head">
                <div class="operator">
                    <div class="operator-icon">🚌</div>
                    <div>
                        <strong>${trip.operator}</strong>
                        <small>Mã chuyến: ${trip.code}</small>
                    </div>
                </div>
                <span class="trip-status">Còn ${trip.seats} chỗ</span>
            </div>

            <div class="trip-route">
                <div class="time">
                    <strong>${trip.depart}</strong>
                    <small>${trip.from}</small>
                </div>
                <div class="route-line">
                    <span class="route-arrow">→</span>
                    <small>${trip.duration}</small>
                    <span class="route-arrow">→</span>
                </div>
                <div class="time">
                    <strong>${trip.arrive}</strong>
                    <small>${trip.to}</small>
                </div>
            </div>

            <div class="trip-bottom">
                <div class="trip-meta">
                    <span>✓ Chỗ ngồi có sẵn</span>
                    <span>▣ ${formatDate(departureInput.value)}</span>
                </div>
                <div>
                    <span class="trip-price">${trip.price.toLocaleString("vi-VN")}đ</span>
                    <button class="select-trip-btn" data-code="${trip.code}">Chọn chuyến</button>
                </div>
            </div>
        </article>
    `).join("");

    document.querySelectorAll(".select-trip-btn").forEach(button => {
        button.addEventListener("click", () => {
            formMessage.style.color = "#1685e8";
            formMessage.textContent = `Bạn đã chọn chuyến ${button.dataset.code}. Có thể chuyển sang bước đặt vé.`;
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
    });
}

function getFilteredTrips() {
    const time = document.querySelector('input[name="timeFilter"]:checked').value;
    const price = document.querySelector('input[name="priceFilter"]:checked').value;

    return trips.filter(trip => {
        const timeOk = time === "all" || trip.time === time;
        const priceOk =
            price === "all" ||
            (price === "low" && trip.price < 100000) ||
            (price === "mid" && trip.price >= 100000 && trip.price <= 150000) ||
            (price === "high" && trip.price > 150000);
        return timeOk && priceOk;
    });
}

searchBtn.addEventListener("click", () => {
    formMessage.style.color = "#e25858";

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

    formMessage.style.color = "#1685e8";
    formMessage.textContent = "Đang tìm chuyến phù hợp...";

    setTimeout(() => {
        const filtered = getFilteredTrips();
        resultSummary.textContent = returnDate
            ? `${from} → ${to} | ${formatDate(departure)} - ${formatDate(returnDate)}`
            : `${from} → ${to} | Ngày ${formatDate(departure)}`;
        renderTrips(filtered);
        document.getElementById("resultsSection").scrollIntoView({ behavior: "smooth" });
    }, 350);
});

document.querySelectorAll('input[name="timeFilter"], input[name="priceFilter"]').forEach(input => {
    input.addEventListener("change", () => {
        const current = getFilteredTrips();
        renderTrips(current);
    });
});

[fromInput, toInput, departureInput, returnInput].forEach(input => {
    input.addEventListener("input", () => {
        formMessage.textContent = "";
    });
});

loginBtn.addEventListener("click", () => {
    alert("Trang Đăng nhập Smartbus sẽ được mở tại đây.");
});
