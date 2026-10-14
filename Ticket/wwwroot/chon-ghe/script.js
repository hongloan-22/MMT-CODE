const MAX_SEATS = 5;

let seats = [];
let selectedSeats = new Set();

let tripId = null;
let fare = 0;

const seatMap =
    document.getElementById("seatMap");

const selectedList =
    document.getElementById("selectedList");

const seatCount =
    document.getElementById("seatCount");

const totalPrice =
    document.getElementById("totalPrice");

const continueBtn =
    document.getElementById("continueBtn");

const tripRoute =
    document.getElementById("tripRoute");

const tripInfo =
    document.getElementById("tripInfo");

const tripFare =
    document.getElementById("tripFare");

const busInfo =
    document.getElementById("busInfo");

const originName =
    document.getElementById("originName");

const destinationName =
    document.getElementById("destinationName");


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
// FORMAT MONEY
// =====================================================

function formatMoney(value) {

    return new Intl.NumberFormat(
        "vi-VN"
    ).format(value) + "đ";
}


// =====================================================
// GET TRIP ID
// =====================================================

function getTripId() {

    const params =
        new URLSearchParams(
            window.location.search
        );

    return Number(
        params.get("tripId")
    );
}


// =====================================================
// LOAD SEAT MAP
// =====================================================

async function loadSeatMap() {

    tripId =
        getTripId();

    if (!tripId || tripId <= 0) {

        showError(
            "Không xác định được chuyến xe."
        );

        return;
    }

    try {

        const response =
            await fetch(
                `/api/Seat/trip/${tripId}`
            );

        const data =
            await response.json()
                .catch(
                    () => ({})
                );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể tải dữ liệu ghế."
            );
        }

        seats =
            data.seats || [];

        fare =
            Number(
                data.fare || 0
            );

        tripFare.textContent =
            formatMoney(fare);

        tripRoute.textContent =
            `${data.startPoint} → ${data.endPoint}`;

        tripInfo.textContent =
            `${data.tripCode} • ${formatDate(data.tripDate)
            } • ${data.departureTime
            }`;

        busInfo.textContent =
            `${data.busCode} • ${data.licensePlate
            }`;

        originName.textContent =
            data.startPoint ||
            "Đang tải...";

        destinationName.textContent =
            data.endPoint ||
            "Đang tải...";

        renderSeats();

        renderSummary();

    } catch (error) {

        console.error(error);

        showError(
            error.message ||
            "Không thể tải sơ đồ ghế."
        );
    }
}


// =====================================================
// FORMAT DATE
// =====================================================

function formatDate(dateString) {

    if (!dateString)
        return "";

    const parts =
        String(dateString)
            .split("-");

    if (parts.length !== 3)
        return dateString;

    return `
        ${parts[2]}/
        ${parts[1]}/
        ${parts[0]}
    `;
}


// =====================================================
// ERROR
// =====================================================

function showError(message) {

    seatMap.innerHTML = `
        <div style="
            grid-column:1 / -1;
            text-align:center;
            color:#e55353;
            padding:30px;
        ">
            ${message}
        </div>
    `;

    continueBtn.disabled =
        true;
}


// =====================================================
// CREATE SEAT
// =====================================================

function createSeatElement(seat) {

    const button =
        document.createElement(
            "button"
        );

    button.type =
        "button";

    button.className =
        `seat ${seat.status}`;

    button.textContent =
        seat.seatCode;

    button.dataset.seatId =
        seat.seatCode;

    button.setAttribute(
        "aria-label",
        `Ghế ${seat.seatCode
        } - ${getStatusText(
            seat.status
        )
        }`
    );

    if (
        seat.status !==
        "available"
    ) {

        button.disabled =
            true;
    }

    if (
        selectedSeats.has(
            seat.seatCode
        )
    ) {

        button.classList.add(
            "selected"
        );
    }

    button.addEventListener(
        "click",
        () =>
            toggleSeat(
                seat.seatCode
            )
    );

    return button;
}


// =====================================================
// RENDER 21 GHẾ
// =====================================================

function renderSeats() {

    seatMap.innerHTML =
        "";

    const seatLookup =
        new Map(
            seats.map(
                seat => [
                    seat.seatCode,
                    seat
                ]
            )
        );

    const verticalLayout = [
        ["A01", "B01", null, "C01", "D01"],
        ["A02", "B02", null, "C02", "D02"],
        ["A03", "B03", null, "C03", "D03"],
        ["A04", "B04", null, "C04", "D04"]
    ];

    verticalLayout.forEach(
        row => {

            row.forEach(
                seatCode => {

                    if (
                        seatCode ===
                        null
                    ) {

                        const aisle =
                            document.createElement(
                                "div"
                            );

                        aisle.className =
                            "aisle";

                        seatMap.appendChild(
                            aisle
                        );

                        return;
                    }

                    const seat =
                        seatLookup.get(
                            seatCode
                        );

                    if (!seat) {

                        const empty =
                            document.createElement(
                                "div"
                            );

                        empty.className =
                            "seat";

                        empty.style.visibility =
                            "hidden";

                        seatMap.appendChild(
                            empty
                        );

                        return;
                    }

                    seatMap.appendChild(
                        createSeatElement(
                            seat
                        )
                    );
                }
            );
        }
    );


    const backRow =
        document.createElement(
            "div"
        );

    backRow.className =
        "back-row";

    const backTitle =
        document.createElement(
            "div"
        );

    backTitle.className =
        "back-row-title";

    backTitle.textContent =
        "GHẾ CUỐI XE";

    backRow.appendChild(
        backTitle
    );


    const backSeats =
        document.createElement(
            "div"
        );

    backSeats.className =
        "back-seats";


    [
        "E01",
        "E02",
        "E03",
        "E04",
        "E05"
    ].forEach(
        seatCode => {

            const seat =
                seatLookup.get(
                    seatCode
                );

            if (seat) {

                backSeats.appendChild(
                    createSeatElement(
                        seat
                    )
                );
            }
        }
    );


    backRow.appendChild(
        backSeats
    );

    seatMap.appendChild(
        backRow
    );
}


// =====================================================
// STATUS TEXT
// =====================================================

function getStatusText(
    status
) {

    const map = {

        available:
            "còn trống",

        booked:
            "đã đặt",

        held:
            "đang được giữ",

        unavailable:
            "không bán"
    };

    return (
        map[status] ||
        ""
    );
}


// =====================================================
// SELECT SEAT
// =====================================================

function toggleSeat(
    seatCode
) {

    if (
        selectedSeats.has(
            seatCode
        )
    ) {

        selectedSeats.delete(
            seatCode
        );

    } else {

        if (
            selectedSeats.size >=
            MAX_SEATS
        ) {

            showToast(
                `⚠️ Bạn chỉ có thể chọn tối đa ${MAX_SEATS} ghế.`
            );

            return;
        }

        selectedSeats.add(
            seatCode
        );
    }

    renderSeats();

    renderSummary();
}


// =====================================================
// SUMMARY
// =====================================================

function renderSummary() {

    const count =
        selectedSeats.size;

    const total =
        count * fare;

    seatCount.textContent =
        `${count} ghế`;

    totalPrice.textContent =
        formatMoney(total);

    continueBtn.disabled =
        count === 0;


    if (count === 0) {

        selectedList.innerHTML =
            `
            <span class="empty-selection">
                Bạn chưa chọn ghế nào
            </span>
            `;

        return;
    }


    selectedList.innerHTML =
        "";


    [
        ...selectedSeats
    ]
        .sort()
        .forEach(
            seatCode => {

                const chip =
                    document.createElement(
                        "span"
                    );

                chip.className =
                    "selected-chip";

                chip.textContent =
                    seatCode;

                selectedList.appendChild(
                    chip
                );
            }
        );
}


// =====================================================
// TẠO GIỮ CHỖ
// =====================================================

async function createHold() {

    const session =
        getSession();

    const userId =
        session.userId ||
        session.UserId ||
        "";

    if (!userId) {

        showToast(
            "⚠️ Vui lòng đăng nhập trước khi đặt vé."
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


    const seatCodes =
        [
            ...selectedSeats
        ].sort();


    if (
        !tripId ||
        seatCodes.length === 0
    ) {

        return;
    }


    continueBtn.disabled =
        true;

    continueBtn.textContent =
        "Đang giữ ghế...";


    try {

        const response =
            await fetch(
                "/api/Booking/hold",
                {
                    method:
                        "POST",

                    headers: {

                        "Content-Type":
                            "application/json",

                        "X-User-Id":
                            userId
                    },

                    body:
                        JSON.stringify({
                            tripId:
                                tripId,

                            seatCodes:
                                seatCodes
                        })
                }
            );


        const data =
            await response.json()
                .catch(
                    () => ({})
                );


        if (!response.ok) {

            if (
                Array.isArray(
                    data.heldSeats
                )
            ) {

                showToast(
                    `⚠️ Ghế ${data.heldSeats.join(", ")} vừa được người khác giữ.`
                );

            } else {

                showToast(
                    "❌ " +
                    (
                        data.message ||
                        "Không thể giữ ghế."
                    )
                );
            }

            await loadSeatMap();

            return;
        }


        // ==========================================
        // LƯU BOOKING
        // ==========================================

        sessionStorage.setItem(
            "currentBooking",
            JSON.stringify(data)
        );


        // ==========================================
        // CHUYỂN SANG THANH TOÁN
        // ==========================================

        const params =
            new URLSearchParams({

                bookingId:
                    String(
                        data.bookingId
                    )
            });


        window.location.href =
            `../thanh-toan/index.html?${params.toString()}`;

    } catch (error) {

        console.error(error);

        showToast(
            "❌ Không thể kết nối đến máy chủ."
        );

    } finally {

        continueBtn.disabled =
            selectedSeats.size === 0;

        continueBtn.textContent =
            "Tiếp tục";
    }
}


// =====================================================
// CONTINUE
// =====================================================

continueBtn.addEventListener(
    "click",
    createHold
);


// =====================================================
// LOAD
// =====================================================

loadSeatMap();
