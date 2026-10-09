// ==========================================
// CẤU HÌNH & KHỞI TẠO THAM SỐ
// ==========================================
const urlParams = new URLSearchParams(window.location.search);
const TRIP_ID = (urlParams.get("tripId") || "TRIP01").trim().toUpperCase();
const USER_ID = urlParams.get("userId") || "usr-cus-001";
const MAX_SEATS = 5;
const API_BASE_URL = "/api/SeatBooking";

let seats = [];
const selectedSeats = new Set();

const seatMap = document.getElementById("seatMap");
const selectedList = document.getElementById("selectedList");
const seatCount = document.getElementById("seatCount");
const totalPrice = document.getElementById("totalPrice");
const continueBtn = document.getElementById("continueBtn");

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN").format(value) + "đ";
}

function getStatusText(status) {
    const statusMap = {
        available: "còn trống",
        held: "đang được giữ",
        booked: "đã bán",
        unavailable: "không thể chọn"
    };
    return statusMap[status] || "";
}

// ==========================================
// 1. TẢI SƠ ĐỒ GHẾ TỪ BACKEND
// ==========================================
async function fetchSeatMap() {
    try {
        const response = await fetch(`${API_BASE_URL}/seat-map/${TRIP_ID}`);
        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || "Không tìm thấy sơ đồ chuyến xe!");
        }

        const data = await response.json();

        // Nhận diện trạng thái ghế linh hoạt (chữ hoặc số enum)
        seats = (data.seats || []).map(s => {
            let statusStr = "available";
            const st = String(s.status || "").toLowerCase();

            if (s.status === 1 || st === "held" || s.isHeld) {
                statusStr = "held";
            } else if (s.status === 2 || st === "booked" || st === "occupied" || s.isBooked) {
                statusStr = "booked";
            }

            return {
                id: s.seatNumber || s.seatId,
                row: s.row,
                col: s.column,
                price: s.price || 120000,
                status: statusStr
            };
        });

        // Hủy chọn nếu ghế đang giữ/đã bán
        for (const seatId of selectedSeats) {
            const current = seats.find(s => s.id === seatId);
            if (!current || current.status !== "available") {
                selectedSeats.delete(seatId);
            }
        }

        renderSeats();
        renderSummary();
    } catch (error) {
        console.error("Lỗi khi tải sơ đồ ghế:", error);
        alert(error.message || "Không thể kết nối đến máy chủ lấy sơ đồ ghế!");
    }
}

// ==========================================
// 2. HIỂN THỊ SƠ ĐỒ GHẾ LÊN GIAO DIỆN
// ==========================================
function renderSeats() {
    if (!seatMap) return;
    seatMap.innerHTML = "";

    seats.forEach((seat, index) => {
        if (index % 4 === 2) {
            const aisle = document.createElement("div");
            aisle.className = "aisle";
            seatMap.appendChild(aisle);
        }

        const button = document.createElement("button");
        button.type = "button";
        button.className = `seat ${seat.status}`;
        button.textContent = seat.id;
        button.dataset.seatId = seat.id;
        button.setAttribute("aria-label", `Ghế ${seat.id} ${getStatusText(seat.status)}`);

        // Khóa nếu không phải Available
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

function toggleSeat(seatId) {
    if (selectedSeats.has(seatId)) {
        selectedSeats.delete(seatId);
    } else {
        if (selectedSeats.size >= MAX_SEATS) {
            alert(`Bạn chỉ có thể chọn tối đa ${MAX_SEATS} ghế mỗi lượt.`);
            return;
        }
        selectedSeats.add(seatId);
    }

    renderSeats();
    renderSummary();
}

function renderSummary() {
    const count = selectedSeats.size;
    let total = 0;

    selectedSeats.forEach(seatId => {
        const found = seats.find(s => s.id === seatId);
        total += found ? found.price : 120000;
    });

    if (seatCount) seatCount.textContent = `${count} ghế`;
    if (totalPrice) totalPrice.textContent = formatMoney(total);
    if (continueBtn) continueBtn.disabled = count === 0;

    if (!selectedList) return;

    if (count === 0) {
        selectedList.innerHTML = '<span class="empty-selection">Bạn chưa chọn ghế nào</span>';
        return;
    }

    selectedList.innerHTML = "";
    [...selectedSeats].sort().forEach(seatId => {
        const chip = document.createElement("span");
        chip.className = "selected-chip";
        chip.textContent = seatId;
        selectedList.appendChild(chip);
    });
}

// ==========================================
// 3. XỬ LÝ SỰ KIỆN TIẾP TỤC (TẠO SEATHOLD)
// ==========================================
if (continueBtn) {
    continueBtn.addEventListener("click", async () => {
        const selected = [...selectedSeats].sort();
        if (selected.length === 0) return;

        continueBtn.disabled = true;
        const originalText = continueBtn.textContent;
        continueBtn.textContent = "Đang giữ chỗ...";

        try {
            const response = await fetch(`${API_BASE_URL}/hold`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    tripId: TRIP_ID,
                    userId: USER_ID,
                    seatIds: selected,
                    holdDurationMinutes: 10
                })
            });

            const result = await response.json();

            if (response.ok && (result.status === "SUCCESS" || result.holdId)) {
                let total = 0;
                selected.forEach(seatId => {
                    const found = seats.find(s => s.id === seatId);
                    total += found ? found.price : 120000;
                });

                const params = new URLSearchParams({
                    tripId: TRIP_ID,
                    holdId: result.holdId,
                    seats: selected.join(","),
                    total: String(total),
                    expiresAt: result.expiresAt
                });

                window.location.href = `../thanh-toan/index.html?${params.toString()}`;
            } else {
                alert(result.message || "Vị trí ghế này vừa được người khác giữ. Vui lòng chọn ghế khác!");
                await fetchSeatMap();
            }
        } catch (error) {
            console.error("Lỗi khi giữ chỗ:", error);
            alert("Có lỗi xảy ra khi kết nối máy chủ! Vui lòng thử lại.");
        } finally {
            continueBtn.disabled = selectedSeats.size === 0;
            continueBtn.textContent = originalText;
        }
    });
}

// Khởi chạy
fetchSeatMap();