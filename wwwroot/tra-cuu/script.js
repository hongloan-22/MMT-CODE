// @ts-nocheck
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

// Mảng danh sách trạm lấy động từ Database SQLite qua API BusStops
let locations = [];

async function loadStationsFromDB() {
    try {
        const response = await fetch("/api/BusStops");
        if (response.ok) {
            const data = await response.json();
            if (Array.isArray(data) && data.length > 0) {
                locations = data.map(item => item.name || item.Name || item.stationName || "").filter(Boolean);
            }
        }
    } catch (err) {
        console.warn("Chưa tải được trạm từ CSDL qua /api/BusStops:", err);
    }
}
loadStationsFromDB();

let currentTripsData = [];

const today = new Date();
const todayString = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
if (departureInput) departureInput.min = todayString;

function formatDate(value) {
    if (!value) return "";
    const parts = value.split("-");
    if (parts.length === 3) return `${parts[2]}/${parts[1]}/${parts[0]}`;
    return value;
}

// Hàm chuẩn hóa tiếng Việt không dấu để tìm kiếm tiện lợi
function removeVietnameseTones(str) {
    if (!str) return "";
    return str
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d").replace(/Đ/g, "D")
        .toLowerCase();
}

// 1. DROPDOWN GỢI Ý ĐỊA ĐIỂM
function setupSuggestions(inputId, boxId) {
    const input = document.getElementById(inputId);
    let box = document.getElementById(boxId);
    if (!input) return;

    if (!box) {
        box = document.createElement("div");
        box.id = boxId;
        input.parentElement.appendChild(box);
    }

    input.setAttribute("autocomplete", "off");

    box.parentElement.style.position = "relative";
    box.style.position = "absolute";
    box.style.top = "100%";
    box.style.left = "0";
    box.style.width = "100%";
    box.style.backgroundColor = "#fff";
    box.style.border = "1px solid #087df5";
    box.style.borderRadius = "8px";
    box.style.boxShadow = "0 10px 30px rgba(0,0,0,0.2)";
    box.style.zIndex = "999999";
    box.style.marginTop = "4px";
    box.style.maxHeight = "250px";
    box.style.overflowY = "auto";
    box.style.display = "none";

    function renderList(list) {
        box.innerHTML = "";
        if (list.length === 0) {
            box.style.display = "none";
            return;
        }

        list.forEach(loc => {
            const item = document.createElement("div");
            item.textContent = loc;
            item.style.padding = "14px 18px";
            item.style.cursor = "pointer";
            item.style.borderBottom = "1px solid #f0f5fa";
            item.style.color = "#12345b";
            item.style.fontWeight = "bold";

            item.addEventListener("mouseenter", () => item.style.backgroundColor = "#eef5fc");
            item.addEventListener("mouseleave", () => item.style.backgroundColor = "transparent");

            item.addEventListener("mousedown", (e) => {
                e.preventDefault();
                input.value = loc;
                box.style.display = "none";
            });

            box.appendChild(item);
        });
        box.style.display = "block";
    }

    function showList() {
        const val = removeVietnameseTones(input.value.trim());
        renderList(val ? locations.filter(l => removeVietnameseTones(l).includes(val)) : locations);
    }

    if (input.parentElement) {
        input.parentElement.addEventListener("click", () => {
            input.focus();
            showList();
        });
    }

    input.addEventListener("focus", showList);
    input.addEventListener("click", showList);
    input.addEventListener("input", showList);

    document.addEventListener("click", (e) => {
        if (e.target !== input && e.target !== input.parentElement && !box.contains(e.target)) {
            box.style.display = "none";
        }
    });
}

setupSuggestions("from", "fromSuggestions");
setupSuggestions("to", "toSuggestions");

if (swapBtn && fromInput && toInput) {
    swapBtn.addEventListener("click", () => {
        const temp = fromInput.value;
        fromInput.value = toInput.value;
        toInput.value = temp;
    });
}

// 2. RENDER DANH SÁCH CHUYẾN XE (ĐIỀU HƯỚNG SANG CHỌN GHẾ)
function renderTrips(list) {
    if (!tripList || !resultCount) return;
    resultCount.textContent = `${list.length} chuyến`;

    if (!list.length) {
        tripList.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">🚌</div>
                <h3>Không tìm thấy tuyến phù hợp</h3>
                <p>Hệ thống không tìm thấy tuyến có thứ tự trạm chạy từ <b>${fromInput?.value || ""}</b> đến <b>${toInput?.value || ""}</b>.</p>
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
                <span class="trip-status" style="background:#e8f8f0; color:#27ae60;">Đang hoạt động</span>
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
                    <span>✓ Lộ trình đúng chiều</span>
                    <span>▣ Cự ly: ${trip.distance} km</span>
                    <span>💺 Còn ${trip.seats} chỗ</span>
                </div>
                <div>
                    <span class="trip-price">${trip.price.toLocaleString("vi-VN")}đ</span>
                    <button class="select-trip-btn" data-code="${trip.code}">Chi tiết tuyến</button>
                </div>
            </div>
        </article>
    `).join("");

    // Điều hướng sang trang chọn ghế khi nhấn nút
    document.querySelectorAll(".select-trip-btn").forEach(button => {
        button.addEventListener("click", () => {
            const code = button.dataset.code;
            const from = encodeURIComponent(fromInput?.value || "");
            const to = encodeURIComponent(toInput?.value || "");
            const depDate = departureInput?.value || todayString;
            window.location.href = `/chon-ghe/index.html?tripCode=${code}&from=${from}&to=${to}&date=${depDate}`;
        });
    });
}

// 3. BỘ LỌC THỜI GIAN & GIÁ VÉ (US-33)
function getFilteredTrips() {
    const timeRadio = document.querySelector('input[name="timeFilter"]:checked');
    const priceRadio = document.querySelector('input[name="priceFilter"]:checked');
    const time = timeRadio ? timeRadio.value : "all";
    const price = priceRadio ? priceRadio.value : "all";

    return currentTripsData.filter(trip => {
        const timeOk = time === "all" || trip.timeCategory === time;
        const priceOk =
            price === "all" ||
            (price === "low" && trip.price < 100000) ||
            (price === "mid" && trip.price >= 100000 && trip.price <= 150000) ||
            (price === "high" && trip.price > 150000);
        return timeOk && priceOk;
    });
}

function getTimeCategory(timeStr) {
    if (!timeStr) return "morning";
    const hour = parseInt(timeStr.split(":")[0], 10);
    if (isNaN(hour)) return "morning";
    if (hour < 12) return "morning";
    if (hour < 18) return "afternoon";
    return "evening";
}

// 4. TRA CỨU TUYẾN TỪ API (TÍNH CỰ LY VÀ GIÁ VÉ CHUẨN XÁC THEO CHẶNG)
async function executeSearch() {
    const from = fromInput ? fromInput.value.trim() : "";
    const to = toInput ? toInput.value.trim() : "";
    const departure = departureInput ? departureInput.value : "";

    if (!from || !to) {
        if (formMessage) {
            formMessage.style.color = "#e25858";
            formMessage.textContent = "Vui lòng chọn nơi xuất phát và nơi đến.";
        }
        return;
    }

    if (formMessage) {
        formMessage.style.color = "#1685e8";
        formMessage.textContent = "Đang tìm chuyến từ cơ sở dữ liệu...";
    }

    try {
        const apiUrl = `/api/TraCuuTuyenXe/theo-diem?diemDi=${encodeURIComponent(from)}&diemDen=${encodeURIComponent(to)}`;
        const response = await fetch(apiUrl);

        if (response.ok) {
            const apiTrips = await response.json();

            currentTripsData = apiTrips.map((item, idx) => {
                const departTimes = ["07:30", "13:30", "18:30"];
                const arriveTimes = ["09:30", "15:30", "20:30"];
                const depTime = departTimes[idx % departTimes.length];
                const arrTime = arriveTimes[idx % arriveTimes.length];

                const totalDist = item.totalDistanceKm || 67;
                let actualDistance = totalDist;

                // TÍNH TOÁN CỰ LY DỰA VÀO VỊ TRÍ ROUTESTOPS
                if (item.routeStops && Array.isArray(item.routeStops) && item.routeStops.length > 0) {
                    const stopsSorted = item.routeStops.slice().sort((a, b) => a.stopOrder - b.stopOrder);
                    const startIdx = stopsSorted.findIndex(rs => (rs.busStop?.name || "").trim().toLowerCase() === from.toLowerCase());
                    const endIdx = stopsSorted.findIndex(rs => (rs.busStop?.name || "").trim().toLowerCase() === to.toLowerCase());

                    if (startIdx !== -1 && endIdx !== -1 && endIdx > startIdx) {
                        const dStart = stopsSorted[startIdx].distanceFromStartKm || 0;
                        const dEnd = stopsSorted[endIdx].distanceFromStartKm || 0;
                        const delta = dEnd - dStart;

                        if (delta > 0) {
                            actualDistance = Math.round(delta * 10) / 10;
                        } else {
                            // Nếu trạm chưa gán khoảng cách cụ thể, tính tỷ lệ cự ly theo số trạm
                            const ratio = (endIdx - startIdx) / Math.max(1, stopsSorted.length - 1);
                            actualDistance = Math.round(totalDist * ratio * 10) / 10;
                        }
                    }
                } else {
                    // Nếu là chặng giữa, cự ly sẽ là một phần của tổng tuyến
                    const isFullRoute = item.routeName && item.routeName.toLowerCase().includes(from.toLowerCase()) && item.routeName.toLowerCase().includes(to.toLowerCase());
                    if (!isFullRoute) {
                        actualDistance = Math.round(totalDist * 0.6 * 10) / 10;
                    }
                }

                // Tính giá vé tương ứng theo cự ly thực tế (1.500đ / km, tối thiểu 15.000đ)
                const actualPrice = Math.max(15000, Math.round(actualDistance * 1500));

                // Ước lượng lại thời gian di chuyển theo cự ly thực tế
                const estMinutes = Math.max(30, Math.round((actualDistance / totalDist) * 90));
                const hours = Math.floor(estMinutes / 60);
                const mins = estMinutes % 60;
                const formattedDuration = `${String(hours).padStart(2, "0")}:${String(mins).padStart(2, "0")}:00`;

                return {
                    operator: item.routeName || "Smartbus Express",
                    code: `TB-${String(item.id).padStart(2, "0")}`,
                    from: from,
                    to: to,
                    depart: depTime,
                    arrive: arrTime,
                    duration: formattedDuration,
                    distance: actualDistance,
                    price: actualPrice,
                    timeCategory: getTimeCategory(depTime),
                    seats: 35
                };
            });
        } else {
            currentTripsData = [];
        }
    } catch (e) {
        console.warn("Lỗi API kết nối:", e);
        currentTripsData = [];
    }

    if (formMessage) formMessage.textContent = "";

    if (resultSummary) {
        resultSummary.textContent = `${from} → ${to} | Ngày ${formatDate(departure)}`;
    }

    const filtered = getFilteredTrips();
    renderTrips(filtered);

    const resultsSection = document.getElementById("resultsSection");
    if (resultsSection) {
        resultsSection.scrollIntoView({ behavior: "smooth" });
    }
}

if (searchBtn) {
    searchBtn.addEventListener("click", executeSearch);
}

window.addEventListener("DOMContentLoaded", () => {
    const urlParams = new URLSearchParams(window.location.search);
    const fromParam = urlParams.get("diemDi") || urlParams.get("from");
    const toParam = urlParams.get("diemDen") || urlParams.get("to");
    const depParam = urlParams.get("departure");

    if (fromParam && fromInput) fromInput.value = decodeURIComponent(fromParam);
    if (toParam && toInput) toInput.value = decodeURIComponent(toParam);
    if (depParam && departureInput) departureInput.value = depParam;

    if (fromParam && toParam) {
        executeSearch();
    }
});

// Lắng nghe sự kiện chuyển đổi bộ lọc bên sidebar trái
document.querySelectorAll('input[name="timeFilter"], input[name="priceFilter"]').forEach(input => {
    input.addEventListener("change", () => {
        const current = getFilteredTrips();
        renderTrips(current);
    });
});

[fromInput, toInput, departureInput].forEach(input => {
    if (input) {
        input.addEventListener("input", () => {
            if (formMessage) formMessage.textContent = "";
        });
    }
});

// 5. TRẠNG THÁI TÀI KHOẢN ĐĂNG NHẬP / ĐĂNG XUẤT
document.addEventListener("DOMContentLoaded", () => {
    const session = localStorage.getItem("userSession");

    if (loginBtn) {
        if (session) {
            try {
                const user = JSON.parse(session);
                loginBtn.innerHTML = `👤 ${user.fullName || "Khách"}`;
                loginBtn.style.backgroundColor = "#e8f8f0";
                loginBtn.style.color = "#27ae60";
                loginBtn.style.border = "1px solid #27ae60";
                loginBtn.title = "Nhấn để đăng xuất";

                loginBtn.onclick = (e) => {
                    e.preventDefault();
                    if (confirm("Bạn có chắc chắn muốn đăng xuất?")) {
                        localStorage.removeItem("userSession");
                        window.location.reload();
                    }
                };
            } catch (e) {
                console.error(e);
            }
        } else {
            loginBtn.onclick = () => {
                window.location.href = "/auth/dangnhap.html";
            };
        }
    }
});