// @ts-nocheck
const fromInput = document.getElementById("from");
const toInput = document.getElementById("to");
const departureInput = document.getElementById("departure");
const returnInput = document.getElementById("returnDate");
const clearReturn = document.getElementById("clearReturn");
const swapBtn = document.getElementById("swapBtn");
const searchBtn = document.getElementById("searchBtn");
const formMessage = document.getElementById("formMessage");
const loginBtn = document.getElementById("loginBtn");

// Mảng danh sách trạm lấy động từ Database SQLite
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

// Cấu hình ngày mặc định
const today = new Date();
const todayString = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
if (departureInput) departureInput.min = todayString;
if (returnInput) returnInput.min = todayString;

const tomorrow = new Date(today);
tomorrow.setDate(tomorrow.getDate() + 1);
if (departureInput && !departureInput.value) {
    departureInput.value = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, "0")}-${String(tomorrow.getDate()).padStart(2, "0")}`;
}

// Dropdown hiển thị danh sách trạm
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
    box.style.marginTop = "5px";
    box.style.maxHeight = "250px";
    box.style.overflowY = "auto";
    box.style.display = "none";

    let parent = input.parentElement;
    while (parent && parent !== document.body) {
        parent.style.overflow = "visible";
        parent = parent.parentElement;
    }

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
        const val = input.value.trim().toLowerCase();
        renderList(val ? locations.filter(l => l.toLowerCase().includes(val)) : locations);
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

if (searchBtn) {
    searchBtn.addEventListener("click", () => {
        if (formMessage) formMessage.textContent = "";

        const from = fromInput ? fromInput.value.trim() : "";
        const to = toInput ? toInput.value.trim() : "";
        const departure = departureInput ? departureInput.value : "";
        const returnDate = returnInput ? returnInput.value : "";

        if (!from || !to) {
            if (formMessage) {
                formMessage.style.color = "#e25858";
                formMessage.textContent = "Vui lòng chọn nơi xuất phát và nơi đến.";
            }
            return;
        }

        if (from.toLowerCase() === to.toLowerCase()) {
            if (formMessage) {
                formMessage.style.color = "#e25858";
                formMessage.textContent = "Nơi xuất phát và nơi đến không được trùng nhau.";
            }
            return;
        }

        const query = new URLSearchParams({
            diemDi: from,
            diemDen: to,
            departure: departure || todayString,
            returnDate: returnDate || ""
        });

        window.location.href = `/tra-cuu/index.html?${query.toString()}`;
    });
}

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

                loginBtn.addEventListener("click", (e) => {
                    e.preventDefault();
                    if (confirm("Bạn có chắc chắn muốn đăng xuất?")) {
                        localStorage.removeItem("userSession");
                        window.location.reload();
                    }
                });
            } catch (e) {
                console.error(e);
            }
        } else {
            loginBtn.addEventListener("click", () => {
                window.location.href = "/auth/dangnhap.html";
            });
        }
    }
});