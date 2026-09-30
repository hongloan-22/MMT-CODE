const PRICE_PER_SEAT = 120000;
const MAX_SEATS = 5;
const HOLD_DURATION_SECONDS = 10 * 60;

// Demo frontend theo Product Backlog:
// "Giữ chỗ tạm thời" trong 10 phút khi khách chuyển sang thanh toán.
// ERD của SmartBus dùng trạng thái vé: GiuCho, DaThanhToan, DaSoat, DaHuy.
const seats = [
    { id: "A01", status: "available" },
    { id: "A02", status: "booked" },
    { id: "A03", status: "available" },
    { id: "A04", status: "available" },
    { id: "A05", status: "unavailable" },

    { id: "B01", status: "available" },
    { id: "B02", status: "available" },
    { id: "B03", status: "booked" },
    { id: "B04", status: "available" },
    { id: "B05", status: "available" },

    { id: "C01", status: "booked" },
    { id: "C02", status: "available" },
    { id: "C03", status: "available" },
    { id: "C04", status: "booked" },
    { id: "C05", status: "available" },

    { id: "D01", status: "available" },
    { id: "D02", status: "available" },
    { id: "D03", status: "available" },
    { id: "D04", status: "available" },
    { id: "D05", status: "booked" },

    { id: "E01", status: "available" },
    { id: "E02", status: "unavailable" },
    { id: "E03", status: "available" },
    { id: "E04", status: "available" },
    { id: "E05", status: "available" },

    { id: "F01", status: "available" },
    { id: "F02", status: "booked" },
    { id: "F03", status: "available" },
    { id: "F04", status: "available" },
    { id: "F05", status: "available" }
];

const selectedSeats = new Set();
let holdTimer = null;
let remainingSeconds = HOLD_DURATION_SECONDS;
let isHolding = false;

const seatMap = document.getElementById("seatMap");
const selectedList = document.getElementById("selectedList");
const seatCount = document.getElementById("seatCount");
const totalPrice = document.getElementById("totalPrice");
const continueBtn = document.getElementById("continueBtn");
const cancelHoldBtn = document.getElementById("cancelHoldBtn");
const holdBanner = document.getElementById("holdBanner");
const holdTitle = document.getElementById("holdTitle");
const holdMessage = document.getElementById("holdMessage");
const countdown = document.getElementById("countdown");
const ticketStatus = document.getElementById("ticketStatus");
const ticketStatusText = document.getElementById("ticketStatusText");

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN").format(value) + "đ";
}

function formatTime(totalSeconds) {
    const minutes = Math.floor(totalSeconds / 60).toString().padStart(2, "0");
    const seconds = (totalSeconds % 60).toString().padStart(2, "0");
    return `${minutes}:${seconds}`;
}

function renderSeats() {
    seatMap.innerHTML = "";

    seats.forEach((seat, index) => {
        // Tạo khoảng trống giữa hai cụm ghế để mô phỏng lối đi 2+1.
        if (index % 5 === 2) {
            const aisle = document.createElement("div");
            aisle.className = "aisle";
            seatMap.appendChild(aisle);
        }

        const button = document.createElement("button");
        button.type = "button";
        button.className = `seat ${seat.status}`;
        button.textContent = seat.id;
        button.dataset.seatId = seat.id;
        button.setAttribute(
            "aria-label",
            `Ghế ${seat.id} - ${getStatusText(seat.status)}`
        );

        // Đã đặt, đang giữ chỗ hoặc không bán đều không thể chọn lại.
        if (seat.status !== "available") {
            button.disabled = true;
        }

        if (selectedSeats.has(seat.id) && !isHolding) {
            button.classList.add("selected");
        }

        button.addEventListener("click", () => toggleSeat(seat.id));
        seatMap.appendChild(button);
    });
}

function getStatusText(status) {
    const statusMap = {
        available: "còn trống",
        booked: "đã đặt",
        holding: "đang giữ chỗ",
        unavailable: "không bán"
    };

    return statusMap[status] || "";
}

function toggleSeat(seatId) {
    if (isHolding) {
        return;
    }

    if (selectedSeats.has(seatId)) {
        selectedSeats.delete(seatId);
    } else {
        if (selectedSeats.size >= MAX_SEATS) {
            alert(`Bạn chỉ có thể chọn tối đa ${MAX_SEATS} ghế.`);
            return;
        }

        selectedSeats.add(seatId);
    }

    renderSeats();
    renderSummary();
}

function renderSummary() {
    const count = selectedSeats.size;

    seatCount.textContent = `${count} ghế`;
    totalPrice.textContent = formatMoney(count * PRICE_PER_SEAT);
    continueBtn.disabled = count === 0 || isHolding;

    if (isHolding) {
        continueBtn.disabled = true;
        continueBtn.innerHTML = `Đang giữ chỗ <span>✓</span>`;
    } else {
        continueBtn.innerHTML = `Tiếp tục thanh toán <span>→</span>`;
    }

    if (count === 0) {
        selectedList.innerHTML =
            '<span class="empty-selection">Bạn chưa chọn ghế nào</span>';
        return;
    }

    selectedList.innerHTML = "";

    [...selectedSeats].sort().forEach((seatId) => {
        const chip = document.createElement("span");
        chip.className = `selected-chip${isHolding ? " holding-chip" : ""}`;
        chip.textContent = seatId;
        selectedList.appendChild(chip);
    });
}

function startHold() {
    if (selectedSeats.size === 0 || isHolding) {
        return;
    }

    isHolding = true;
    remainingSeconds = HOLD_DURATION_SECONDS;

    // Chuyển các ghế người dùng chọn sang trạng thái "holding".
    seats.forEach((seat) => {
        if (selectedSeats.has(seat.id)) {
            seat.status = "holding";
        }
    });

    updateHoldUI();
    renderSeats();
    renderSummary();

    holdTimer = setInterval(() => {
        remainingSeconds -= 1;
        updateHoldUI();

        if (remainingSeconds <= 0) {
            expireHold();
        }
    }, 1000);
}

function updateHoldUI() {
    countdown.hidden = !isHolding;

    if (!isHolding) {
        holdBanner.classList.remove("active", "expired");
        holdTitle.textContent = "Chưa bắt đầu giữ chỗ";
        holdMessage.textContent =
            "Ghế chỉ được giữ tạm thời khi bạn tiếp tục sang bước thanh toán.";
        ticketStatus.className = "status-badge status-pending";
        ticketStatus.textContent = "Chưa giữ chỗ";
        ticketStatusText.textContent =
            "Chọn ghế rồi nhấn “Tiếp tục thanh toán” để bắt đầu giữ chỗ 10 phút.";
        cancelHoldBtn.hidden = true;
        return;
    }

    holdBanner.classList.add("active");
    holdBanner.classList.remove("expired");
    holdTitle.textContent = "Ghế đang được giữ tạm thời";
    holdMessage.textContent =
        "Hoàn tất thanh toán trước khi đồng hồ về 00:00 để giữ vé.";
    countdown.textContent = formatTime(remainingSeconds);
    countdown.classList.toggle("warning", remainingSeconds <= 60);

    ticketStatus.className = "status-badge status-holding";
    ticketStatus.textContent = "GiuCho";
    ticketStatusText.textContent =
        `Hệ thống đang giữ ${selectedSeats.size} ghế cho bạn trong thời gian còn lại.`;
    cancelHoldBtn.hidden = false;
}

function expireHold() {
    clearInterval(holdTimer);
    holdTimer = null;
    isHolding = false;

    seats.forEach((seat) => {
        if (selectedSeats.has(seat.id)) {
            seat.status = "available";
        }
    });

    selectedSeats.clear();

    holdBanner.classList.remove("active");
    holdBanner.classList.add("expired");
    countdown.hidden = true;
    holdTitle.textContent = "Hết thời gian giữ chỗ";
    holdMessage.textContent =
        "Các ghế chưa thanh toán đã được trả lại trạng thái còn trống.";
    ticketStatus.className = "status-badge status-expired";
    ticketStatus.textContent = "Hết hạn";
    ticketStatusText.textContent =
        "Vui lòng chọn lại ghế nếu bạn vẫn muốn tiếp tục đặt vé.";
    cancelHoldBtn.hidden = true;

    renderSeats();
    renderSummary();
}

function cancelHold() {
    if (!isHolding) {
        return;
    }

    clearInterval(holdTimer);
    holdTimer = null;
    isHolding = false;

    seats.forEach((seat) => {
        if (selectedSeats.has(seat.id)) {
            seat.status = "available";
        }
    });

    selectedSeats.clear();
    remainingSeconds = HOLD_DURATION_SECONDS;

    holdBanner.classList.remove("active", "expired");
    countdown.classList.remove("warning");
    countdown.hidden = true;

    updateHoldUI();
    renderSeats();
    renderSummary();
}

continueBtn.addEventListener("click", startHold);
cancelHoldBtn.addEventListener("click", cancelHold);

renderSeats();
renderSummary();
updateHoldUI();
