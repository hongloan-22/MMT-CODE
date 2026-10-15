// @ts-nocheck

const fromInput =
    document.getElementById("from");

const toInput =
    document.getElementById("to");

const departureInput =
    document.getElementById("departure");

const departureTimeInput =
    document.getElementById("departureTime");

const returnInput =
    document.getElementById("returnDate");

const clearReturn =
    document.getElementById("clearReturn");

const swapBtn =
    document.getElementById("swapBtn");

const searchBtn =
    document.getElementById("searchBtn");

const formMessage =
    document.getElementById("formMessage");

const tripList =
    document.getElementById("tripList");

const resultSummary =
    document.getElementById("resultSummary");

const resultCount =
    document.getElementById("resultCount");

const loginBtn =
    document.getElementById("loginBtn");


let locations = [];

let currentTripsData = [];


// =========================================================
// DATE
// =========================================================

const today = new Date();

const todayString =
    `${today.getFullYear()}-${String(
        today.getMonth() + 1
    ).padStart(2, "0")}-${String(
        today.getDate()
    ).padStart(2, "0")}`;


if (departureInput) {

    departureInput.min =
        todayString;

    departureInput.value =
        todayString;
}


// =========================================================
// LOAD STATIONS FROM SQL SERVER
// =========================================================

async function loadStationsFromDB() {

    try {

        const response =
            await fetch(
                "/api/BusStops"
            );


        if (!response.ok) {

            throw new Error(
                "Không thể tải trạm."
            );
        }


        const data =
            await response.json();


        if (
            Array.isArray(data)
        ) {

            locations =
                data
                    .filter(
                        item =>
                            item.isActive !==
                            false &&
                            item.name
                    )
                    .map(
                        item =>
                            item.name
                    )
                    .filter(
                        (
                            value,
                            index,
                            array
                        ) =>
                            array.indexOf(
                                value
                            ) === index
                    );
        }

    }
    catch (error) {

        console.error(
            error
        );

        if (formMessage) {

            formMessage.textContent =
                "Không thể tải danh sách trạm từ cơ sở dữ liệu.";
        }
    }
}


loadStationsFromDB();


// =========================================================
// FORMAT
// =========================================================

function formatDate(value) {

    if (!value)
        return "";

    const parts =
        String(value)
            .split("-");


    if (
        parts.length === 3
    ) {

        return `${parts[2]}/${parts[1]}/${parts[0]}`;
    }


    return value;
}


function formatMoney(value) {

    return Number(
        value || 0
    )
        .toLocaleString(
            "vi-VN"
        ) + "đ";
}


function formatDuration(
    minutes
) {

    const total =
        Number(minutes) || 0;


    if (total <= 0)
        return "—";


    const hours =
        Math.floor(
            total / 60
        );


    const mins =
        total % 60;


    if (hours <= 0) {

        return `${mins} phút`;
    }


    return `${hours} giờ ${mins} phút`;
}


function formatDateTime(
    value
) {

    if (!value)
        return "—";


    const date =
        new Date(value);


    if (
        Number.isNaN(
            date.getTime()
        )
    ) {

        return "—";
    }


    return date.toLocaleTimeString(
        "vi-VN",
        {
            hour:
                "2-digit",

            minute:
                "2-digit",

            hour12:
                false
        }
    );
}


// =========================================================
// REMOVE VIETNAMESE TONES
// =========================================================

function removeVietnameseTones(
    str
) {

    if (!str)
        return "";


    return str
        .normalize("NFD")
        .replace(
            /[\u0300-\u036f]/g,
            ""
        )
        .replace(
            /đ/g,
            "d"
        )
        .replace(
            /Đ/g,
            "D"
        )
        .toLowerCase();
}


// =========================================================
// SUGGESTIONS
// =========================================================

function setupSuggestions(
    inputId,
    boxId
) {

    const input =
        document.getElementById(
            inputId
        );


    let box =
        document.getElementById(
            boxId
        );


    if (!input)
        return;


    if (!box) {

        box =
            document.createElement(
                "div"
            );

        box.id =
            boxId;

        input.parentElement
            .appendChild(
                box
            );
    }


    input.autocomplete =
        "off";


    box.parentElement.style.position =
        "relative";


    box.style.position =
        "absolute";


    box.style.top =
        "100%";


    box.style.left =
        "0";


    box.style.width =
        "100%";


    box.style.backgroundColor =
        "#fff";


    box.style.border =
        "1px solid #087df5";


    box.style.borderRadius =
        "8px";


    box.style.boxShadow =
        "0 10px 30px rgba(0,0,0,0.2)";


    box.style.zIndex =
        "999999";


    box.style.marginTop =
        "4px";


    box.style.maxHeight =
        "250px";


    box.style.overflowY =
        "auto";


    box.style.display =
        "none";


    function renderList(
        list
    ) {

        box.innerHTML =
            "";


        if (!list.length) {

            box.style.display =
                "none";

            return;
        }


        list.forEach(
            loc => {

                const item =
                    document.createElement(
                        "div"
                    );


                item.textContent =
                    loc;


                item.style.padding =
                    "14px 18px";


                item.style.cursor =
                    "pointer";


                item.style.borderBottom =
                    "1px solid #f0f5fa";


                item.style.color =
                    "#12345b";


                item.style.fontWeight =
                    "bold";


                item.addEventListener(
                    "mouseenter",
                    () =>
                        item.style.backgroundColor =
                        "#eef5fc"
                );


                item.addEventListener(
                    "mouseleave",
                    () =>
                        item.style.backgroundColor =
                        "transparent"
                );


                item.addEventListener(
                    "mousedown",
                    event => {

                        event.preventDefault();

                        input.value =
                            loc;

                        box.style.display =
                            "none";
                    }
                );


                box.appendChild(
                    item
                );
            }
        );


        box.style.display =
            "block";
    }


    function showList() {

        const value =
            removeVietnameseTones(
                input.value.trim()
            );


        const filtered =
            value

                ? locations.filter(
                    location =>
                        removeVietnameseTones(
                            location
                        ).includes(
                            value
                        )
                )

                : locations;


        renderList(
            filtered
        );
    }


    input.addEventListener(
        "focus",
        showList
    );


    input.addEventListener(
        "click",
        showList
    );


    input.addEventListener(
        "input",
        showList
    );


    document.addEventListener(
        "click",
        event => {

            if (
                event.target !==
                input &&
                !box.contains(
                    event.target
                )
            ) {

                box.style.display =
                    "none";
            }
        }
    );
}


setupSuggestions(
    "from",
    "fromSuggestions"
);


setupSuggestions(
    "to",
    "toSuggestions"
);


// =========================================================
// SWAP
// =========================================================

if (
    swapBtn &&
    fromInput &&
    toInput
) {

    swapBtn.addEventListener(
        "click",
        () => {

            const temp =
                fromInput.value;


            fromInput.value =
                toInput.value;


            toInput.value =
                temp;
        }
    );
}


// =========================================================
// RENDER TRIPS
// =========================================================

function renderTrips(
    list
) {

    if (
        !tripList ||
        !resultCount
    ) {

        return;
    }


    resultCount.textContent =
        `${list.length} chuyến`;


    if (!list.length) {

        tripList.innerHTML =
            `
                <div class="empty-state">

                    <div class="empty-icon">
                        🚌
                    </div>

                    <h3>
                        Không tìm thấy chuyến phù hợp
                    </h3>

                    <p>
                        Không có chuyến phù hợp với
                        <b>
                            ${escapeHtml(
                fromInput?.value ||
                ""
            )}
                        </b>
                        →
                        <b>
                            ${escapeHtml(
                toInput?.value ||
                ""
            )}
                        </b>
                        vào ngày
                        <b>
                            ${formatDate(
                departureInput?.value
            )}
                        </b>.
                    </p>

                </div>
            `;

        return;
    }


    tripList.innerHTML =
        list
            .map(
                trip => {

                    const active =
                        trip.isActive !== false;


                    return `
                        <article
                            class="trip-card">

                            <div
                                class="trip-head">

                                <div
                                    class="operator">

                                    <div
                                        class="operator-icon">
                                        🚌
                                    </div>

                                    <div>

                                        <strong>
                                            ${escapeHtml(
                        trip.routeName ||
                        ""
                    )}
                                        </strong>

                                        <small>
                                            Mã chuyến:
                                            ${escapeHtml(
                        trip.tripCode ||
                        `TRIP-${trip.tripId}`
                    )}
                                        </small>

                                    </div>

                                </div>

                                <span
                                    class="trip-status"
                                    style="
                                    background:#e8f8f0;
                                    color:#27ae60;
                                    ">

                                    ${active
                            ? "Đang hoạt động"
                            : "Ngừng hoạt động"
                        }

                                </span>

                            </div>


                            <div
                                class="trip-route">

                                <div
                                    class="time">

                                    <strong>
                                        ${formatDateTime(
                            trip.departureDateTime
                        )
                        }
                                    </strong>

                                    <small>
                                        ${escapeHtml(
                            trip.originStopName
                        )
                        }
                                    </small>

                                </div>


                                <div
                                    class="route-line">

                                    <span
                                        class="route-arrow">
                                        →
                                    </span>

                                    <small>
                                        ${formatDuration(
                            trip.durationMinutes
                        )
                        }
                                    </small>

                                    <span
                                        class="route-arrow">
                                        →
                                    </span>

                                </div>


                                <div
                                    class="time">

                                    <strong>
                                        ${formatDateTime(
                            trip.arrivalDateTime
                        )
                        }
                                    </strong>

                                    <small>
                                        ${escapeHtml(
                            trip.destinationStopName
                        )
                        }
                                    </small>

                                </div>

                            </div>


                            <div
                                class="trip-bottom">

                                <div
                                    class="trip-meta">

                                    <span>
                                        ✓ Lộ trình đúng chiều
                                    </span>

                                    <span>
                                        ▣ Cự ly:
                                        ${Number(
                            trip.distanceKm ||
                            0
                        ).toFixed(1)
                        }
                                        km
                                    </span>

                                    <span>
                                        ⏱
                                        ${formatDuration(
                            trip.durationMinutes
                        )
                        }
                                    </span>

                                    <span>
                                        💺 Còn
                                        ${Number(
                            trip.availableSeats ||
                            0
                        )
                        }
                                        chỗ
                                    </span>

                                </div>


                                <div>

                                    <span
                                        class="trip-price">

                                        ${formatMoney(
                            trip.fare
                        )
                        }

                                    </span>

                                    <button
                                        class="select-trip-btn"
                                        data-trip-id="${trip.tripId
                        }"
                                        data-trip-code="${trip.tripCode ||
                        `TRIP-${trip.tripId}`
                        }">

                                        Chi tiết chuyến

                                    </button>

                                </div>

                            </div>

                        </article>
                    `;
                }
            )
            .join("");


    document
        .querySelectorAll(
            ".select-trip-btn"
        )
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    () => {

                        const tripId =
                            button.dataset
                                .tripId;


                        const tripCode =
                            button.dataset
                                .tripCode;


                        const from =
                            encodeURIComponent(
                                fromInput?.value ||
                                ""
                            );


                        const to =
                            encodeURIComponent(
                                toInput?.value ||
                                ""
                            );


                        const date =
                            departureInput
                                ?.value ||
                            todayString;


                        window.location.href =
                            `/chon-ghe/index.html?tripId=${encodeURIComponent(
                                tripId
                            )}&tripCode=${encodeURIComponent(
                                tripCode
                            )}&from=${from}&to=${to}&date=${date}`;
                    }
                );
            }
        );
}


// =========================================================
// FILTER
// =========================================================

function getTimeCategory(
    timeStr
) {

    if (!timeStr)
        return "morning";


    const hour =
        parseInt(
            String(
                timeStr
            ).split(":")[0],
            10
        );


    if (
        Number.isNaN(hour)
    ) {

        return "morning";
    }


    if (hour < 12)
        return "morning";


    if (hour < 18)
        return "afternoon";


    return "evening";
}


function getFilteredTrips() {

    const timeRadio =
        document.querySelector(
            'input[name="timeFilter"]:checked'
        );


    const priceRadio =
        document.querySelector(
            'input[name="priceFilter"]:checked'
        );


    const time =
        timeRadio
            ? timeRadio.value
            : "all";


    const price =
        priceRadio
            ? priceRadio.value
            : "all";


    return currentTripsData.filter(
        trip => {

            const timeOk =
                time === "all" ||
                trip.timeCategory ===
                time;


            const priceOk =
                price ===
                "all" ||

                (
                    price ===
                    "low" &&
                    trip.fare <
                    100000
                ) ||

                (
                    price ===
                    "mid" &&
                    trip.fare >=
                    100000 &&
                    trip.fare <=
                    150000
                ) ||

                (
                    price ===
                    "high" &&
                    trip.fare >
                    150000
                );


            return (
                timeOk &&
                priceOk
            );
        }
    );
}


// =========================================================
// SEARCH
// =========================================================

async function executeSearch() {

    const from =
        fromInput
            ? fromInput.value.trim()
            : "";


    const to =
        toInput
            ? toInput.value.trim()
            : "";


    const departure =
        departureInput
            ? departureInput.value
            : "";


    const departureTime =
        departureTimeInput
            ? departureTimeInput.value
            : "";


    if (!from || !to) {

        if (formMessage) {

            formMessage.style.color =
                "#e25858";

            formMessage.textContent =
                "Vui lòng chọn nơi xuất phát và nơi đến.";
        }

        return;
    }


    if (!departure) {

        if (formMessage) {

            formMessage.style.color =
                "#e25858";

            formMessage.textContent =
                "Vui lòng chọn ngày đi.";
        }

        return;
    }


    if (formMessage) {

        formMessage.style.color =
            "#1685e8";

        formMessage.textContent =
            "Đang tìm chuyến từ cơ sở dữ liệu...";
    }


    try {

        const params =
            new URLSearchParams();


        params.set(
            "diemDi",
            from
        );


        params.set(
            "diemDen",
            to
        );


        params.set(
            "ngay",
            departure
        );


        if (departureTime) {

            params.set(
                "gio",
                departureTime
            );
        }


        const response =
            await fetch(
                `/api/TraCuuTuyenXe/theo-diem?${params.toString()}`
            );


        const data =
            await response
                .json()
                .catch(
                    () => []
                );


        if (!response.ok) {

            throw new Error(
                data?.message ||
                "Không thể tra cứu chuyến."
            );
        }


        currentTripsData =
            (
                Array.isArray(data)
                    ? data
                    : []
            )
                .map(
                    item => {

                        const departTime =
                            item.departureDateTime
                                ? new Date(
                                    item.departureDateTime
                                )
                                    .toTimeString()
                                    .slice(0, 5)
                                : "";


                        return {

                            tripId:
                                item.tripId,

                            tripCode:
                                item.tripCode ||
                                `TRIP-${item.tripId}`,

                            routeCode:
                                item.routeCode ||
                                "",

                            routeName:
                                item.routeName ||
                                "",

                            from:
                                item.originStopName ||
                                from,

                            to:
                                item.destinationStopName ||
                                to,

                            departureDateTime:
                                item.departureDateTime,

                            arrivalDateTime:
                                item.arrivalDateTime,

                            depart:
                                departTime,

                            arrive:
                                item.arrivalDateTime
                                    ? new Date(
                                        item.arrivalDateTime
                                    )
                                        .toTimeString()
                                        .slice(
                                            0,
                                            5
                                        )
                                    : "",

                            fare:
                                Number(
                                    item.fare
                                ) || 0,

                            price:
                                Number(
                                    item.fare
                                ) || 0,

                            distance:
                                Number(
                                    item.distanceKm
                                ) || 0,

                            durationMinutes:
                                Number(
                                    item.durationMinutes
                                ) || 0,

                            seats:
                                Number(
                                    item.availableSeats
                                ) || 0,

                            availableSeats:
                                Number(
                                    item.availableSeats
                                ) || 0,

                            timeCategory:
                                getTimeCategory(
                                    departTime
                                ),

                            isActive:
                                item.isActive !==
                                false
                        };
                    }
                );

    }
    catch (error) {

        console.error(
            error
        );

        currentTripsData =
            [];


        if (formMessage) {

            formMessage.style.color =
                "#e25858";

            formMessage.textContent =
                error.message ||
                "Không thể kết nối hệ thống.";
        }
    }


    if (resultSummary) {

        resultSummary.textContent =
            `${from} → ${to} | Ngày ${formatDate(
                departure
            )}${departureTime
                ? ` | Từ ${departureTime}`
                : ""
            }`;
    }


    const filtered =
        getFilteredTrips();


    renderTrips(
        filtered
    );


    if (formMessage &&
        currentTripsData.length) {

        formMessage.textContent =
            "";
    }


    const resultsSection =
        document.getElementById(
            "resultsSection"
        );


    if (resultsSection) {

        resultsSection.scrollIntoView(
            {
                behavior:
                    "smooth"
            }
        );
    }
}


// =========================================================
// ESCAPE
// =========================================================

function escapeHtml(
    value
) {

    return String(
        value ?? ""
    )
        .replaceAll(
            "&",
            "&amp;"
        )
        .replaceAll(
            "<",
            "&lt;"
        )
        .replaceAll(
            ">",
            "&gt;"
        )
        .replaceAll(
            '"',
            "&quot;"
        )
        .replaceAll(
            "'",
            "&#039;"
        );
}


// =========================================================
// EVENTS
// =========================================================

if (searchBtn) {

    searchBtn.addEventListener(
        "click",
        event => {

            event.preventDefault();

            executeSearch();
        }
    );
}


document
    .querySelectorAll(
        'input[name="timeFilter"], input[name="priceFilter"]'
    )
    .forEach(
        input => {

            input.addEventListener(
                "change",
                () => {

                    renderTrips(
                        getFilteredTrips()
                    );
                }
            );
        }
    );


[
    fromInput,
    toInput,
    departureInput,
    departureTimeInput
]
    .forEach(
        input => {

            if (input) {

                input.addEventListener(
                    "input",
                    () => {

                        if (
                            formMessage
                        ) {

                            formMessage.textContent =
                                "";
                        }
                    }
                );
            }
        }
    );


// =========================================================
// CLEAR RETURN DATE
// =========================================================

if (clearReturn) {

    clearReturn.addEventListener(
        "click",
        event => {

            event.preventDefault();

            if (returnInput) {

                returnInput.value =
                    "";
            }
        }
    );
}


// =========================================================
// URL PARAMS
// =========================================================

window.addEventListener(
    "DOMContentLoaded",
    () => {

        const params =
            new URLSearchParams(
                window.location.search
            );


        const fromParam =
            params.get("diemDi") ||
            params.get("from");


        const toParam =
            params.get("diemDen") ||
            params.get("to");


        const departureParam =
            params.get("departure") ||
            params.get("date");


        const timeParam =
            params.get("gio");


        if (
            fromParam &&
            fromInput
        ) {

            fromInput.value =
                fromParam;
        }


        if (
            toParam &&
            toInput
        ) {

            toInput.value =
                toParam;
        }


        if (
            departureParam &&
            departureInput
        ) {

            departureInput.value =
                departureParam;
        }


        if (
            timeParam &&
            departureTimeInput
        ) {

            departureTimeInput.value =
                timeParam;
        }


        if (
            fromParam &&
            toParam &&
            departureParam
        ) {

            executeSearch();
        }


        // =====================================================
        // LOGIN STATE
        // =====================================================

        const session =
            localStorage.getItem(
                "userSession"
            );


        if (!loginBtn)
            return;


        if (session) {

            try {

                const user =
                    JSON.parse(
                        session
                    );


                loginBtn.innerHTML =
                    `👤 ${escapeHtml(
                        user.fullName ||
                        "Khách"
                    )
                    }`;


                loginBtn.style.backgroundColor =
                    "#e8f8f0";


                loginBtn.style.color =
                    "#27ae60";


                loginBtn.style.border =
                    "1px solid #27ae60";


                loginBtn.title =
                    "Nhấn để đăng xuất";


                loginBtn.onclick =
                    event => {

                        event.preventDefault();


                        if (
                            confirm(
                                "Bạn có chắc chắn muốn đăng xuất?"
                            )
                        ) {

                            localStorage.removeItem(
                                "userSession"
                            );


                            window.location.reload();
                        }
                    };

            }
            catch (
            error
            ) {

                console.error(
                    error
                );
            }

        }
        else {

            loginBtn.onclick =
                () => {

                    window.location.href =
                        "/auth/dangnhap.html";
                };
        }

    }
);
