let bookingId = null;
let booking = null;
let currentPayment = null;

let remainingSeconds = 0;
let countdownTimer = null;

const params =
    new URLSearchParams(
        window.location.search
    );

bookingId =
    Number(
        params.get("bookingId")
    );

const money =
    value =>
        new Intl.NumberFormat(
            "vi-VN"
        ).format(
            Number(value || 0)
        ) + "đ";

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

    return String(
        session.userId ||
        session.UserId ||
        ""
    ).trim();
}

function authHeaders(
    includeJson = false
) {
    const headers = {
        "X-User-Id":
            getUserId()
    };

    if (includeJson) {
        headers["Content-Type"] =
            "application/json";
    }

    return headers;
}

function showToast(
    message,
    duration = 2500
) {
    const toast =
        document.getElementById(
            "appToast"
        );

    if (!toast) {
        void SmartbusDialog.alert(message, { title: "Thông báo" });
        return;
    }

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

async function readJson(
    response
) {
    return await response
        .json()
        .catch(
            () => ({})
        );
}


// =====================================================
// LOAD BOOKING
// =====================================================

async function loadBooking() {

    if (!bookingId ||
        bookingId <= 0) {

        showToast(
            "Không xác định được đơn đặt vé."
        );

        return;
    }

    if (!getUserId()) {

        showToast(
            "Vui lòng đăng nhập."
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
                `/api/Booking/${bookingId}`,
                {
                    headers:
                        authHeaders()
                }
            );

        const data =
            await readJson(
                response
            );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể tải đơn đặt vé."
            );
        }

        booking = data;

        // Đồng bộ thời gian backend trả về
        remainingSeconds =
            Number(
                booking.remainingSeconds ?? 0
            );

        renderBooking();
        startCountdown();

    } catch (error) {

        console.error(error);

        showToast(
            error.message ||
            "Không thể tải đơn đặt vé."
        );
    }
}


// =====================================================
// RENDER BOOKING
// =====================================================

function renderBooking() {

    if (!booking) {
        return;
    }

    // ================================================
    // ĐIỂM ĐI
    // ================================================

    const origin =
        document.getElementById(
            "originName"
        );

    if (origin) {
        origin.textContent =
            booking.startPoint ||
            "—";
    }

    const originAddress =
        document.getElementById(
            "originAddress"
        );

    if (originAddress) {
        originAddress.textContent =
            booking.startPoint ||
            "—";
    }

    // ================================================
    // ĐIỂM ĐẾN
    // ================================================

    const destination =
        document.getElementById(
            "destinationName"
        );

    if (destination) {
        destination.textContent =
            booking.endPoint ||
            "—";
    }

    const destinationAddress =
        document.getElementById(
            "destinationAddress"
        );

    if (destinationAddress) {
        destinationAddress.textContent =
            booking.endPoint ||
            "—";
    }

    // ================================================
    // NGÀY
    // ================================================

    const tripDate =
        document.getElementById(
            "tripDate"
        );

    if (tripDate) {
        tripDate.textContent =
            formatDate(
                booking.tripDate
            );
    }

    // ================================================
    // GIỜ KHỞI HÀNH
    // ================================================

    const departureTime =
        document.getElementById(
            "departureTime"
        );

    if (departureTime) {
        departureTime.textContent =
            booking.departureTime ||
            "—";
    }

    // ================================================
    // GHẾ
    // ================================================

    const seatText =
        document.getElementById(
            "seatText"
        );

    if (seatText) {

        const seats =
            Array.isArray(
                booking.seatCodes
            )
                ? booking.seatCodes
                : [];

        seatText.textContent =
            seats.length > 0
                ? seats.join(", ")
                : "—";
    }

    // ================================================
    // GIÁ VÉ
    // ================================================

    const priceText =
        document.getElementById(
            "priceText"
        );

    if (priceText) {
        priceText.textContent =
            money(
                booking.totalAmount
            );
    }

    // ================================================
    // TỔNG TIỀN
    // ================================================

    const totalText =
        document.getElementById(
            "totalText"
        );

    if (totalText) {
        totalText.textContent =
            money(
                booking.totalAmount
            );
    }

    // ================================================
    // SỐ TIỀN TRÊN NÚT
    // ================================================

    const payAmount =
        document.getElementById(
            "payAmount"
        );

    if (payAmount) {
        payAmount.textContent =
            money(
                booking.totalAmount
            );
    }

    // ================================================
    // HẾT HẠN
    // ================================================

    if (
        booking.status &&
        booking.status !== "HELD"
    ) {

        const button =
            document.getElementById(
                "payBtn"
            );

        if (button) {

            button.disabled =
                true;

            button.textContent =
                booking.status ===
                    "EXPIRED"
                    ? "Hết thời gian giữ chỗ"
                    : "Đơn đặt vé không còn hiệu lực";
        }
    }

    console.log(
        "Booking đồng bộ:",
        booking
    );
}

// =====================================================
// DATE
// =====================================================

function formatDate(date) {

    if (!date) {
        return "—";
    }

    const value =
        String(date)
            .substring(
                0,
                10
            );

    const parts =
        value.split("-");

    if (parts.length !== 3) {
        return "—";
    }

    return `${parts[2]}/${parts[1]}/${parts[0]}`;
}


// =====================================================
// COUNTDOWN
// =====================================================

function startCountdown() {

    clearInterval(countdownTimer);

    // Backend đã tính sẵn thời gian còn lại.
    // Không tính lại bằng expiresAt để tránh lệch timezone.
    remainingSeconds = Number(
        booking?.remainingSeconds ?? 0
    );

    const countdown =
        document.getElementById("countdown");

    const payBtn =
        document.getElementById("payBtn");

    function updateCountdown() {

        if (!countdown) {
            return;
        }

        const safeSeconds =
            Math.max(
                0,
                Number(remainingSeconds || 0)
            );

        const minutes =
            String(
                Math.floor(
                    safeSeconds / 60
                )
            ).padStart(2, "0");

        const seconds =
            String(
                safeSeconds % 60
            ).padStart(2, "0");

        countdown.textContent =
            `${minutes}:${seconds}`;

        if (safeSeconds <= 0) {

            clearInterval(
                countdownTimer
            );

            countdown.textContent =
                "00:00";

            if (payBtn) {

                payBtn.disabled = true;

                payBtn.textContent =
                    "Hết thời gian giữ chỗ";
            }

            return;
        }

        remainingSeconds =
            safeSeconds - 1;
    }

    updateCountdown();

    countdownTimer =
        setInterval(
            updateCountdown,
            1000
        );
}


// =====================================================
// PAYMENT METHOD
// =====================================================

const notes = {

    VNPay:
        "Thanh toán mô phỏng qua VNPay.",

    MoMo:
        "Thanh toán mô phỏng qua MoMo.",

    ZaloPay:
        "Thanh toán mô phỏng qua ZaloPay.",

    Bank:
        "Thanh toán mô phỏng qua thẻ ngân hàng.",

    Cash:
        "Thanh toán tiền mặt. Hệ thống sẽ xác nhận và phát hành vé."
};

document
    .querySelectorAll(
        ".method"
    )
    .forEach(
        method => {

            method.addEventListener(
                "click",
                () => {

                    document
                        .querySelectorAll(
                            ".method"
                        )
                        .forEach(
                            item => {
                                item.classList.remove(
                                    "active"
                                );
                            }
                        );

                    method.classList.add(
                        "active"
                    );

                    const input =
                        method.querySelector(
                            "input"
                        );

                    input.checked =
                        true;

                    document
                        .querySelector(
                            "#methodNote p"
                        )
                        .textContent =
                        notes[
                        input.value
                        ];

                    const payButton =
                        document.getElementById(
                            "payBtn"
                        );

                    if (payButton) {

                        if (input.value === "Cash") {

                            payButton.innerHTML =
                                'Xác nhận thanh toán tiền mặt <span>→</span>';

                        } else {

                            payButton.innerHTML =
                                'Thanh toán <span>→</span>';
                        }
                    }
                }
            );
        }
    );


// =====================================================
// CREATE PAYMENT
// =====================================================

async function createPayment() {

    const selectedMethod =
        document.querySelector(
            "input[name='paymentMethod']:checked"
        );

    if (!selectedMethod) {

        throw new Error(
            "Vui lòng chọn phương thức thanh toán."
        );
    }

    const response =
        await fetch(
            "/api/Payment/create",
            {
                method:
                    "POST",

                headers:
                    authHeaders(true),

                body:
                    JSON.stringify({
                        bookingId:
                            bookingId,

                        provider:
                            selectedMethod.value
                    })
            }
        );

    const data =
        await readJson(
            response
        );

    if (!response.ok) {

        throw new Error(
            data.message ||
            "Không thể tạo giao dịch."
        );
    }

    return data;
}


// =====================================================
// CONFIRM PAYMENT
// =====================================================

async function confirmPayment(
    paymentId
) {

    const response =
        await fetch(
            `/api/Payment/${paymentId}/confirm`,
            {
                method:
                    "POST",

                headers:
                    authHeaders()
            }
        );

    const data =
        await readJson(
            response
        );

    if (!response.ok) {

        throw new Error(
            data.message ||
            "Thanh toán không thành công."
        );
    }

    return data;
}


// =====================================================
// PAY BUTTON
// =====================================================

const payButton =
    document.getElementById(
        "payBtn"
    );

if (payButton) {

    payButton.addEventListener(
        "click",
        async () => {

            if (
                !booking ||
                booking.status !==
                "HELD"
            ) {

                showToast(
                    "Đơn đặt vé không còn hiệu lực."
                );

                return;
            }

            if (
                remainingSeconds <= 0
            ) {

                showToast(
                    "Thời gian giữ chỗ đã hết."
                );

                return;
            }

            payButton.disabled =
                true;

            payButton.innerHTML =
                "Đang xử lý...";

            try {

                // ====================================
                // BƯỚC 1: TẠO GIAO DỊCH
                // ====================================

                const payment =
                    await createPayment();

                currentPayment =
                    payment;

                sessionStorage.setItem(
                    "currentPayment",
                    JSON.stringify(
                        payment
                    )
                );

                // ====================================
                // BƯỚC 2:
                // MÔ PHỎNG XÁC NHẬN CỔNG THANH TOÁN
                // ====================================

                await new Promise(
                    resolve =>
                        setTimeout(
                            resolve,
                            800
                        )
                );

                // ====================================
                // BƯỚC 3: CONFIRM
                // ====================================

                const result =
                    await confirmPayment(
                        payment.paymentId
                    );

                const status =
                    String(
                        result.status ||
                        result.Status ||
                        ""
                    )
                        .trim()
                        .toUpperCase();

                if (
                    status !==
                    "SUCCESS"
                ) {

                    throw new Error(
                        "Giao dịch chưa được xác nhận thành công."
                    );
                }

                sessionStorage.setItem(
                    "confirmedPayment",
                    JSON.stringify(
                        result
                    )
                );

                // ====================================
                // BƯỚC 4: CHUYỂN TRANG KẾT QUẢ
                // ====================================

                const resultParams =
                    new URLSearchParams({
                        bookingId:
                            String(
                                bookingId
                            ),

                        paymentId:
                            String(
                                payment.paymentId
                            )
                    });

                await showPaymentComplete(
                    bookingId
                );

            } catch (error) {

                console.error(error);

                showToast(
                    "❌ " +
                    (
                        error.message ||
                        "Thanh toán thất bại."
                    )
                );

                payButton.disabled =
                    false;

                payButton.innerHTML =
                    `Thanh toán <strong id="payAmount">${money(
                        booking?.totalAmount || 0
                    )}</strong><span>→</span>`;
            }
        }
    );
}

async function showPaymentComplete(
    bookingId
) {
    try {

        // =========================================
        // LẤY VÉ VỪA TẠO
        // =========================================

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
                "Thanh toán thành công nhưng chưa lấy được vé."
            );
        }

        const tickets =
            Array.isArray(data)
                ? data
                : [];

        if (tickets.length === 0) {

            throw new Error(
                "Thanh toán thành công nhưng chưa tạo được vé."
            );
        }

        const ticket =
            tickets[0];

        // =========================================
        // ẨN PHẦN THANH TOÁN
        // =========================================

        const paymentLayout =
            document.querySelector(
                ".payment-layout"
            );

        if (paymentLayout) {

            paymentLayout.style.display =
                "none";
        }

        // =========================================
        // ĐỔI STEP
        // =========================================

        const stepPayment =
            document.getElementById(
                "stepPayment"
            );

        const stepComplete =
            document.getElementById(
                "stepComplete"
            );

        const lineComplete =
            document.getElementById(
                "lineComplete"
            );

        if (stepPayment) {

            stepPayment.classList.remove(
                "current"
            );

            stepPayment.classList.add(
                "done"
            );

            stepPayment
                .querySelector("span")
                .textContent =
                "✓";
        }

        if (lineComplete) {

            lineComplete.classList.add(
                "active"
            );
        }

        if (stepComplete) {

            stepComplete.classList.add(
                "current"
            );
        }

        // =========================================
        // HIỆN HOÀN TẤT
        // =========================================

        const completionPanel =
            document.getElementById(
                "completionPanel"
            );

        if (completionPanel) {

            completionPanel.style.display =
                "block";
        }

        // =========================================
        // THÔNG TIN VÉ
        // =========================================

        const ticketCode =
            document.getElementById(
                "completeTicketCode"
            );

        if (ticketCode) {

            ticketCode.textContent =
                ticket.ticketCode ||
                "—";
        }

        const completeRoute =
            document.getElementById(
                "completeRoute"
            );

        if (completeRoute) {

            completeRoute.textContent =
                ticket.routeName ||
                ticket.routeCode ||
                "—";
        }

        const completeDate =
            document.getElementById(
                "completeDate"
            );

        if (completeDate) {

            completeDate.textContent =
                formatDate(
                    ticket.tripDate
                );
        }

        const completeSeat =
            document.getElementById(
                "completeSeat"
            );

        if (completeSeat) {

            completeSeat.textContent =
                ticket.seatCode ||
                "—";
        }

        // =========================================
        // NÚT XEM VÉ
        // =========================================

        const viewTicketBtn =
            document.getElementById(
                "viewTicketBtn"
            );

        if (viewTicketBtn) {

            viewTicketBtn.onclick =
                () => {

                    window.location.href =
                        `../ve/index.html?bookingId=${bookingId}`;
                };
        }

        // =========================================
        // LƯU VÀO SESSION
        // =========================================

        sessionStorage.setItem(
            "confirmedTicket",
            JSON.stringify(
                ticket
            )
        );

    } catch (error) {

        console.error(
            "Complete payment error:",
            error
        );

        showToast(
            "⚠️ " +
            (
                error.message ||
                "Không thể hiển thị vé."
            )
        );
    }
}
// =====================================================
// START
// =====================================================

loadBooking();
