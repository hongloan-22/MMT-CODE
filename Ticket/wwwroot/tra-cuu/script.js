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
let currentPage = 1;
let lastFilteredTrips = [];

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


function formatTripDate(value) {
    if (!value) return "—";

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "—";
    }

    return date.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric"
    });
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



function renderTrips(list) {
    if (!tripList || !resultCount) return;

    lastFilteredTrips = Array.isArray(list) ? list : [];

    const pageSize = Math.max(
        1,
        Number(
            document.getElementById("pageSizeSelect")?.value || 6
        )
    );

    const totalItems = lastFilteredTrips.length;
    const totalPages = Math.max(
        1,
        Math.ceil(totalItems / pageSize)
    );

    currentPage = Math.min(
        Math.max(1, currentPage),
        totalPages
    );

    resultCount.textContent = `${totalItems} chuyến`;

    if (totalItems === 0) {
        const hasData = currentTripsData.length > 0;

        tripList.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">
                    ${hasData ? "🔎" : "🚌"}
                </div>

                <h3>
                    ${hasData
                ? "Không có chuyến phù hợp với bộ lọc"
                : "Hiện chưa có chuyến phù hợp"}
                </h3>

                <p>
                    ${hasData
                ? "Hãy thay đổi bộ lọc giờ khởi hành, giá vé hoặc trạng thái."
                : "Vui lòng thử lại sau hoặc thay đổi điều kiện tìm kiếm."}
                </p>

                <button type="button"
                        class="reset-filter-btn"
                        id="emptyResetFilters">
                    Xóa bộ lọc
                </button>
            </div>`;

        renderPagination(0, pageSize);

        document.getElementById("emptyResetFilters")
            ?.addEventListener("click", resetTripFilters);

        return;
    }

    const startIndex = (currentPage - 1) * pageSize;

    const pageItems = lastFilteredTrips.slice(
        startIndex,
        startIndex + pageSize
    );

    tripList.innerHTML = pageItems.map(trip => {
        const status = getEffectiveTripStatus(trip);

        const statusInfo = {
            ACTIVE: {
                label: "Còn chỗ",
                className: "status-active"
            },
            DEPARTING_SOON: {
                label: "Sắp khởi hành",
                className: "status-soon"
            },
            SOLD_OUT: {
                label: "Hết chỗ",
                className: "status-sold-out"
            },
            UNAVAILABLE: {
                label: "Không khả dụng",
                className: "status-unavailable"
            }
        }[status];

        const availableSeats = Number(
            trip.availableSeats ?? trip.seats ?? 0
        );

        const totalSeats = Number(trip.totalSeats ?? 0);

        const tripCode =
            trip.tripCode || `TRIP-${trip.tripId}`;

        const disabled =
            status === "SOLD_OUT" ||
            status === "UNAVAILABLE";

        return `
            <article class="trip-card">
                <div class="trip-head">
                    <div class="operator">
                        <div class="operator-icon">🚌</div>

                        <div>
                            <strong>
                                ${escapeHtml(trip.routeName || "Tuyến xe")}
                            </strong>

                            <small>
                                Mã chuyến:
                                ${escapeHtml(tripCode)}
                            </small>
                        </div>
                    </div>

                    <span class="trip-status ${statusInfo.className}">
                        ${statusInfo.label}
                    </span>
                </div>

                <div class="trip-route">
                    <div class="time">
                        <strong>
                            ${formatDateTime(trip.departureDateTime)}
                        </strong>

                        <small>
                            ${formatTripDate(trip.departureDateTime)}
                        </small>

                        <small>
                            ${escapeHtml(trip.originStopName || "")}
                        </small>
                    </div>

                    <div class="route-line">
                        <span class="route-arrow">→</span>

                        <small>
                            ${formatDuration(trip.durationMinutes)}
                        </small>

                        <span class="route-arrow">→</span>
                    </div>

                    <div class="time">
                        <strong>
                            ${formatDateTime(trip.arrivalDateTime)}
                        </strong>

                        <small>
                            ${formatTripDate(trip.arrivalDateTime)}
                        </small>

                        <small>
                            ${escapeHtml(trip.destinationStopName || "")}
                        </small>
                    </div>
                </div>

                <div class="trip-bottom">
                    <div class="trip-meta">
                        <span>
                            ▣ Cự ly:
                            ${Number(trip.distanceKm ?? 0).toFixed(1)} km
                        </span>

                        <span>
                            ⏱ ${formatDuration(trip.durationMinutes)}
                        </span>

                        <span>
                            💺 Còn ${availableSeats}/${totalSeats} ghế
                        </span>
                    </div>

                    <div>
                        <span class="trip-price">
                            ${formatMoney(trip.fare)}
                        </span>

                        <button
                            class="select-trip-btn"
                            data-trip-id="${trip.tripId}"
                            data-trip-code="${escapeHtml(tripCode)}"
                            ${disabled ? "disabled" : ""}>
                            ${disabled
                ? (status === "SOLD_OUT"
                    ? "Đã hết chỗ"
                    : "Không khả dụng")
                : "Chi tiết chuyến"}
                        </button>
                    </div>
                </div>
            </article>`;
    }).join("");

    // Nối sang bước chọn ghế.
    document.querySelectorAll(
        "#tripList .select-trip-btn:not(:disabled)"
    ).forEach(button => {
        button.addEventListener("click", () => {
            const tripId = button.dataset.tripId;
            const tripCode = button.dataset.tripCode;

            const trip = currentTripsData.find(
                item => String(item.tripId) === String(tripId)
            );

            const from = encodeURIComponent(
                fromInput?.value.trim() ||
                trip?.originStopName ||
                ""
            );

            const to = encodeURIComponent(
                toInput?.value.trim() ||
                trip?.destinationStopName ||
                ""
            );

            const date = trip?.departureDateTime
                ? String(trip.departureDateTime).slice(0, 10)
                : (departureInput?.value || todayString);

            window.location.href =
                `/chon-ghe/index.html?tripId=${encodeURIComponent(tripId)}` +
                `&tripCode=${encodeURIComponent(tripCode)}` +
                `&from=${from}&to=${to}` +
                `&date=${encodeURIComponent(date)}`;
        });
    });

    renderPagination(totalItems, pageSize);
}

function renderPagination(totalItems, pageSize) {
    const container = document.getElementById("tripPagination");
    if (!container) return;

    const totalPages = Math.max(
        1,
        Math.ceil(totalItems / pageSize)
    );

    if (totalItems === 0) {
        container.innerHTML = "";
        return;
    }

    const firstItem = (currentPage - 1) * pageSize + 1;
    const lastItem = Math.min(
        currentPage * pageSize,
        totalItems
    );

    let buttonsHtml = `
        <button
            class="pagination-btn"
            data-page="${currentPage - 1}"
            ${currentPage === 1 ? "disabled" : ""}>
            ‹ Trước
        </button>`;

    // Giới hạn số nút trang hiển thị để giao diện gọn.
    const windowSize = 7;

    let startPage = Math.max(1, currentPage - 3);
    let endPage = Math.min(
        totalPages,
        startPage + windowSize - 1
    );

    startPage = Math.max(
        1,
        endPage - windowSize + 1
    );

    for (let page = startPage; page <= endPage; page++) {
        buttonsHtml += `
            <button
                class="pagination-btn ${page === currentPage ? "active" : ""}"
                data-page="${page}"
                ${page === currentPage
                ? 'aria-current="page"'
                : ""}>
                ${page}
            </button>`;
    }

    buttonsHtml += `
        <button
            class="pagination-btn"
            data-page="${currentPage + 1}"
            ${currentPage === totalPages ? "disabled" : ""}>
            Sau ›
        </button>`;

    container.innerHTML = `
        <span class="pagination-info">
            Hiển thị ${firstItem}–${lastItem} trong ${totalItems} chuyến
        </span>

        <div class="pagination-buttons">
            ${buttonsHtml}
        </div>`;

    container.querySelectorAll("[data-page]").forEach(button => {
        button.addEventListener("click", () => {
            const nextPage = Number(button.dataset.page);

            if (
                nextPage < 1 ||
                nextPage > totalPages ||
                nextPage === currentPage
            ) {
                return;
            }

            currentPage = nextPage;
            renderTrips(lastFilteredTrips);

            document.getElementById("resultsSection")
                ?.scrollIntoView({
                    behavior: "smooth",
                    block: "start"
                });
        });
    });
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


function getEffectiveTripStatus(trip) {
    const originalStatus = String(
        trip.status || "ACTIVE"
    ).toUpperCase();

    const availableSeats = Number(
        trip.availableSeats ?? trip.seats ?? 0
    );

    if (
        trip.isActive === false ||
        originalStatus === "INACTIVE" ||
        originalStatus === "CANCELLED"
    ) {
        return "UNAVAILABLE";
    }

    if (
        availableSeats <= 0 ||
        originalStatus === "SOLD_OUT"
    ) {
        return "SOLD_OUT";
    }

    if (originalStatus === "DEPARTING_SOON") {
        return "DEPARTING_SOON";
    }

    return "ACTIVE";
}

function getFilteredTrips() {
    const timeValue =
        document.querySelector(
            'input[name="timeFilter"]:checked'
        )?.value || "all";

    const priceValue =
        document.querySelector(
            'input[name="priceFilter"]:checked'
        )?.value || "all";

    const statusValue =
        document.querySelector(
            'input[name="statusFilter"]:checked'
        )?.value || "all";

    const sortValue =
        document.getElementById("sortTrips")?.value ||
        "time-asc";

    const filtered = currentTripsData.filter(trip => {
        const departureTime =
            trip.depart ||
            (
                trip.departureDateTime
                    ? new Date(
                        trip.departureDateTime
                    ).toLocaleTimeString("vi-VN", {
                        hour: "2-digit",
                        minute: "2-digit",
                        hour12: false
                    })
                    : ""
            );

        const timeCategory = getTimeCategory(departureTime);
        const status = getEffectiveTripStatus(trip);
        const fare = Number(trip.fare ?? trip.price ?? 0);

        const timeMatches =
            timeValue === "all" ||
            timeCategory === timeValue;

        let priceMatches = true;

        if (priceValue === "low") {
            priceMatches = fare < 100000;
        } else if (priceValue === "mid") {
            priceMatches = fare >= 100000 && fare <= 150000;
        } else if (priceValue === "high") {
            priceMatches = fare > 150000;
        }

        const statusMatches =
            statusValue === "all" ||
            status === statusValue;

        return timeMatches && priceMatches && statusMatches;
    });

    // Sắp xếp kết quả đã lọc.
    filtered.sort((a, b) => {
        const timeA = new Date(
            a.departureDateTime || 0
        ).getTime();

        const timeB = new Date(
            b.departureDateTime || 0
        ).getTime();

        const fareA = Number(a.fare ?? a.price ?? 0);
        const fareB = Number(b.fare ?? b.price ?? 0);

        const durationA = Number(a.durationMinutes ?? 0);
        const durationB = Number(b.durationMinutes ?? 0);

        switch (sortValue) {
            case "time-desc":
                return timeB - timeA;

            case "price-asc":
                return fareA - fareB;

            case "price-desc":
                return fareB - fareA;

            case "duration-asc":
                return durationA - durationB;

            default:
                return timeA - timeB;
        }
    });

    return filtered;
}

// =========================================================
// SEARCH
// =========================================================


async function executeSearch(options = {}) {
    const initialLoad = options.initialLoad === true;

    const from = fromInput?.value.trim() || "";
    const to = toInput?.value.trim() || "";
    const departure = departureInput?.value || "";
    const departureTime = departureTimeInput?.value || "";

    // Khi người dùng chủ động tìm kiếm, yêu cầu nhập đủ thông tin.
    // Khi trang vừa mở, cho phép tải danh sách chuyến sắp tới.
    if (!initialLoad) {
        if (!from || !to) {
            if (formMessage) {
                formMessage.style.color = "#e25858";
                formMessage.textContent =
                    "Vui lòng chọn nơi xuất phát và nơi đến.";
            }
            return;
        }

        if (!departure) {
            if (formMessage) {
                formMessage.style.color = "#e25858";
                formMessage.textContent = "Vui lòng chọn ngày đi.";
            }
            return;
        }

        if (departure < todayString) {
            if (formMessage) {
                formMessage.style.color = "#e25858";
                formMessage.textContent =
                    "Ngày đi không được ở trong quá khứ.";
            }
            return;
        }
    }

    if (formMessage) {
        formMessage.style.color = "#1685e8";
        formMessage.textContent = initialLoad
            ? "Đang tải danh sách chuyến đã lên lịch..."
            : "Đang tìm chuyến từ cơ sở dữ liệu...";
    }

    if (tripList) {
        tripList.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">🚌</div>
                <h3>Đang tải chuyến xe...</h3>
                <p>Vui lòng chờ trong giây lát.</p>
            </div>`;
    }

    if (resultCount) {
        resultCount.textContent = "Đang tải...";
    }

    try {
        // API danh sách chấp nhận các tham số tùy chọn.
        // Không gửi ngày mặc định khi mới mở trang để có thể
        // tải danh sách chuyến tương lai ở nhiều ngày khác nhau.
        const params = new URLSearchParams();

        if (from) {
            params.set("diemDi", from);
        }

        if (to) {
            params.set("diemDen", to);
        }

        if (!initialLoad && departure) {
            params.set("ngay", departure);
        }

        if (departureTime) {
            params.set("gioMin", departureTime);
        }

        params.set("sortBy", "time");
        params.set("sortOrder", "asc");
        params.set("pageSize", "100");

        async function fetchTripPage(pageNumber) {
            const pageParams = new URLSearchParams(params);
            pageParams.set("page", String(pageNumber));

            const response = await fetch(
                `/api/TraCuuTuyenXe/danh-sach?${pageParams.toString()}`
            );

            const data = await response.json().catch(() => null);

            if (!response.ok) {
                throw new Error(
                    data?.message ||
                    `Không thể tải danh sách chuyến (HTTP ${response.status}).`
                );
            }

            if (!data || !Array.isArray(data.items)) {
                throw new Error(
                    "Dữ liệu API không đúng định dạng danh sách chuyến."
                );
            }

            return data;
        }

        // Đọc trang đầu tiên
        const firstPage = await fetchTripPage(1);

        let allItems = [...firstPage.items];

        const totalPages = Math.max(
            1,
            Number(firstPage.totalPages) || 1
        );

        // Nếu API chia thành nhiều trang, tiếp tục tải các trang còn lại.
        for (let page = 2; page <= totalPages; page++) {
            const pageData = await fetchTripPage(page);
            allItems.push(...pageData.items);
        }

        // Chuyển dữ liệu DTO sang cấu trúc mà giao diện sử dụng.
        currentTripsData = allItems.map(item => {
            const status = String(
                item.status || "ACTIVE"
            ).toUpperCase();

            const departureDate = item.departureDateTime
                ? new Date(item.departureDateTime)
                : null;

            const arrivalDate = item.arrivalDateTime
                ? new Date(item.arrivalDateTime)
                : null;

            const departTime =
                departureDate &&
                    !Number.isNaN(departureDate.getTime())
                    ? departureDate.toLocaleTimeString("vi-VN", {
                        hour: "2-digit",
                        minute: "2-digit",
                        hour12: false
                    })
                    : "";

            const arriveTime =
                arrivalDate &&
                    !Number.isNaN(arrivalDate.getTime())
                    ? arrivalDate.toLocaleTimeString("vi-VN", {
                        hour: "2-digit",
                        minute: "2-digit",
                        hour12: false
                    })
                    : "";

            return {
                tripId: item.tripId,
                tripCode: item.tripCode || `TRIP-${item.tripId}`,

                routeCode: item.routeCode || "",
                routeName: item.routeName || "",

                originStopName: item.originStopName || "",
                destinationStopName: item.destinationStopName || "",

                departureDateTime: item.departureDateTime,
                arrivalDateTime: item.arrivalDateTime,

                depart: departTime,
                arrive: arriveTime,

                fare: Number(item.fare ?? 0),
                price: Number(item.fare ?? 0),

                distanceKm: Number(item.distanceKm ?? 0),
                durationMinutes: Number(item.durationMinutes ?? 0),

                totalSeats: Number(item.totalSeats ?? 0),
                availableSeats: Number(item.availableSeats ?? 0),
                seats: Number(item.availableSeats ?? 0),

                status: status,
                minutesUntilDeparture:
                    Number(item.minutesUntilDeparture ?? 0),

                stopCount: Number(item.stopCount ?? 0),

                timeCategory: getTimeCategory(departTime),

                isActive:
                    status !== "INACTIVE" &&
                    status !== "CANCELLED"
            };
        });

        // Tóm tắt khu vực kết quả
        if (resultSummary) {
            if (initialLoad && !from && !to) {
                resultSummary.textContent =
                    "Danh sách các chuyến đã lên lịch sắp tới";
            } else {
                resultSummary.textContent =
                    `${from || "Mọi điểm đi"} → ${to || "Mọi điểm đến"}` +
                    (departure
                        ? ` | Ngày ${formatDate(departure)}`
                        : " | Các ngày sắp tới") +
                    (departureTime
                        ? ` | Từ ${departureTime}`
                        : "");
            }
        }

        // Áp dụng các bộ lọc giờ đi và giá đang có trên giao diện.
        renderTrips(getFilteredTrips());

        if (formMessage) {
            formMessage.textContent = "";
        }

    } catch (error) {
        console.error("Lỗi tải danh sách chuyến:", error);

        currentTripsData = [];

        if (formMessage) {
            formMessage.style.color = "#e25858";
            formMessage.textContent =
                error.message || "Không thể kết nối hệ thống.";
        }

        if (resultCount) {
            resultCount.textContent = "Lỗi tải dữ liệu";
        }

        if (tripList) {
            tripList.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">⚠️</div>
                    <h3>Không thể tải danh sách chuyến</h3>
                    <p>
                        Hãy kiểm tra kết nối và thử tìm kiếm lại.
                    </p>
                </div>`;
        }
    }

    const resultsSection = document.getElementById("resultsSection");

    if (resultsSection) {
        resultsSection.scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
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



function resetTripFilters() {
    document.querySelector(
        'input[name="timeFilter"][value="all"]'
    )?.click();

    document.querySelector(
        'input[name="priceFilter"][value="all"]'
    )?.click();

    document.querySelector(
        'input[name="statusFilter"][value="all"]'
    )?.click();

    const sortSelect = document.getElementById("sortTrips");
    const pageSizeSelect = document.getElementById("pageSizeSelect");

    if (sortSelect) {
        sortSelect.value = "time-asc";
    }

    if (pageSizeSelect) {
        pageSizeSelect.value = "6";
    }

    currentPage = 1;
    renderTrips(getFilteredTrips());
}

document.querySelectorAll(
    'input[name="timeFilter"], ' +
    'input[name="priceFilter"], ' +
    'input[name="statusFilter"]'
).forEach(input => {
    input.addEventListener("change", () => {
        currentPage = 1;
        renderTrips(getFilteredTrips());
    });
});

document.getElementById("sortTrips")
    ?.addEventListener("change", () => {
        currentPage = 1;
        renderTrips(getFilteredTrips());
    });

document.getElementById("pageSizeSelect")
    ?.addEventListener("change", () => {
        currentPage = 1;
        renderTrips(getFilteredTrips());
    });

document.getElementById("resetTripFilters")
    ?.addEventListener("click", resetTripFilters);

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


        if (fromParam && toParam && departureParam) {
            // Khi người dùng đến từ biểu mẫu tìm kiếm,
            // sử dụng đầy đủ các điều kiện trên URL.
            executeSearch();
        } else {
            // Khi người dùng mở trực tiếp trang Tra cứu chuyến,
            // tự tải danh sách các chuyến đã lên lịch sắp tới.
            executeSearch({ initialLoad: true });
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
                    async event => {

                        event.preventDefault();


                        if (await SmartbusDialog.confirm(
                            "Bạn có chắc chắn muốn đăng xuất?",
                            { title: "Xác nhận đăng xuất", confirmText: "Đăng xuất", danger: true }
                        )) {

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
