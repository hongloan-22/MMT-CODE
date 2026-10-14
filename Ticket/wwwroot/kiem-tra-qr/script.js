let selectedTripId = null;
let verifiedTicket = null;

const tripSelect =
    document.getElementById(
        "tripSelect"
    );

const qrToken =
    document.getElementById(
        "qrToken"
    );

const verifyBtn =
    document.getElementById(
        "verifyBtn"
    );

const useBtn =
    document.getElementById(
        "useBtn"
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

function headers() {

    return {
        "Content-Type":
            "application/json",

        "X-User-Id":
            getUserId()
    };
}


// =====================================================
// CHECK LOGIN
// =====================================================

function checkLogin() {

    const session =
        getSession();

    const userId =
        session.userId ||
        session.UserId ||
        "";

    if (!userId) {

        showToast(
            "⚠️ Vui lòng đăng nhập trước."
        );

        setTimeout(
            () => {

                window.location.href =
                    "../auth/dangnhap.html";

            },
            700
        );

        return false;
    }

    const role =
        String(
            session.role ||
            session.roleCode ||
            session.Role ||
            session.RoleCode ||
            ""
        )
            .trim()
            .toUpperCase();

    const allowed =
        role === "ADMIN" ||
        role === "MANAGER" ||
        role === "DRIVER";

    if (!allowed) {

        showToast(
            "❌ Bạn không có quyền truy cập khu vực kiểm tra vé QR."
        );

        setTimeout(
            () => {

                window.location.href =
                    "../index.html";

            },
            700
        );

        return false;
    }

    return true;
}


// =====================================================
// LOAD TRIPS
// =====================================================

async function loadTrips() {

    const response =
        await fetch(
            "/api/Ticket/today-trips",
            {
                headers:
                    headers()
            }
        );

    const data =
        await response.json()
            .catch(
                () => []
            );

    if (!response.ok) {
        throw new Error(
            data.message ||
            "Không thể tải danh sách chuyến."
        );
    }


    tripSelect.innerHTML =
        `
        <option value="">
            -- Chọn chuyến hôm nay --
        </option>
        `;


    data.forEach(
        trip => {

            const option =
                document.createElement(
                    "option"
                );

            option.value =
                trip.tripId;

            option.textContent =
                `${trip.tripCode} | ${trip.departureTime
                } | ${trip.routeCode
                }`;

            tripSelect.appendChild(
                option
            );
        }
    );
}


// =====================================================
// SELECT TRIP
// =====================================================

tripSelect.addEventListener(
    "change",
    () => {

        selectedTripId =
            Number(
                tripSelect.value
            ) || null;


        const selected =
            [...tripSelect.options]
                .find(
                    option =>
                        Number(
                            option.value
                        ) ===
                        selectedTripId
                );


        document.getElementById(
            "selectedTrip"
        ).style.display =
            selectedTripId
                ? "block"
                : "none";


        if (selected) {

            document.getElementById(
                "selectedTripCode"
            ).textContent =
                selected.textContent;

            document.getElementById(
                "selectedTripInfo"
            ).textContent =
                "Đang kiểm tra vé cho chuyến này.";
        }


        updateVerifyButton();

    }
);


// =====================================================
// QR INPUT
// =====================================================

qrToken.addEventListener(
    "input",
    updateVerifyButton
);


function updateVerifyButton() {

    verifyBtn.disabled =
        !selectedTripId ||
        !qrToken.value.trim();
}


// =====================================================
// VERIFY QR
// =====================================================

async function verifyQR() {

    const token =
        qrToken.value.trim();


    if (!selectedTripId) {

        showToast(
            "❌ Hãy chọn chuyến xe."
        );

        return;
    }


    if (!token) {

        showToast(
            "❌ Hãy nhập hoặc quét mã QR."
        );

        return;
    }


    verifyBtn.disabled =
        true;

    useBtn.disabled =
        true;


    try {

        const response =
            await fetch(
                "/api/Ticket/verify-qr",
                {
                    method:
                        "POST",

                    headers:
                        headers(),

                    body:
                        JSON.stringify({

                            qrToken:
                                token,

                            tripId:
                                selectedTripId
                        })
                }
            );


        const data =
            await response.json()
                .catch(
                    () => ({})
                );


        if (!response.ok) {

            showInvalid(
                data.message ||
                "Vé không hợp lệ."
            );

            return;
        }


        verifiedTicket =
            data.ticket;


        showValid(
            verifiedTicket
        );

    } catch (error) {

        console.error(error);

        showInvalid(
            "Không thể kết nối máy chủ."
        );

    } finally {

        verifyBtn.disabled =
            !selectedTripId ||
            !qrToken.value.trim();
    }
}


// =====================================================
// VALID
// =====================================================

function showValid(ticket) {

    document.getElementById(
        "emptyResult"
    ).style.display =
        "none";


    const box =
        document.getElementById(
            "resultBox"
        );


    box.className =
        "result valid";


    document.getElementById(
        "resultTitle"
    ).textContent =
        "✓ Vé hợp lệ";


    document.getElementById(
        "ticketCode"
    ).textContent =
        ticket.ticketCode || "—";


    document.getElementById(
        "passengerName"
    ).textContent =
        ticket.passengerName || "—";


    document.getElementById(
        "seatCode"
    ).textContent =
        ticket.seatCode || "—";


    document.getElementById(
        "routeCode"
    ).textContent =
        ticket.routeCode || "—";


    document.getElementById(
        "departureStop"
    ).textContent =
        ticket.departureStop || "—";


    document.getElementById(
        "arrivalStop"
    ).textContent =
        ticket.arrivalStop || "—";


    document.getElementById(
        "ticketStatus"
    ).textContent =
        ticket.status || "VALID";


    document.getElementById(
        "statusMessage"
    ).textContent =
        "Có thể xác nhận hành khách đã lên xe.";


    useBtn.disabled =
        false;
}


// =====================================================
// INVALID
// =====================================================

function showInvalid(message) {

    verifiedTicket =
        null;


    document.getElementById(
        "emptyResult"
    ).style.display =
        "none";


    const box =
        document.getElementById(
            "resultBox"
        );


    box.className =
        "result invalid";


    document.getElementById(
        "resultTitle"
    ).textContent =
        "✕ Vé không hợp lệ";


    document.getElementById(
        "ticketCode"
    ).textContent =
        "—";


    document.getElementById(
        "passengerName"
    ).textContent =
        "—";


    document.getElementById(
        "seatCode"
    ).textContent =
        "—";


    document.getElementById(
        "routeCode"
    ).textContent =
        "—";


    document.getElementById(
        "departureStop"
    ).textContent =
        "—";


    document.getElementById(
        "arrivalStop"
    ).textContent =
        "—";


    document.getElementById(
        "ticketStatus"
    ).textContent =
        "INVALID";


    document.getElementById(
        "statusMessage"
    ).textContent =
        message;


    useBtn.disabled =
        true;
}


// =====================================================
// USE TICKET
// =====================================================

useBtn.addEventListener(
    "click",
    async () => {

        if (!verifiedTicket) {
            return;
        }


        if (!selectedTripId) {
            return;
        }


        useBtn.dataset.confirming =
            "true";

        useBtn.textContent =
            "Bấm lần nữa để xác nhận";

        useBtn.classList.add(
            "confirming"
        );

        if (
            useBtn.dataset.confirmed !==
            "true"
        ) {

            useBtn.dataset.confirmed =
                "true";

            setTimeout(
                () => {

                    useBtn.dataset.confirmed =
                        "false";

                    useBtn.dataset.confirming =
                        "false";

                    useBtn.textContent =
                        "Xác nhận đã sử dụng";

                    useBtn.classList.remove(
                        "confirming"
                    );

                },
                3000
            );

            return;
        }

        useBtn.dataset.confirmed =
            "false";

        useBtn.disabled =
            true;


        try {

            const response =
                await fetch(
                    `/api/Ticket/${verifiedTicket.ticketId
                    }/use?tripId=${selectedTripId
                    }`,
                    {
                        method:
                            "POST",

                        headers:
                            headers()
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
                    "Không thể xác nhận vé."
                );
            }


            document.getElementById(
                "ticketStatus"
            ).textContent =
                "USED";


            document.getElementById(
                "statusMessage"
            ).textContent =
                data.message ||
                "Đã xác nhận vé đã sử dụng.";


            const box =
                document.getElementById(
                    "resultBox"
                );


            box.className =
                "result valid";


            useBtn.textContent =
                "✓ Đã xác nhận sử dụng";


            verifiedTicket =
                null;


            qrToken.value =
                "";

        } catch (error) {

            console.error(error);

            showInvalid(
                error.message ||
                "Không thể xác nhận vé."
            );
        }
    }
);


// =====================================================
// SCANNER
// =====================================================

let scanner = null;


function startScanner() {

    if (
        typeof Html5Qrcode ===
        "undefined"
    ) {

        return;
    }


    scanner =
        new Html5Qrcode(
            "reader"
        );


    scanner.start(
        {
            facingMode:
                "environment"
        },
        {
            fps:
                10,

            qrbox:
            {
                width:
                    240,

                height:
                    240
            }
        },
        decodedText => {

            qrToken.value =
                decodedText;

            updateVerifyButton();

            verifyQR();

        },
        () => {
            // Không cần xử lý lỗi từng frame
        }
    )
        .catch(
            error => {

                console.warn(
                    "Không mở được camera:",
                    error
                );
            }
        );
}


// =====================================================
// START
// =====================================================

async function init() {

    if (!checkLogin()) {
        return;
    }


    try {

        await loadTrips();

    } catch (error) {

        console.error(error);

        showToast(
            "❌ " +
            (
                error.message ||
                "Không thể tải chuyến xe."
            )
        );
    }

    const driverLogout =
        document.getElementById(
            "driverLogout"
        );

    driverLogout?.addEventListener(
        "click",
        () => {

            localStorage.removeItem(
                "userSession"
            );

            window.location.href =
                "../auth/dangnhap.html";
        }
    );

    startScanner();
}


init();
