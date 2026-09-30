const PRICE_PER_SEAT = 120000;
const MAX_SEATS = 5;

// Trạng thái mẫu để demo giao diện.
// Sau này BE có thể trả danh sách ghế đã đặt từ database/API.
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

const seatMap = document.getElementById("seatMap");
const selectedList = document.getElementById("selectedList");
const seatCount = document.getElementById("seatCount");
const totalPrice = document.getElementById("totalPrice");
const continueBtn = document.getElementById("continueBtn");

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN").format(value) + "đ";
}

function renderSeats() {
    seatMap.innerHTML = "";

    seats.forEach((seat, index) => {
        // Tạo khoảng trống ở giữa để mô phỏng lối đi 2+1.
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

        if (seat.status !== "available") {
            button.disabled = true;
        }

        if (selectedSeats.has(seat.id)) {
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
        unavailable: "không bán"
    };

    return statusMap[status] || "";
}

function toggleSeat(seatId) {
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
    continueBtn.disabled = count === 0;

    if (count === 0) {
        selectedList.innerHTML =
            '<span class="empty-selection">Bạn chưa chọn ghế nào</span>';
        return;
    }

    selectedList.innerHTML = "";

    [...selectedSeats].sort().forEach((seatId) => {
        const chip = document.createElement("span");
        chip.className = "selected-chip";
        chip.textContent = seatId;
        selectedList.appendChild(chip);
    });
}

continueBtn.addEventListener("click", () => {
    const selected = [...selectedSeats].sort();

    if (selected.length === 0) {
        return;
    }

    alert(
        `Bạn đã chọn: ${selected.join(", ")}\n` +
        `Tổng tiền: ${formatMoney(selected.length * PRICE_PER_SEAT)}`
    );

    // Sau này có thể thay alert bằng:
    // window.location.href = "../thanh-toan/index.html";
});

renderSeats();
renderSummary();
