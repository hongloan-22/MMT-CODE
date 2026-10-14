const tickets = [
    {
        id: "SB-20260930-00126",
        route: "Tuyến 02: Hà Nội - Thái Nguyên",
        from: "Hà Nội",
        fromDetail: "Bến xe Mỹ Đình",
        to: "Thái Nguyên",
        toDetail: "Cổng Trường ĐH ICTU",
        depart: "06:30",
        arrive: "08:00",
        date: "30/09/2026",
        dateValue: "2026-09-30",
        seat: "A03",
        passenger: "Nguyễn Văn A",
        amount: 120000,
        status: "paid",
        statusText: "Đã thanh toán",
        timeType: "upcoming"
    },
    {
        id: "SB-20261002-00418",
        route: "Tuyến 05: Hà Nội - Bắc Ninh",
        from: "Hà Nội",
        fromDetail: "Bến xe Gia Lâm",
        to: "Bắc Ninh",
        toDetail: "Bến xe Bắc Ninh",
        depart: "09:00",
        arrive: "10:15",
        date: "02/10/2026",
        dateValue: "2026-10-02",
        seat: "B02",
        passenger: "Nguyễn Văn A",
        amount: 85000,
        status: "paid",
        statusText: "Đã thanh toán",
        timeType: "upcoming"
    },
    {
        id: "SB-20260918-00072",
        route: "Tuyến 01: Thái Nguyên - Hà Nội",
        from: "Thái Nguyên",
        fromDetail: "Bến xe Thái Nguyên",
        to: "Hà Nội",
        toDetail: "Bến xe Mỹ Đình",
        depart: "14:00",
        arrive: "15:30",
        date: "18/09/2026",
        dateValue: "2026-09-18",
        seat: "C05",
        passenger: "Nguyễn Văn A",
        amount: 120000,
        status: "cancelled",
        statusText: "Đã hủy",
        timeType: "past"
    }
];

const ticketList = document.getElementById("ticketList");
const emptyState = document.getElementById("emptyState");
const searchInput = document.getElementById("searchInput");
const statusFilter = document.getElementById("statusFilter");
const dateFilter = document.getElementById("dateFilter");
const modalBackdrop = document.getElementById("modalBackdrop");
const modalContent = document.getElementById("modalContent");

const money = value => new Intl.NumberFormat("vi-VN").format(value) + "đ";

function renderTickets() {
    const keyword = searchInput.value.trim().toLowerCase();
    const status = statusFilter.value;
    const date = dateFilter.value;

    const filtered = tickets.filter(ticket => {
        const matchKeyword =
            !keyword ||
            ticket.id.toLowerCase().includes(keyword) ||
            ticket.route.toLowerCase().includes(keyword) ||
            ticket.from.toLowerCase().includes(keyword) ||
            ticket.to.toLowerCase().includes(keyword);

        const matchStatus = status === "all" || ticket.status === status;
        const matchDate = date === "all" || ticket.timeType === date;

        return matchKeyword && matchStatus && matchDate;
    });

    ticketList.innerHTML = filtered.map(createTicketCard).join("");
    emptyState.classList.toggle("hidden", filtered.length !== 0);

    document.querySelectorAll("[data-action]").forEach(button => {
        button.addEventListener("click", handleAction);
    });

    updateSummary();
}

function createTicketCard(ticket) {
    const canManage = ticket.status === "paid";

    return `
        <article class="ticket-item">
            <div class="ticket-main">
                <div>
                    <div class="ticket-head">
                        <span class="ticket-code">${ticket.id}</span>
                        <span class="status ${ticket.status}">${ticket.statusText}</span>
                    </div>

                    <div class="route">
                        <div class="station">
                            <span class="time">${ticket.depart}</span>
                            <strong>${ticket.from}</strong>
                            <small>${ticket.fromDetail}</small>
                        </div>

                        <div class="route-middle">
                            <span class="route-line"></span>
                            <span class="bus">🚌</span>
                            <span class="duration">~ 1 giờ 30 phút</span>
                        </div>

                        <div class="station to">
                            <span class="time">${ticket.arrive}</span>
                            <strong>${ticket.to}</strong>
                            <small>${ticket.toDetail}</small>
                        </div>
                    </div>

                    <div class="details">
                        <div class="detail"><small>Ngày đi</small><strong>${ticket.date}</strong></div>
                        <div class="detail"><small>Số ghế</small><strong>${ticket.seat}</strong></div>
                        <div class="detail"><small>Hành khách</small><strong>${ticket.passenger}</strong></div>
                        <div class="detail"><small>Loại vé</small><strong>Vé lượt</strong></div>
                    </div>
                </div>

                <aside class="ticket-side">
                    <span class="price-label">Tổng tiền</span>
                    <strong class="price">${money(ticket.amount)}</strong>

                    <button class="action-btn primary-btn" data-action="detail" data-id="${ticket.id}">
                        Xem chi tiết
                    </button>

                    ${canManage ? `
                        <button class="action-btn secondary-btn" data-action="change" data-id="${ticket.id}">
                            Đổi vé
                        </button>
                        <button class="action-btn danger-btn" data-action="cancel" data-id="${ticket.id}">
                            Hủy vé
                        </button>
                    ` : ""}
                </aside>
            </div>

            <div class="ticket-footer">
                Mã vé <strong>${ticket.id}</strong> · Trạng thái hiện tại được hiển thị để hành khách theo dõi.
            </div>
        </article>
    `;
}

function updateSummary() {
    document.getElementById("totalCount").textContent = tickets.length;
    document.getElementById("paidCount").textContent =
        tickets.filter(t => t.status === "paid").length;
    document.getElementById("pendingCount").textContent =
        tickets.filter(t => t.status === "pending").length;
}

function getTicket(id) {
    return tickets.find(ticket => ticket.id === id);
}

function handleAction(event) {
    const ticket = getTicket(event.currentTarget.dataset.id);
    if (!ticket) return;

    const action = event.currentTarget.dataset.action;

    if (action === "detail") {
        openDetail(ticket);
    } else if (action === "change") {
        openChange(ticket);
    } else if (action === "cancel") {
        openCancel(ticket);
    }
}

function openDetail(ticket) {
    modalContent.innerHTML = `
        <h2>Chi tiết vé</h2>
        <p>Thông tin vé đang được hiển thị từ dữ liệu demo của giao diện.</p>

        <div class="modal-summary">
            <strong>${ticket.id}</strong>
            <span>${ticket.route}</span>
            <span>${ticket.date} · ${ticket.depart} · Ghế ${ticket.seat}</span>
            <span>Hành khách: ${ticket.passenger}</span>
            <span>Trạng thái: ${ticket.statusText}</span>
        </div>

        <div class="modal-actions">
            <button class="secondary-btn" type="button" data-close>Đóng</button>
            ${ticket.status === "paid" ? `
                <button class="primary-btn" type="button" data-modal-action="change">Đổi vé</button>
            ` : ""}
        </div>
    `;

    bindModalButtons(() => openChange(ticket));
    showModal();
}

function openChange(ticket) {
    modalContent.innerHTML = `
        <h2>Đổi vé</h2>
        <p>Chọn thông tin mới cho chuyến đi. Đây là giao diện demo, chưa gửi yêu cầu tới API.</p>

        <div class="modal-summary">
            <strong>${ticket.id}</strong>
            <span>Vé hiện tại: ${ticket.date} · ${ticket.depart} · Ghế ${ticket.seat}</span>
        </div>

        <div class="form-group">
            <label for="newDate">Ngày đi mới</label>
            <select id="newDate">
                <option>01/10/2026</option>
                <option>02/10/2026</option>
                <option>03/10/2026</option>
            </select>
        </div>

        <div class="form-group">
            <label for="newSeat">Ghế mới</label>
            <select id="newSeat">
                <option>A04</option>
                <option>A05</option>
                <option>B03</option>
                <option>B04</option>
            </select>
        </div>

        <div class="modal-actions">
            <button class="secondary-btn" type="button" data-close>Hủy</button>
            <button class="primary-btn" type="button" id="confirmChange">Xác nhận đổi vé</button>
        </div>
    `;

    bindModalButtons();
    document.getElementById("confirmChange").addEventListener("click", () => {
        ticket.status = "pending";
        ticket.statusText = "Đang xử lý";
        closeModal();
        renderTickets();
        alert(`Đã ghi nhận yêu cầu đổi vé ${ticket.id}.`);
    });

    showModal();
}

function openCancel(ticket) {
    modalContent.innerHTML = `
        <h2>Hủy vé</h2>
        <p>Kiểm tra thông tin trước khi gửi yêu cầu hủy vé.</p>

        <div class="modal-summary">
            <strong>${ticket.id}</strong>
            <span>${ticket.route}</span>
            <span>${ticket.date} · ${ticket.depart} · Ghế ${ticket.seat}</span>
            <span>Giá vé: ${money(ticket.amount)}</span>
        </div>

        <div class="form-group">
            <label for="cancelReason">Lý do hủy vé</label>
            <textarea id="cancelReason" placeholder="Nhập lý do nếu cần..."></textarea>
        </div>

        <div class="modal-actions">
            <button class="secondary-btn" type="button" data-close>Quay lại</button>
            <button class="danger-btn" type="button" id="confirmCancel">Xác nhận hủy vé</button>
        </div>
    `;

    bindModalButtons();
    document.getElementById("confirmCancel").addEventListener("click", () => {
        ticket.status = "cancelled";
        ticket.statusText = "Đã hủy";
        ticket.timeType = "past";
        closeModal();
        renderTickets();
        alert(`Đã ghi nhận yêu cầu hủy vé ${ticket.id}.`);
    });

    showModal();
}

function bindModalButtons(afterClose) {
    document.querySelectorAll("[data-close]").forEach(button => {
        button.addEventListener("click", closeModal);
    });

    const modalAction = document.querySelector("[data-modal-action]");
    if (modalAction && afterClose) {
        modalAction.addEventListener("click", afterClose);
    }
}

function showModal() {
    modalBackdrop.classList.remove("hidden");
}

function closeModal() {
    modalBackdrop.classList.add("hidden");
}

document.getElementById("modalClose").addEventListener("click", closeModal);

modalBackdrop.addEventListener("click", event => {
    if (event.target === modalBackdrop) closeModal();
});

document.addEventListener("keydown", event => {
    if (event.key === "Escape") closeModal();
});

[searchInput, statusFilter, dateFilter].forEach(control => {
    control.addEventListener("input", renderTickets);
    control.addEventListener("change", renderTickets);
});

renderTickets();
