let bookingId = null;

let tickets = [];
let currentTicketIndex = 0;

const params =
    new URLSearchParams(
        window.location.search
    );

bookingId =
    Number(
        params.get("bookingId")
    );


// =====================================================
// SESSION
// =====================================================

function getSession() {

    try {

        return JSON.parse(
            localStorage.getItem(
                "userSession"
            ) || "{}"
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
function showToast(
    message,
    duration = 2200
) {

    const toast =
        document.getElementById(
            "appToast"
        );

    if (!toast)
        return;

    toast.textContent =
        message;

    toast.classList.add(
        "show"
    );

    clearTimeout(
        window.appToastTimer
    );

    window.appToastTimer =
        setTimeout(
            () => {

                toast.classList.remove(
                    "show"
                );

            },
            duration
        );
}

// =====================================================
// HEADER
// =====================================================

function authHeaders() {

    return {
        "X-User-Id":
            getUserId()
    };
}


// =====================================================
// MONEY
// =====================================================

function formatMoney(value) {

    return new Intl.NumberFormat(
        "vi-VN"
    ).format(
        Number(value || 0)
    ) + "đ";
}


// =====================================================
// DATE
// =====================================================

function formatDate(value) {

    if (!value)
        return "—";

    const date =
        new Date(value);

    if (Number.isNaN(
        date.getTime()
    )) {
        return value;
    }

    return date.toLocaleDateString(
        "vi-VN"
    );
}


// =====================================================
// LOAD TICKETS
// =====================================================

async function loadTickets() {

    if (!bookingId || bookingId <= 0) {

        showError(
            "Không xác định được đơn đặt vé."
        );

        return;
    }


    const userId =
        getUserId();


    if (!userId) {

        showToast(
            "⚠️ Vui lòng đăng nhập."
        );

        setTimeout(
            () => {

                window.location.href =
                    "../auth/dangnhap.html";

            },
            700
        );

        return;
    }


    try {

        const response =
            await fetch(
                `/api/Ticket/booking/${bookingId}`,
                {
                    headers:
                        authHeaders()
                }
            );


        const data =
            await response.json()
                .catch(
                    () => ({})
                );


        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể tải vé điện tử."
            );
        }


        tickets =
            Array.isArray(data)
                ? data
                : [];


        if (tickets.length === 0) {

            throw new Error(
                "Booking chưa có vé điện tử."
            );
        }


        currentTicketIndex =
            0;


        createTicketSelector();

        renderTicket(
            currentTicketIndex
        );

    } catch (error) {

        console.error(error);

        showError(
            error.message ||
            "Không thể tải vé điện tử."
        );
    }
}


// =====================================================
// SELECTOR NẾU BOOKING CÓ NHIỀU VÉ
// =====================================================

function createTicketSelector() {

    const existing =
        document.getElementById(
            "ticketSelector"
        );

    if (!existing)
        return;


    if (tickets.length <= 1) {

        existing.innerHTML =
            "";

        existing.style.display =
            "none";

        return;
    }


    existing.style.display =
        "block";


    existing.innerHTML =
        `
        <label for="ticketSelect">
            Chọn vé / ghế
        </label>

        <select id="ticketSelect">

            ${tickets.map(
            (ticket, index) =>
                `
                    <option value="${index}">
                        Ghế ${escapeHtml(
                    ticket.seatCode
                )} - ${escapeHtml(
                    ticket.ticketCode
                )
                }
                    </option>
                    `
        ).join("")}

        </select>
        `;


    document
        .getElementById(
            "ticketSelect"
        )
        .addEventListener(
            "change",
            event => {

                currentTicketIndex =
                    Number(
                        event.target.value
                    );

                renderTicket(
                    currentTicketIndex
                );
            }
        );
}


// =====================================================
// RENDER TICKET
// =====================================================

function renderTicket(index) {

    const ticket =
        tickets[index];


    if (!ticket)
        return;


    // =================================================
    // MÃ VÉ
    // =================================================

    document.getElementById(
        "ticketCode"
    ).textContent =
        ticket.ticketCode || "—";


    document.getElementById(
        "qrText"
    ).textContent =
        ticket.ticketCode || "—";


    // =================================================
    // TUYẾN
    // =================================================

    const routeName =
        document.querySelector(
            ".route-summary .route-station strong"
        );


    if (routeName) {

        routeName.textContent =
            ticket.departureStop || "—";
    }


    const stations =
        document.querySelectorAll(
            ".route-summary .route-station"
        );


    if (stations.length >= 2) {

        const first =
            stations[0];

        const last =
            stations[1];


        const firstStrong =
            first.querySelector(
                "strong"
            );

        const lastStrong =
            last.querySelector(
                "strong"
            );


        const firstSmall =
            first.querySelector(
                "small"
            );

        const lastSmall =
            last.querySelector(
                "small"
            );


        const firstTime =
            first.querySelector(
                ".time"
            );

        const lastTime =
            last.querySelector(
                ".time"
            );


        if (firstStrong) {

            firstStrong.textContent =
                ticket.departureStop ||
                "—";
        }


        if (lastStrong) {

            lastStrong.textContent =
                ticket.arrivalStop ||
                "—";
        }


        if (firstSmall) {

            firstSmall.textContent =
                "Điểm đi";
        }


        if (lastSmall) {

            lastSmall.textContent =
                "Điểm đến";
        }


        if (firstTime) {

            firstTime.textContent =
                ticket.departureTime ||
                "—";
        }


        if (lastTime) {

            lastTime.textContent =
                ticket.arrivalTime ||
                "—";
        }
    }


    // =================================================
    // NGÀY
    // =================================================

    const infoItems =
        document.querySelectorAll(
            ".ticket-info-grid .info-item"
        );


    if (infoItems.length >= 6) {

        infoItems[0]
            .querySelector("strong")
            .textContent =
            formatDate(
                ticket.tripDate
            );


        infoItems[1]
            .querySelector("strong")
            .textContent =
            ticket.routeCode ||
            "—";


        infoItems[2]
            .querySelector("strong")
            .textContent =
            ticket.seatCode ||
            "—";


        infoItems[4]
            .querySelector("strong")
            .textContent =
            ticket.passengerName ||
            "—";


        infoItems[5]
            .querySelector("strong")
            .textContent =
            "—";
    }


    // =================================================
    // TIỀN
    // =================================================

    const paymentStrong =
        document.querySelector(
            ".ticket-payment > strong"
        );


    if (paymentStrong) {

        paymentStrong.textContent =
            formatMoney(
                ticket.fare
            );
    }


    const paymentLabel =
        document.querySelector(
            ".ticket-payment span"
        );


    if (paymentLabel) {

        paymentLabel.innerHTML =
            "Giá vé";
    }


    // =================================================
    // STATUS
    // =================================================

    const statusBadge =
        document.querySelector(
            ".status-badge"
        );

    if (statusBadge) {

        statusBadge.className =
            "status-badge";

        let text =
            "Có hiệu lực";

        if (
            ticket.status ===
            "USED"
        ) {

            text =
                "Đã sử dụng";

            statusBadge.classList.add(
                "used"
            );

        }
        else if (
            ticket.status ===
            "CANCELLED"
        ) {

            text =
                "Đã hủy";

            statusBadge.classList.add(
                "cancelled"
            );
        }

        statusBadge.innerHTML =
            `<span></span> ${text}`;
    }

    // =================================================
    // TRẠNG THÁI NÚT HỦY
    // =================================================

    const cancelBtn =
        document.getElementById(
            "cancelBtn"
        );
    const changeBtn =
        document.getElementById(
            "changeBtn"
        );

    if (changeBtn) {

        changeBtn.disabled =
            ticket.status !== "VALID";

        if (ticket.status === "CANCELLED") {

            changeBtn.textContent =
                "Vé đã hủy";

        } else if (
            ticket.status === "USED"
        ) {

            changeBtn.textContent =
                "Vé đã sử dụng";

        } else {

            changeBtn.textContent =
                "Đổi vé";
        }
    }

    if (cancelBtn) {

        const canCancel =
            ticket.status === "VALID";

        cancelBtn.disabled =
            !canCancel;

        if (ticket.status === "CANCELLED") {

            cancelBtn.textContent =
                "Vé đã hủy";

        }
        else if (
            ticket.status === "USED"
        ) {

            cancelBtn.textContent =
                "Vé đã sử dụng";

        }
        else {

            cancelBtn.textContent =
                "Yêu cầu hủy vé";
        }
    }

    // =================================================
    // QR
    // =================================================

    renderQR(
        ticket.qrToken
    );
    updateTicketStatusBadge(ticket);
    updateCancelButton();
}
// ============================================================
// VỊ TRÍ:
// wwwroot/ve/script.js
// ============================================================

function updateTicketStatusBadge(ticket) {

    const badge =
        document.getElementById(
            "ticketStatusBadge"
        );

    if (!badge || !ticket)
        return;

    const status =
        String(
            ticket.status || ""
        ).toUpperCase();

    if (status === "VALID") {

        badge.innerHTML =
            "<span></span> Còn hiệu lực";

    } else if (status === "USED") {

        badge.innerHTML =
            "<span></span> Đã sử dụng";

    } else if (
        status === "CANCELLED"
    ) {

        badge.innerHTML =
            "<span></span> Đã hủy";

    } else {

        badge.innerHTML =
            "<span></span> Không xác định";
    }
}

// =====================================================
// RENDER QR
// =====================================================

function renderQR(
    qrToken
) {

    const qr =
        document.getElementById(
            "qrcode"
        );


    if (!qr)
        return;


    qr.innerHTML =
        "";


    if (!qrToken) {

        qr.innerHTML =
            `
            <div class="qr-error">
                Không có mã QR.
            </div>
            `;

        return;
    }


    if (
        typeof QRCode ===
        "undefined"
    ) {

        qr.innerHTML =
            `
            <div class="qr-error">
                Không tải được thư viện QR.
            </div>
            `;

        return;
    }


    new QRCode(
        qr,
        {
            text:
                qrToken,

            width:
                186,

            height:
                186,

            colorDark:
                "#172033",

            colorLight:
                "#ffffff",

            correctLevel:
                QRCode.CorrectLevel.M
        }
    );
}


// =====================================================
// COPY TICKET CODE
// =====================================================

document
    .getElementById(
        "copyBtn"
    )
    ?.addEventListener(
        "click",
        async event => {

            const ticket =
                tickets[
                currentTicketIndex
                ];


            if (!ticket)
                return;


            try {

                await navigator.clipboard
                    .writeText(
                        ticket.ticketCode
                    );


                event.currentTarget
                    .textContent =
                    "✓ Đã sao chép";


                setTimeout(
                    () => {

                        event.currentTarget
                            .textContent =
                            "Sao chép mã vé";

                    },
                    1800
                );

            } catch {

                showToast(
                    `Mã vé: ${ticket.ticketCode}`
                );
            }
        }
    );


// =====================================================
// PRINT
// =====================================================

document
    .getElementById(
        "printBtn"
    )
    ?.addEventListener(
        "click",
        () => {

            window.print();
        }
    );


// =====================================================
// UPDATE CANCEL BUTTON
// =====================================================

function updateCancelButton() {

    const cancelBtn =
        document.getElementById(
            "cancelBtn"
        );

    const ticket =
        tickets[
        currentTicketIndex
        ];

    if (!cancelBtn ||
        !ticket) {
        return;
    }

    if (ticket.status === "CANCELLED") {

        cancelBtn.disabled =
            true;

        cancelBtn.textContent =
            "Vé đã hủy";

        return;
    }

    if (ticket.status === "USED") {

        cancelBtn.disabled =
            true;

        cancelBtn.textContent =
            "Vé đã sử dụng";

        return;
    }

    cancelBtn.disabled =
        false;

    cancelBtn.textContent =
        "Yêu cầu hủy vé";
}


// =====================================================
// CANCEL TICKET
// =====================================================

document
    .getElementById(
        "cancelBtn"
    )
    ?.addEventListener(
        "click",
        async () => {

            const ticket =
                tickets[
                currentTicketIndex
                ];

            if (!ticket) {

                showToast(
                    "⚠️ Không xác định được vé."
                );

                return;
            }

            if (ticket.status !== "VALID") {

                showToast(
                    ticket.status === "USED"
                        ? "⚠️ Vé đã được sử dụng."
                        : "⚠️ Vé không thể hủy."
                );
                updateCancelButton();

                return;
            }

            try {

                // =================================================
                // KIỂM TRA ĐIỀU KIỆN HỦY
                // =================================================

                const infoResponse =
                    await fetch(
                        `/api/Ticket/${ticket.ticketId}/cancel-info`,
                        {
                            method:
                                "GET",

                            headers:
                                authHeaders()
                        }
                    );

                const info =
                    await infoResponse
                        .json()
                        .catch(
                            () => ({})
                        );

                if (!infoResponse.ok ||
                    !info.canCancel) {

                    showToast(
                        "⚠️ " +
                        (
                            info.message ||
                            "Vé không đủ điều kiện hủy."
                        )
                    );

                    return;
                }

                // =================================================
                // XÁC NHẬN
                // =================================================

                const confirmed = await SmartbusDialog.confirm(
                    `Mã vé: ${ticket.ticketCode}\n` +
                    `Ghế: ${ticket.seatCode}\n` +
                    `Điểm đi: ${ticket.departureStop}\n` +
                    `Điểm đến: ${ticket.arrivalStop}\n\n` +
                    `Vé phải được hủy trước giờ khởi hành ít nhất ${info.cutoffMinutes} phút.`,
                    { title: "Xác nhận hủy vé", confirmText: "Hủy vé", danger: true }
                );

                if (!confirmed) {
                    return;
                }

                const cancelBtn =
                    document.getElementById(
                        "cancelBtn"
                    );

                if (cancelBtn) {

                    cancelBtn.disabled =
                        true;

                    cancelBtn.textContent =
                        "Đang hủy...";
                }

                // =================================================
                // GỌI API
                // =================================================

                const response =
                    await fetch(
                        `/api/Ticket/${ticket.ticketId}/cancel`,
                        {
                            method:
                                "POST",

                            headers:
                                authHeaders()
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
                        "Không thể hủy vé."
                    );
                }

                // =================================================
                // XỬ LÝ HOÀN TIỀN MÔ PHỎNG
                // =================================================

                let refundResult = null;

                if (data.refundId) {

                    refundResult =
                        await processRefund(
                            data.refundId,
                            true
                        );
                }

                // =================================================
                // THÔNG BÁO KẾT QUẢ
                // =================================================

                if (
                    refundResult &&
                    refundResult.status === "SUCCESS"
                ) {

                    showToast(
                        `✓ Hủy vé và hoàn tiền mô phỏng ${formatMoney(refundResult.amount)} thành công.`
                    );

                } else if (data.refundId) {

                    showToast(
                        "✓ Hủy vé thành công. Khoản hoàn tiền đang chờ xử lý."
                    );

                } else {

                    showToast(
                        "✓ Hủy vé thành công."
                    );
                }

                // =================================================
                // TẢI LẠI DỮ LIỆU TỪ BACKEND
                // =================================================

                await loadTickets();

            }
            catch (error) {

                console.error(
                    "Cancel ticket error:",
                    error
                );

                showToast(
                    "❌ " +
                    (
                        error.message ||
                        "Không thể hủy vé."
                    )
                );

            }
            finally {

                updateCancelButton();
            }
        }
);

// =====================================================
// ĐỔI VÉ
// =====================================================

const changeBtn =
    document.getElementById(
        "changeBtn"
    );

const changeModal =
    document.getElementById(
        "changeModal"
    );

const closeChangeModal =
    document.getElementById(
        "closeChangeModal"
    );

const cancelChangeBtn =
    document.getElementById(
        "cancelChangeBtn"
    );

const changeTripSelect =
    document.getElementById(
        "changeTripSelect"
    );

const changeSeatSelect =
    document.getElementById(
        "changeSeatSelect"
    );

const changePriceInfo =
    document.getElementById(
        "changePriceInfo"
    );

const submitChangeBtn =
    document.getElementById(
        "submitChangeBtn"
    );


// =====================================================
// ĐÓNG MODAL
// =====================================================

function closeChangeModalWindow() {

    if (changeModal) {

        changeModal.style.display =
            "none";
    }
}


closeChangeModal?.addEventListener(
    "click",
    closeChangeModalWindow
);


cancelChangeBtn?.addEventListener(
    "click",
    closeChangeModalWindow
);


// =====================================================
// LOAD CHUYẾN CÓ THỂ ĐỔI
// =====================================================

async function openChangeModal() {

    const ticket =
        tickets[
        currentTicketIndex
        ];

    if (!ticket) {

        showToast(
            "Không xác định được vé."
        );

        return;
    }

    if (ticket.status !== "VALID") {

        showToast(
            "Chỉ vé còn hiệu lực mới được đổi."
        );

        return;
    }

    try {

        const response =
            await fetch(
                `/api/Ticket/${ticket.ticketId}/change-options`,
                {
                    headers:
                        authHeaders()
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
                "Không thể tải chuyến có thể đổi."
            );
        }

        changeTripSelect.innerHTML =
            `
            <option value="">
                -- Chọn chuyến --
            </option>
            `;

        changeSeatSelect.innerHTML =
            `
            <option value="">
                -- Chọn ghế --
            </option>
            `;

        if (!data.trips ||
            data.trips.length === 0) {

            showToast(
                "Không có chuyến phù hợp để đổi."
            );

            return;
        }

        data.trips.forEach(
            trip => {

                const option =
                    document.createElement(
                        "option"
                    );

                option.value =
                    trip.tripId;

                option.textContent =
                    `${trip.tripCode} - ` +
                    `${formatDate(trip.tripDate)} ` +
                    `${trip.departureTime} - ` +
                    `${formatMoney(trip.fare)}`;

                option.dataset.fare =
                    trip.fare;

                changeTripSelect.appendChild(
                    option
                );
            }
        );

        changePriceInfo.textContent =
            `Giá vé hiện tại: ${formatMoney(data.oldFare)}`;

        changeModal.style.display =
            "flex";

    } catch (error) {

        console.error(error);

        showToast(
            error.message ||
            "Không thể mở chức năng đổi vé."
        );
    }
}


changeBtn?.addEventListener(
    "click",
    openChangeModal
);


// =====================================================
// CHỌN CHUYẾN -> LOAD GHẾ
// =====================================================

changeTripSelect?.addEventListener(
    "change",
    async event => {

        const tripId =
            Number(
                event.target.value
            );

        changeSeatSelect.innerHTML =
            `
            <option value="">
                -- Chọn ghế --
            </option>
            `;

        if (!tripId)
            return;

        try {

            const response =
                await fetch(
                    `/api/Seat/trip/${tripId}`
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
                    "Không thể tải ghế."
                );
            }

            (data.seats || [])
                .filter(
                    seat =>
                        seat.status ===
                        "available"
                )
                .forEach(
                    seat => {

                        const option =
                            document.createElement(
                                "option"
                            );

                        option.value =
                            seat.seatCode;

                        option.textContent =
                            `${seat.seatCode}`;

                        changeSeatSelect
                            .appendChild(
                                option
                            );
                    }
                );

            const selected =
                event.target
                    .selectedOptions[0];

            const fare =
                Number(
                    selected?.dataset
                        ?.fare || 0
                );

            const currentTicket =
                tickets[
                currentTicketIndex
                ];

            if (currentTicket) {

                const difference =
                    fare -
                    Number(
                        currentTicket.fare ||
                        0
                    );

                if (difference > 0) {

                    changePriceInfo.textContent =
                        `Giá mới cao hơn ${formatMoney(difference)}. Demo sẽ ghi nhận khoản thanh toán chênh lệch.`;

                } else if (
                    difference < 0
                ) {

                    changePriceInfo.textContent =
                        `Giá mới thấp hơn ${formatMoney(Math.abs(difference))}. Phần chênh lệch sẽ được hoàn tiền.`;

                } else {

                    changePriceInfo.textContent =
                        "Giá vé không thay đổi.";
                }
            }

        } catch (error) {

            console.error(error);

            showToast(
                error.message ||
                "Không thể tải danh sách ghế."
            );
        }
    }
);


// =====================================================
// XÁC NHẬN ĐỔI VÉ
// =====================================================

submitChangeBtn?.addEventListener(
    "click",
    async () => {

        const ticket =
            tickets[
            currentTicketIndex
            ];

        const newTripId =
            Number(
                changeTripSelect.value
            );

        const newSeatCode =
            changeSeatSelect.value;

        if (!ticket) {

            showToast(
                "Không xác định được vé."
            );

            return;
        }

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

        try {

            submitChangeBtn.disabled =
                true;

            submitChangeBtn.textContent =
                "Đang xử lý...";

            const response =
                await fetch(
                    `/api/Ticket/${ticket.ticketId}/change`,
                    {
                        method:
                            "POST",

                        headers:
                        {
                            ...authHeaders(),

                            "Content-Type":
                                "application/json"
                        },

                        body:
                            JSON.stringify({
                                newTripId,

                                newSeatCode
                            })
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
                    "Không thể đổi vé."
                );
            }

            closeChangeModalWindow();

            if (data.refundId) {

                await processRefund(
                    data.refundId,
                    true
                );
            }

            if (
                Number(data.difference || 0) > 0
            ) {

                showToast(
                    `✓ Đổi vé thành công. Đã thanh toán mô phỏng phần chênh lệch ${formatMoney(data.difference)}.`
                );

            } else if (
                Number(data.difference || 0) < 0
            ) {

                showToast(
                    `✓ Đổi vé thành công. Đã tạo hoàn tiền mô phỏng ${formatMoney(Math.abs(data.difference))}.`
                );

            } else {

                showToast(
                    "✓ Đổi vé thành công. Không có chênh lệch giá."
                );
            }

            showToast(
                "✓ Đổi vé thành công."
            );

            await loadTickets();

        } catch (error) {

            console.error(error);

            showToast(
                error.message ||
                "Không thể đổi vé."
            );

        } finally {

            submitChangeBtn.disabled =
                false;

            submitChangeBtn.textContent =
                "Xác nhận đổi vé";
        }
    }
);


// =====================================================
// HOÀN TIỀN MÔ PHỎNG
// =====================================================

async function processRefund(
    refundId,
    silent = false
) {

    if (!refundId)
        return null;

    try {

        const response =
            await fetch(
                `/api/Refund/${refundId}/process`,
                {
                    method:
                        "POST",

                    headers:
                        authHeaders()
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
                "Không thể xử lý hoàn tiền."
            );
        }

        if (!silent) {

            showToast(
                `✓ Hoàn tiền mô phỏng ${formatMoney(data.amount)} thành công.`
            );
        }

        return data;

    } catch (error) {

        console.error(
            "Refund error:",
            error
        );

        if (!silent) {

            showToast(
                error.message ||
                "Khoản hoàn tiền đang chờ xử lý."
            );
        }

        return null;
    }
}

// =====================================================
// ERROR
// =====================================================

function showError(message) {

    const card =
        document.getElementById(
            "ticketCard"
        );


    if (card) {

        card.innerHTML =
            `
            <div style="
                padding:40px;
                text-align:center;
                color:#d9534f;
            ">
                ${escapeHtml(
                message
            )}
            </div>
            `;
    }


    const qr =
        document.getElementById(
            "qrcode"
        );


    if (qr) {

        qr.innerHTML =
            "";
    }
}


// =====================================================
// ESCAPE HTML
// =====================================================

function escapeHtml(value) {

    return String(
        value ?? ""
    ).replace(
        /[&<>'"]/g,
        c => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            "'": "&#39;",
            '"': "&quot;"
        }[c])
    );
}


// =====================================================
// START
// =====================================================

loadTickets();
