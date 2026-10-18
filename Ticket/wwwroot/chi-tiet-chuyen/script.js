/* =========================================================
   CHI TIẾT CHUYẾN XE - DỮ LIỆU LẤY TỪ API
   ========================================================= */

let tripData = null;
let occupancyData = null;
let tickets = [];

const statusText = {
    confirmed: "Đã xác nhận",
    pending: "Chờ thanh toán",
    cancelled: "Đã hủy"
};

const $ = id => document.getElementById(id);

function getSession() {
    try {
        return JSON.parse(localStorage.getItem("userSession") || "{}");
    } catch {
        return {};
    }
}

function authHeaders() {
    const session = getSession();
    return {
        "X-User-Id": session.userId || session.UserId || session.userID || ""
    };
}

function getTripId() {
    const params = new URLSearchParams(window.location.search);
    const fromQuery = params.get("tripId") || params.get("id");
    const value = fromQuery || localStorage.getItem("selectedTripId") || "";
    const id = Number(value);
    return Number.isInteger(id) && id > 0 ? id : null;
}

async function fetchJson(url) {
    const response = await fetch(url, { headers: authHeaders() });
    let body = null;
    try {
        body = await response.json();
    } catch {
        body = null;
    }

    if (!response.ok) {
        const message = body?.message || `Gọi API thất bại (HTTP ${response.status}).`;
        throw new Error(message);
    }

    return body;
}

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN", {
        maximumFractionDigits: 0
    }).format(Number(value || 0)) + " đ";
}

function formatMoneyCompact(value) {
    const amount = Number(value || 0);
    if (amount >= 1_000_000) {
        return (amount / 1_000_000).toLocaleString("vi-VN", {
            maximumFractionDigits: 2
        }) + " triệu";
    }
    if (amount >= 1_000) {
        return (amount / 1_000).toLocaleString("vi-VN", {
            maximumFractionDigits: 1
        }) + " nghìn";
    }
    return formatMoney(amount);
}

function formatDate(value) {
    if (!value) return "—";
    const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) return `${match[3]}/${match[2]}/${match[1]}`;

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return String(value);
    return new Intl.DateTimeFormat("vi-VN").format(parsed);
}

function formatWeekday(value) {
    if (!value) return "—";
    const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (!match) return "—";
    const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    const result = new Intl.DateTimeFormat("vi-VN", { weekday: "long" }).format(date);
    return result.charAt(0).toLocaleUpperCase("vi-VN") + result.slice(1);
}

function showToast(message) {
    const toast = $("toast");
    if (!toast) return;

    toast.textContent = message;
    toast.classList.add("show");
    clearTimeout(window.__toastTimer);
    window.__toastTimer = setTimeout(() => toast.classList.remove("show"), 2500);
}

function setText(id, value) {
    const element = $(id);
    if (element) element.textContent = value == null || value === "" ? "—" : String(value);
}

function statusLabel(status, isActive = true) {
    const value = String(status || "").toUpperCase();
    if (!isActive || value === "CANCELLED") return "Đã hủy";
    if (value === "SCHEDULED") return "Đã lên lịch";
    if (value === "ACTIVE") return "Đang hoạt động";
    return status || "Chưa xác định";
}

function renderTripDetails() {
    if (!tripData) return;

    const origin = tripData.originStopName || "Chưa xác định";
    const destination = tripData.destinationStopName || "Chưa xác định";
    const date = formatDate(tripData.tripDate);
    const departure = tripData.departureTime || "—";
    const arrival = tripData.arrivalTime || "—";
    const stateText = statusLabel(tripData.status, tripData.isActive);

    ["routeCodeHeader", "routeCodeInfo"].forEach(id => setText(id, tripData.routeCode || "—"));
    ["originStopHeader", "originStopInfo"].forEach(id => setText(id, origin));
    ["destinationStopHeader", "destinationStopInfo"].forEach(id => setText(id, destination));
    ["tripCodeHeader", "tripCodeInfo"].forEach(id => setText(id, tripData.tripCode || `#${tripData.tripId}`));
    ["tripDateHeader", "tripDateInfo"].forEach(id => setText(id, date));
    setText("tripDayHeader", formatWeekday(tripData.tripDate));
    setText("departureTimeHeader", departure);
    setText("arrivalTimeHeader", arrival);
    setText("tripTimeInfo", `${departure} - ${arrival}`);
    setText("busCode", tripData.busCode || "Chưa phân xe");
    setText("busSeatCount", occupancyData?.totalSeats ?? tripData.totalSeats ?? 0);
    setText("licensePlate", tripData.licensePlate || "Chưa có thông tin");
    setText("driverName", tripData.driverName || "Chưa phân công");
    setText("attendantName", tripData.attendantName || "Chưa phân công");

    const headerStatus = $("tripStatusHeader");
    if (headerStatus) {
        headerStatus.textContent = `● ${stateText}`;
        headerStatus.className = `trip-status ${(!tripData.isActive || String(tripData.status).toUpperCase() === "CANCELLED") ? "" : "active"}`.trim();
    }

    const infoStatus = $("tripStatusInfo");
    if (infoStatus) {
        infoStatus.textContent = stateText;
        infoStatus.className = `status ${(!tripData.isActive || String(tripData.status).toUpperCase() === "CANCELLED") ? "muted" : "active"}`;
    }
}

function renderStatistics() {
    if (!occupancyData) return;

    const total = Number(occupancyData.totalSeats || 0);
    const booked = Number(occupancyData.bookedSeats || 0);
    const held = Number(occupancyData.heldSeats || 0);
    const available = Number(occupancyData.availableSeats || 0);
    const occupancy = Number(occupancyData.occupancyRate || 0);
    const percentText = `${occupancy.toLocaleString("vi-VN", { maximumFractionDigits: 2 })}%`;

    setText("bookedTickets", booked);
    setText("availableSeats", available);
    setText("occupancyRate", percentText);
    setText("seatPercent", percentText);
    setText("tripRevenue", formatMoneyCompact(tripData?.revenue || 0));
    setText("seatProgressLabel", `${booked} / ${total}`);
    setText("breakdownBooked", booked);
    setText("breakdownAvailable", available);
    setText("breakdownHeld", held);

    const barWidth = Math.max(0, Math.min(100, occupancy));
    const progressBar = $("seatProgressBar");
    if (progressBar) progressBar.style.width = `${barWidth}%`;

    const progress = document.querySelector(".progress-track span");
    if (progress) progress.style.width = `${barWidth}%`;
}

function ticketStatusCounts() {
    return tickets.reduce((counts, ticket) => {
        const status = statusText[ticket.status] ? ticket.status : "pending";
        counts[status] += 1;
        return counts;
    }, { confirmed: 0, pending: 0, cancelled: 0 });
}

function renderTicketSummary() {
    const counts = ticketStatusCounts();
    setText("ticketCountBadge", `${tickets.length} vé`);
    setText("ticketStatConfirmed", counts.confirmed);
    setText("ticketStatPending", counts.pending);
    setText("ticketStatCancelled", counts.cancelled);
    setText("ticketStatTotal", tickets.length);
}

function renderTickets(data) {
    const tbody = $("ticketTableBody");
    const empty = $("tableEmpty");
    if (!tbody) return;

    tbody.replaceChildren();

    if (!data.length) {
        if (empty) empty.style.display = "flex";
        return;
    }

    if (empty) empty.style.display = "none";

    data.forEach(ticket => {
        const tr = document.createElement("tr");
        const cells = [ticket.code, ticket.passenger, ticket.seat, formatMoney(ticket.price)];
        cells.forEach(value => {
            const td = document.createElement("td");
            td.textContent = value == null ? "—" : String(value);
            tr.appendChild(td);
        });

        const statusCell = document.createElement("td");
        const statusSpan = document.createElement("span");
        statusSpan.className = `ticket-status ${ticket.status || "pending"}`;
        statusSpan.textContent = statusText[ticket.status] || "Chờ xử lý";
        statusCell.appendChild(statusSpan);
        tr.appendChild(statusCell);

        const actionCell = document.createElement("td");
        const button = document.createElement("button");
        button.className = "mini-action";
        button.type = "button";
        button.textContent = "Xem";
        button.dataset.ticket = ticket.code || "";
        button.addEventListener("click", () => {
            showToast(`Mã vé ${ticket.code || "—"} · Ghế ${ticket.seat || "—"} · ${statusText[ticket.status] || "Chưa xác định"}`);
        });
        actionCell.appendChild(button);
        tr.appendChild(actionCell);

        tbody.appendChild(tr);
    });
}

function filterTickets() {
    const searchInput = $("ticketSearch");
    const statusSelect = $("ticketStatus");
    const keyword = String(searchInput?.value || "").trim().toLocaleLowerCase("vi-VN");
    const status = statusSelect?.value || "all";

    const filtered = tickets.filter(ticket => {
        const searchText = [ticket.code, ticket.passenger, ticket.seat]
            .join(" ")
            .toLocaleLowerCase("vi-VN");
        return (!keyword || searchText.includes(keyword)) &&
            (status === "all" || ticket.status === status);
    });

    renderTickets(filtered);
}

function exportTickets() {
    if (!tickets.length) {
        showToast("Chuyến xe chưa có dữ liệu vé để xuất.");
        return;
    }

    const rows = [["Mã vé", "Hành khách", "Ghế", "Giá vé", "Trạng thái"]];
    tickets.forEach(ticket => rows.push([
        ticket.code || "",
        ticket.passenger || "",
        ticket.seat || "",
        ticket.price ?? 0,
        statusText[ticket.status] || "Chưa xác định"
    ]));

    const csv = rows.map(row => row.map(value =>
        `"${String(value).replaceAll('"', '""')}"`
    ).join(",")).join("\r\n");

    const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `danh-sach-ve-${tripData?.tripCode || tripData?.tripId || "chuyen-xe"}.csv`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    showToast("Đã xuất danh sách vé thực tế.");
}

function initEvents() {
    $("ticketSearch")?.addEventListener("input", filterTickets);
    $("ticketStatus")?.addEventListener("change", filterTickets);
    $("exportBtn")?.addEventListener("click", exportTickets);

    $("backBtn")?.addEventListener("click", () => {
        window.location.href = "../lap-lich-trinh/index.html";
    });

    $("vehicleBtn")?.addEventListener("click", () => {
        window.location.href = "../doi-xe/index.html";
    });

    document.querySelector(".logout-btn")?.addEventListener("click", () => {
        localStorage.removeItem("userSession");
        window.location.href = "../auth/dangnhap.html";
    });
}

async function loadTripData() {
    const tripId = getTripId();
    if (!tripId) {
        showToast("Không có tripId. Hãy mở chi tiết từ danh sách chuyến.");
        return;
    }

    try {
        const [detail, occupancy] = await Promise.all([
            fetchJson(`/api/Statistics/trip/${tripId}/detail`),
            fetchJson(`/api/Statistics/trip/${tripId}/occupancy`)
        ]);

        tripData = detail;
        occupancyData = occupancy;
        tickets = Array.isArray(detail.tickets) ? detail.tickets : [];

        renderTripDetails();
        renderStatistics();
        renderTicketSummary();
        filterTickets();
    } catch (error) {
        console.error("Không tải được dữ liệu chi tiết chuyến:", error);
        showToast(error.message || "Không tải được dữ liệu chuyến xe.");
    }
}

document.addEventListener("DOMContentLoaded", () => {
    initEvents();
    loadTripData();
});
