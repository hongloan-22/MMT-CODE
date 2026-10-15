// ============================================================
// VỊ TRÍ:
// wwwroot/quan-ly-ve/script.js
// ============================================================

const ticketList =
    document.getElementById("ticketList");

const emptyState =
    document.getElementById("emptyState");

const searchInput =
    document.getElementById("searchInput");

const statusFilter =
    document.getElementById("statusFilter");

const dateFilter =
    document.getElementById("dateFilter");

const modalBackdrop =
    document.getElementById("modalBackdrop");

const modalContent =
    document.getElementById("modalContent");

let tickets = [];


// ============================================================
// SESSION
// ============================================================

function getSession() {

    try {

        return JSON.parse(
            localStorage.getItem("userSession") || "{}"
        );

    } catch {

        return {};
    }
}


function getUserId() {

    const session =
        getSession();

    return (
        session.userId ||
        session.UserId ||
        ""
    );
}


function headers() {

    return {
        "Content-Type": "application/json",
        "X-User-Id": getUserId()
    };
}


// ============================================================
// TOAST
// ============================================================

function showToast(
    message,
    duration = 2500
) {

    let toast =
        document.getElementById("appToast");

    if (!toast) {

        toast =
            document.createElement("div");

        toast.id =
            "appToast";

        toast.className =
            "app-toast";

        document.body.appendChild(toast);
    }

    toast.textContent =
        message;

    toast.classList.add("show");

    clearTimeout(
        window.ticketToastTimer
    );

    window.ticketToastTimer =
        setTimeout(
            () => {
                toast.classList.remove("show");
            },
            duration
        );
}


// ============================================================
// FORMAT
// ============================================================

function money(value) {

    return Number(value || 0)
        .toLocaleString("vi-VN") + "đ";
}


function formatDate(value) {

    if (!value)
        return "—";

    const parts =
        String(value)
            .substring(0, 10)
            .split("-");

    if (parts.length !== 3)
        return value;

    return `${parts[2]}/${parts[1]}/${parts[0]}`;
}


function statusText(status) {

    switch (
    String(status || "").toUpperCase()
    ) {

        case "VALID":
            return "Còn hiệu lực";

        case "USED":
            return "Đã sử dụng";

        case "CANCELLED":
            return "Đã hủy";

        default:
            return status || "Không xác định";
    }
}


function statusClass(status) {

    switch (
    String(status || "").toUpperCase()
    ) {

        case "VALID":
            return "paid";

        case "USED":
            return "used";

        case "CANCELLED":
            return "cancelled";

        default:
            return "pending";
    }
}


// ============================================================
// LOAD TICKETS
// ============================================================

async function loadTickets() {

    const userId =
        getUserId();

    if (!userId) {

        window.location.href =
            "../auth/dangnhap.html";

        return;
    }

    try {

        const response =
            await fetch(
                "/api/Ticket/my",
                {
                    headers:
                        headers()
                }
            );

        const data =
            await response
                .json()
                .catch(
                    () => []
                );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể tải danh sách vé."
            );
        }

        tickets =
            Array.isArray(data)
                ? data
                : [];

        renderTickets();

    } catch (error) {

        console.error(error);

        tickets = [];

        renderTickets();

        showToast(
            error.message ||
            "Không thể tải danh sách vé."
        );
    }
}


// ============================================================
// FILTER
// ============================================================

function getFilteredTickets() {

    const keyword =
        searchInput.value
            .trim()
            .toLowerCase();

    const status =
        statusFilter.value;

    const date =
        dateFilter.value;

    const now =
        new Date();

    return tickets.filter(
        ticket => {

            const matchKeyword =
                !keyword ||
                String(
                    ticket.ticketCode || ""
                )
                    .toLowerCase()
                    .includes(keyword) ||

                String(
                    ticket.bookingCode || ""
                )
                    .toLowerCase()
                    .includes(keyword) ||

                String(
                    ticket.routeCode || ""
                )
                    .toLowerCase()
                    .includes(keyword) ||

                String(
                    ticket.routeName || ""
                )
                    .toLowerCase()
                    .includes(keyword) ||

                String(
                    ticket.departureStop || ""
                )
                    .toLowerCase()
                    .includes(keyword) ||

                String(
                    ticket.arrivalStop || ""
                )
                    .toLowerCase()
                    .includes(keyword);

            const matchStatus =
                status === "all" ||
                String(ticket.status)
                    .toUpperCase() ===
                status;

            const tripDate =
                ticket.tripDate
                    ? new Date(
                        ticket.tripDate
                    )
                    : null;

            let matchDate =
                true;

            if (date === "upcoming") {

                matchDate =
                    tripDate &&
                    tripDate >= now;
            }

            if (date === "past") {

                matchDate =
                    tripDate &&
                    tripDate < now;
            }

            return (
                matchKeyword &&
                matchStatus &&
                matchDate
            );
        }
    );
}


// ============================================================
// RENDER
// ============================================================

function renderTickets() {

    const filtered =
        getFilteredTickets();

    ticketList.innerHTML =
        filtered
            .map(createTicketCard)
            .join("");

    emptyState.classList.toggle(
        "hidden",
        filtered.length !== 0
    );

    document
        .querySelectorAll("[data-action]")
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    handleAction
                );
            }
        );

    updateSummary();
}


// ============================================================
// CARD
// ============================================================

function createTicketCard(ticket) {

    const status =
        String(
            ticket.status || ""
        ).toUpperCase();

    const canManage =
        status === "VALID";

    return `
        <article class="ticket-item">

            <div class="ticket-main">

                <div>

                    <div class="ticket-head">

                        <span class="ticket-code">
                            ${ticket.ticketCode || "—"}
                        </span>

                        <span class="status ${statusClass(status)}">
                            ${statusText(status)}
                        </span>

                    </div>


                    <div class="route">

                        <div class="station">

                            <span class="time">
                                ${ticket.departureTime || "--:--"}
                            </span>

                            <strong>
                                ${ticket.departureStop || "—"}
                            </strong>

                            <small>
                                ${ticket.routeCode || ""}
                            </small>

                        </div>


                        <div class="route-middle">

                            <span class="route-line"></span>

                            <span class="bus">
                                🚌
                            </span>

                            <span class="duration">
                                ${ticket.tripCode || ""}
                            </span>

                        </div>


                        <div class="station to">

                            <span class="time">
                                ${ticket.arrivalTime || "--:--"}
                            </span>

                            <strong>
                                ${ticket.arrivalStop || "—"}
                            </strong>

                            <small>
                                ${ticket.routeName || ""}
                            </small>

                        </div>

                    </div>


                    <div class="details">

                        <div class="detail">
                            <small>Ngày đi</small>
                            <strong>
                                ${formatDate(ticket.tripDate)}
                            </strong>
                        </div>

                        <div class="detail">
                            <small>Số ghế</small>
                            <strong>
                                ${ticket.seatCode || "—"}
                            </strong>
                        </div>

                        <div class="detail">
                            <small>Hành khách</small>
                            <strong>
                                ${ticket.passengerName || "—"}
                            </strong>
                        </div>

                        <div class="detail">
                            <small>Giá vé</small>
                            <strong>
                                ${money(ticket.fare)}
                            </strong>
                        </div>

                    </div>

                </div>


                <aside class="ticket-side">

                    <span class="price-label">
                        Giá vé
                    </span>

                    <strong class="price">
                        ${money(ticket.fare)}
                    </strong>


                    <button
                        class="action-btn primary-btn"
                        data-action="detail"
                        data-id="${ticket.ticketId}">
                        Xem chi tiết
                    </button>


                    ${canManage
            ? `
                                <button
                                    class="action-btn secondary-btn"
                                    data-action="change"
                                    data-id="${ticket.ticketId}">
                                    Đổi vé
                                </button>

                                <button
                                    class="action-btn danger-btn"
                                    data-action="cancel"
                                    data-id="${ticket.ticketId}">
                                    Hủy vé
                                </button>
                              `
            : ""
        }

                </aside>

            </div>


            <div class="ticket-footer">

                Mã booking:
                <strong>
                    ${ticket.bookingCode || "—"}
                </strong>

                ·

                Mã vé:
                <strong>
                    ${ticket.ticketCode || "—"}
                </strong>

            </div>

        </article>
    `;
}


// ============================================================
// SUMMARY
// ============================================================

function updateSummary() {

    const total =
        tickets.length;

    const valid =
        tickets.filter(
            x =>
                String(x.status)
                    .toUpperCase() ===
                "VALID"
        ).length;

    const used =
        tickets.filter(
            x =>
                String(x.status)
                    .toUpperCase() ===
                "USED"
        ).length;

    document.getElementById(
        "totalCount"
    ).textContent =
        total;

    document.getElementById(
        "paidCount"
    ).textContent =
        valid;

    document.getElementById(
        "pendingCount"
    ).textContent =
        used;
}


// ============================================================
// ACTION
// ============================================================

function getTicket(ticketId) {

    return tickets.find(
        x =>
            Number(x.ticketId) ===
            Number(ticketId)
    );
}


function handleAction(event) {

    const ticket =
        getTicket(
            event.currentTarget.dataset.id
        );

    if (!ticket)
        return;

    switch (
    event.currentTarget.dataset.action
    ) {

        case "detail":
            openDetail(ticket);
            break;

        case "change":
            openChange(ticket);
            break;

        case "cancel":
            openCancel(ticket);
            break;
    }
}


// ============================================================
// DETAIL
// ============================================================

function openDetail(ticket) {

    modalContent.innerHTML = `

        <h2>Chi tiết vé</h2>

        <div class="modal-summary">

            <strong>
                ${ticket.ticketCode || "—"}
            </strong>

            <span>
                Booking:
                ${ticket.bookingCode || "—"}
            </span>

            <span>
                ${ticket.routeCode || ""}
                -
                ${ticket.routeName || ""}
            </span>

            <span>
                ${ticket.departureStop || "—"}
                →
                ${ticket.arrivalStop || "—"}
            </span>

            <span>
                Ngày:
                ${formatDate(ticket.tripDate)}
            </span>

            <span>
                Giờ:
                ${ticket.departureTime || "--:--"}
            </span>

            <span>
                Ghế:
                ${ticket.seatCode || "—"}
            </span>

            <span>
                Hành khách:
                ${ticket.passengerName || "—"}
            </span>

            <span>
                Giá:
                ${money(ticket.fare)}
            </span>

            <span>
                Trạng thái:
                ${statusText(ticket.status)}
            </span>

        </div>

        <div class="modal-actions">

            <button
                class="secondary-btn"
                type="button"
                data-close>
                Đóng
            </button>

            ${String(ticket.status)
            .toUpperCase() ===
            "VALID"
            ? `
                        <button
                            class="primary-btn"
                            type="button"
                            data-modal-action="change">
                            Đổi vé
                        </button>
                      `
            : ""
        }

        </div>
    `;

    bindModalButtons(
        () => openChange(ticket)
    );

    showModal();
}


// ============================================================
// CHANGE
// ============================================================

async function openChange(ticket) {

    try {

        const response =
            await fetch(
                `/api/Ticket/${ticket.ticketId}/change-options`,
                {
                    headers:
                        headers()
                }
            );

        const data =
            await response
                .json()
                .catch(
                    () => ({})
                );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể tải chuyến đổi vé."
            );
        }

        const trips =
            data.trips || [];

        if (!trips.length) {

            showToast(
                "Không có chuyến phù hợp để đổi."
            );

            return;
        }

        modalContent.innerHTML = `

            <h2>Đổi vé</h2>

            <div class="modal-summary">

                <strong>
                    ${ticket.ticketCode}
                </strong>

                <span>
                    Vé hiện tại:
                    ${formatDate(ticket.tripDate)}
                    ·
                    ${ticket.departureTime}
                    ·
                    Ghế ${ticket.seatCode}
                </span>

            </div>

            <div class="form-group">

                <label>
                    Chuyến mới
                </label>

                <select id="newTrip">

                    <option value="">
                        -- Chọn chuyến --
                    </option>

                    ${trips.map(
            trip => `
                            <option
                                value="${trip.tripId}"
                                data-fare="${trip.fare}">
                                ${trip.tripCode}
                                -
                                ${formatDate(trip.tripDate)}
                                -
                                ${trip.departureTime}
                                -
                                ${money(trip.fare)}
                            </option>
                        `
        ).join("")}

                </select>

            </div>

            <div class="form-group">

                <label>
                    Ghế mới
                </label>

                <select id="newSeat">

                    <option value="">
                        -- Chọn chuyến trước --
                    </option>

                </select>

            </div>

            <div
                id="changePrice"
                class="modal-summary">
            </div>

            <div class="modal-actions">

                <button
                    class="secondary-btn"
                    type="button"
                    data-close>
                    Hủy
                </button>

                <button
                    class="primary-btn"
                    type="button"
                    id="confirmChange">
                    Xác nhận đổi vé
                </button>

            </div>
        `;

        bindModalButtons();

        const tripSelect =
            document.getElementById("newTrip");

        const seatSelect =
            document.getElementById("newSeat");

        const priceBox =
            document.getElementById("changePrice");

        tripSelect.addEventListener(
            "change",
            async () => {

                const tripId =
                    Number(
                        tripSelect.value
                    );

                seatSelect.innerHTML =
                    `
                    <option value="">
                        Đang tải ghế...
                    </option>
                    `;

                if (!tripId)
                    return;

                const seatResponse =
                    await fetch(
                        `/api/Seat/trip/${tripId}`
                    );

                const seatData =
                    await seatResponse
                        .json()
                        .catch(
                            () => ({})
                        );

                if (!seatResponse.ok) {

                    showToast(
                        seatData.message ||
                        "Không thể tải ghế."
                    );

                    return;
                }

                seatSelect.innerHTML =
                    `
                    <option value="">
                        -- Chọn ghế --
                    </option>
                    `;

                (seatData.seats || [])
                    .filter(
                        x =>
                            x.status ===
                            "available"
                    )
                    .forEach(
                        seat => {

                            seatSelect.insertAdjacentHTML(
                                "beforeend",
                                `
                                <option value="${seat.seatCode}">
                                    ${seat.seatCode}
                                </option>
                                `
                            );
                        }
                    );

                const option =
                    tripSelect
                        .selectedOptions[0];

                const newFare =
                    Number(
                        option.dataset.fare ||
                        0
                    );

                const difference =
                    newFare -
                    Number(
                        ticket.fare || 0
                    );

                priceBox.textContent =
                    difference > 0
                        ? `Cần thanh toán thêm: ${money(difference)}`
                        : difference < 0
                            ? `Dự kiến hoàn lại: ${money(Math.abs(difference))}`
                            : "Không phát sinh chênh lệch.";
            }
        );


        document
            .getElementById("confirmChange")
            .addEventListener(
                "click",
                async () => {

                    const newTripId =
                        Number(
                            tripSelect.value
                        );

                    const newSeatCode =
                        seatSelect.value;

                    if (!newTripId) {

                        showToast(
                            "Vui lòng chọn chuyến mới."
                        );

                        return;
                    }

                    if (!newSeatCode) {

                        showToast(
                            "Vui lòng chọn ghế mới."
                        );

                        return;
                    }

                    const button =
                        document.getElementById(
                            "confirmChange"
                        );

                    button.disabled =
                        true;

                    button.textContent =
                        "Đang xử lý...";

                    try {

                        const response =
                            await fetch(
                                `/api/Ticket/${ticket.ticketId}/change`,
                                {
                                    method:
                                        "POST",

                                    headers:
                                        headers(),

                                    body:
                                        JSON.stringify({
                                            newTripId,
                                            newSeatCode
                                        })
                                }
                            );

                        const result =
                            await response
                                .json()
                                .catch(
                                    () => ({})
                                );

                        if (!response.ok) {

                            throw new Error(
                                result.message ||
                                "Đổi vé thất bại."
                            );
                        }

                        closeModal();

                        showToast(
                            "Đổi vé thành công."
                        );

                        await loadTickets();

                    } catch (error) {

                        showToast(
                            error.message
                        );

                    } finally {

                        button.disabled =
                            false;

                        button.textContent =
                            "Xác nhận đổi vé";
                    }
                }
            );

        showModal();

    } catch (error) {

        showToast(
            error.message ||
            "Không thể mở chức năng đổi vé."
        );
    }
}


// ============================================================
// CANCEL
// ============================================================

async function openCancel(ticket) {

    try {

        const response =
            await fetch(
                `/api/Ticket/${ticket.ticketId}/cancel-info`,
                {
                    headers:
                        headers()
                }
            );

        const data =
            await response
                .json()
                .catch(
                    () => ({})
                );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể kiểm tra điều kiện hủy vé."
            );
        }

        if (!data.canCancel) {

            showToast(
                data.message ||
                "Vé không thể hủy."
            );

            return;
        }

        modalContent.innerHTML = `

            <h2>Hủy vé</h2>

            <div class="modal-summary">

                <strong>
                    ${ticket.ticketCode}
                </strong>

                <span>
                    ${ticket.routeCode}
                    -
                    ${ticket.routeName}
                </span>

                <span>
                    ${formatDate(ticket.tripDate)}
                    ·
                    ${ticket.departureTime}
                    ·
                    Ghế ${ticket.seatCode}
                </span>

                <span>
                    Số tiền:
                    ${money(ticket.fare)}
                </span>

                <span>
                    Nếu đủ điều kiện,
                    hệ thống sẽ tạo giao dịch hoàn tiền.
                </span>

            </div>

            <div class="form-group">

                <label for="cancelReason">
                    Lý do hủy
                </label>

                <textarea
                    id="cancelReason"
                    placeholder="Nhập lý do nếu cần...">
                </textarea>

            </div>

            <div class="modal-actions">

                <button
                    class="secondary-btn"
                    type="button"
                    data-close>
                    Quay lại
                </button>

                <button
                    class="danger-btn"
                    type="button"
                    id="confirmCancel">
                    Xác nhận hủy vé
                </button>

            </div>
        `;

        bindModalButtons();

        document
            .getElementById("confirmCancel")
            .addEventListener(
                "click",
                async () => {

                    const button =
                        document.getElementById(
                            "confirmCancel"
                        );

                    button.disabled =
                        true;

                    button.textContent =
                        "Đang xử lý...";

                    try {

                        const response =
                            await fetch(
                                `/api/Ticket/${ticket.ticketId}/cancel`,
                                {
                                    method:
                                        "POST",

                                    headers:
                                        headers()
                                }
                            );

                        const result =
                            await response
                                .json()
                                .catch(
                                    () => ({})
                                );

                        if (!response.ok) {

                            throw new Error(
                                result.message ||
                                "Hủy vé thất bại."
                            );
                        }

                        closeModal();

                        showToast(
                            result.refundAmount > 0
                                ? `Hủy vé thành công. Hoàn tiền ${money(result.refundAmount)}.`
                                : "Hủy vé thành công."
                        );

                        await loadTickets();

                    } catch (error) {

                        showToast(
                            error.message
                        );

                    } finally {

                        button.disabled =
                            false;

                        button.textContent =
                            "Xác nhận hủy vé";
                    }
                }
            );

        showModal();

    } catch (error) {

        showToast(
            error.message ||
            "Không thể kiểm tra vé."
        );
    }
}


// ============================================================
// MODAL
// ============================================================

function bindModalButtons(
    afterClose
) {

    document
        .querySelectorAll("[data-close]")
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    closeModal
                );
            }
        );

    const modalAction =
        document.querySelector(
            "[data-modal-action]"
        );

    if (
        modalAction &&
        afterClose
    ) {

        modalAction.addEventListener(
            "click",
            afterClose
        );
    }
}


function showModal() {

    modalBackdrop.classList.remove(
        "hidden"
    );
}


function closeModal() {

    modalBackdrop.classList.add(
        "hidden"
    );
}


document
    .getElementById("modalClose")
    .addEventListener(
        "click",
        closeModal
    );


modalBackdrop.addEventListener(
    "click",
    event => {

        if (
            event.target ===
            modalBackdrop
        ) {

            closeModal();
        }
    }
);


document.addEventListener(
    "keydown",
    event => {

        if (
            event.key ===
            "Escape"
        ) {

            closeModal();
        }
    }
);


// ============================================================
// FILTER EVENTS
// ============================================================

[
    searchInput,
    statusFilter,
    dateFilter
]
    .forEach(
        control => {

            control.addEventListener(
                "input",
                renderTickets
            );

            control.addEventListener(
                "change",
                renderTickets
            );
        }
    );


// ============================================================
// INIT
// ============================================================

loadTickets();
