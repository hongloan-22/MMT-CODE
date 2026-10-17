(() => {

    const API = {
        recurring: "/api/LichTrinh/recurring",
        trips: "/api/LichTrinh/trips",
        buses: "/api/Bus/active-for-schedule",
        trip: "/api/LichTrinh/trip",
        routes: "/api/TuyenXe"
    };

    const state = {
        recurring: [],
        trips: [],
        buses: [],
        routes: [],
        editingId: 0
    };


    const $ = id =>
        document.getElementById(id);


    function escapeHtml(value) {

        return String(value ?? "")
            .replace(
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


    function checkAuthorization() {

        const session =
            getSession();

        const role =
            String(
                session.role ||
                session.roleCode ||
                ""
            ).toUpperCase();

        const isAdmin =
            role === "ADMIN";

        const isManager =
            role === "MANAGER";

        if (!isAdmin && !isManager) {
            window.location.href =
                "../index.html";

            return false;
        }

        return true;
    }


    function authHeaders() {

        const session =
            getSession();

        const userId =
            session.userId ||
            session.UserId ||
            "";

        return {
            "Content-Type":
                "application/json",

            "X-User-Id":
                userId
        };
    }


    function toast(message) {

        const element =
            $("toast");

        element.textContent =
            message;

        element.style.opacity =
            "1";

        clearTimeout(
            window.toastTimer
        );

        window.toastTimer =
            setTimeout(() => {

                element.style.opacity =
                    "0";

            }, 2500);
    }


    function formatDate(value) {

        if (!value)
            return "—";

        const parts =
            String(value).split("-");

        if (parts.length !== 3)
            return value;

        return `
            ${parts[2]}/${parts[1]}/${parts[0]}
        `;
    }


    function scheduleFrequency(item) {

        if (
            String(item.scheduleType)
                .toUpperCase() === "DAILY"
        ) {

            return "Hàng ngày";
        }

        const map = {
            "0": "CN",
            "1": "T2",
            "2": "T3",
            "3": "T4",
            "4": "T5",
            "5": "T6",
            "6": "T7"
        };

        return String(
            item.daysOfWeek || ""
        )
            .split(",")
            .map(x =>
                map[x.trim()] || x
            )
            .join(", ");
    }


    function statusHtml(active) {

        return active
            ? `
                <span class="status success">
                    Đang hoạt động
                </span>
              `
            : `
                <span class="status muted">
                    Ngừng hoạt động
                </span>
              `;
    }


    async function loadAll() {

        await Promise.all([
            loadRecurring(),
            loadTrips(),
            loadRoutes(),
            loadBuses()
        ]);

        renderRecurring();
        renderTrips();
        renderTripOverview();
        updateStats();
    }


    async function loadRecurring() {

        const response =
            await fetch(
                API.recurring,
                {
                    headers: authHeaders()
                }
            );

        if (!response.ok) {

            throw new Error(
                "Không thể tải lịch cố định."
            );
        }

        state.recurring =
            await response.json();
    }


    async function loadTrips() {

        const response =
            await fetch(
                API.trips,
                {
                    headers: authHeaders()
                }
            );

        if (!response.ok) {

            throw new Error(
                "Không thể tải danh sách chuyến."
            );
        }

        state.trips =
            await response.json();
    }


    async function loadBuses() {

        const response =
            await fetch(
                API.buses,
                {
                    headers: authHeaders()
                }
            );

        if (!response.ok) {

            throw new Error(
                "Không thể tải danh sách xe."
            );
        }

        state.buses =
            await response.json();
    }


    async function loadRoutes() {

        const response =
            await fetch(
                API.routes,
                {
                    headers: authHeaders()
                }
            );

        if (!response.ok) {

            throw new Error(
                "Không thể tải danh sách tuyến."
            );
        }

        const data =
            await response.json();

        if (Array.isArray(data)) {

            state.routes = data;

        } else if (
            Array.isArray(data.data)
        ) {

            state.routes =
                data.data;

        } else if (
            Array.isArray(data.routes)
        ) {

            state.routes =
                data.routes;

        } else {

            state.routes = [];
        }
    }


    function renderRouteOptions() {

        const select =
            $("routeSelect");

        const current =
            select.value;

        select.innerHTML = `
            <option value="">
                -- Chọn tuyến --
            </option>
        `;

        state.routes
            .filter(x =>
                x.isActive !== false
            )
            .forEach(route => {

                const option =
                    document.createElement(
                        "option"
                    );

                option.value =
                    route.id ||
                    route.routeId;

                option.textContent =
                    `${route.routeCode || ""} - ${route.routeName ||
                    route.name ||
                    ""
                    }`;

                select.appendChild(
                    option
                );
            });

        if (current)
            select.value =
                current;
    }


    function renderBusOptions() {

        const select =
            $("busSelect");

        const current =
            select.value;

        select.innerHTML = `
            <option value="">
                -- Chọn xe --
            </option>
        `;

        state.buses.forEach(bus => {

            const option =
                document.createElement(
                    "option"
                );

            option.value =
                bus.id;

            option.textContent =
                `${bus.busCode} - ${bus.licensePlate
                } (${bus.seatCount} ghế)`;

            select.appendChild(
                option
            );
        });

        if (current)
            select.value =
                current;
    }


    function renderRecurring() {

        const body =
            $("recurringTableBody");

        body.innerHTML = "";

        const list =
            state.recurring;

        $("recurringEmpty")
            .classList.toggle(
                "hidden",
                list.length !== 0
            );

        list.forEach(item => {

            const row =
                document.createElement(
                    "tr"
                );

            row.innerHTML = `
                <td>
                    <strong>
                        ${escapeHtml(
                item.routeCode || "—"
            )}
                    </strong>
                    <div style="
                        font-size:12px;
                        opacity:.7;
                        margin-top:3px;
                    ">
                        ${escapeHtml(
                item.routeName || ""
            )}
                    </div>
                </td>

                <td>
                    ${escapeHtml(
                item.busCode || "—"
            )}
                    <div style="
                        font-size:12px;
                        opacity:.7;
                        margin-top:3px;
                    ">
                        ${escapeHtml(
                item.licensePlate || ""
            )}
                    </div>
                </td>

                <td>
                    ${escapeHtml(
                scheduleFrequency(item)
            )}
                </td>

                <td>
                    <strong>
                        ${escapeHtml(
                item.departureTime
            )}
                    </strong>
                </td>

                <td>
                    ${formatDate(
                item.startDate
            )}
                    →
                    ${item.endDate
                    ? formatDate(
                        item.endDate
                    )
                    : "Không giới hạn"
                }
                </td>

                <td>
                    ${item.generatedTripCount || 0}
                </td>

                <td>
                    ${statusHtml(
                    item.isActive
                )}
                </td>

                <td>
                    <div style="
                        display:flex;
                        gap:6px;
                    ">

                        <button
                            class="secondary-btn"
                            type="button"
                            data-edit="${item.id}">
                            Sửa
                        </button>

                        <button
                            class="secondary-btn"
                            type="button"
                            data-delete="${item.id}">
                            Ngừng
                        </button>

                    </div>
                </td>
            `;

            body.appendChild(row);
        });


        body
            .querySelectorAll(
                "[data-edit]"
            )
            .forEach(button => {

                button.addEventListener(
                    "click",
                    () => openEditModal(
                        Number(
                            button.dataset.edit
                        )
                    )
                );
            });


        body
            .querySelectorAll(
                "[data-delete]"
            )
            .forEach(button => {

                button.addEventListener(
                    "click",
                    () => disableSchedule(
                        Number(
                            button.dataset.delete
                        )
                    )
                );
            });
    }


    function renderTrips() {

        const body =
            $("tripTableBody");

        body.innerHTML = "";

        let list =
            [...state.trips];

        const search =
            String(
                $("searchInput").value || ""
            )
                .trim()
                .toLowerCase();

        const dateFilter =
            $("dateFilter").value;

        const statusFilter =
            $("statusFilter").value;

        const today =
            new Date()
                .toISOString()
                .slice(0, 10);

        if (search) {

            list =
                list.filter(item => {

                    const text =
                        [
                            item.tripCode,
                            item.routeCode,
                            item.routeName,
                            item.busCode
                        ]
                            .join(" ")
                            .toLowerCase();

                    return text.includes(
                        search
                    );
                });
        }


        if (dateFilter === "today") {

            list =
                list.filter(
                    item =>
                        item.tripDate === today
                );
        }


        if (dateFilter === "upcoming") {

            list =
                list.filter(
                    item =>
                        item.tripDate >= today
                );
        }


        if (statusFilter !== "all") {

            list =
                list.filter(
                    item =>
                        String(
                            item.status
                        ).toUpperCase() ===
                        statusFilter
                );
        }


        $("tripEmpty")
            .classList.toggle(
                "hidden",
                list.length !== 0
            );


        list.forEach(item => {

            const row =
                document.createElement(
                    "tr"
                );

            const generated =
                item.recurringScheduleId
                    ? "Tự sinh"
                    : "Thủ công";

            row.innerHTML = `
                <td>

                    <strong>
                        ${escapeHtml(
                item.tripCode
            )}
                    </strong>

                    <div style="
                        font-size:11px;
                        opacity:.65;
                        margin-top:3px;
                    ">
                        ${generated}
                    </div>

                </td>

                <td>
                    ${escapeHtml(
                item.routeCode || "—"
            )}
                    <div style="
                        font-size:12px;
                        opacity:.7;
                    ">
                        ${escapeHtml(
                item.routeName || ""
            )}
                    </div>
                </td>

                <td>
                    ${escapeHtml(
                item.busCode || "—"
            )}
                </td>

                <td>
                    ${formatDate(
                item.tripDate
            )}
                </td>

                <td>
                    <strong>
                        ${escapeHtml(
                item.departureTime
            )}
                    </strong>
                </td>

                <td>
                    ${item.availableSeats}
                    /
                    ${item.totalSeats}
                </td>

                <td>
                    <span class="status ${String(
                item.status
            ).toUpperCase() ===
                    "CANCELLED"
                    ? "muted"
                    : "success"
                }">
                        ${escapeHtml(
                    item.status
                )}
                    </span>
                </td>

                <td>
                    <button
                        class="secondary-btn"
                        type="button"
                        data-detail="${item.id}">
                        Chi tiết
                    </button>
                </td>
            `;

            body.appendChild(row);
        });

        body.querySelectorAll("[data-detail]").forEach(button => {
            button.addEventListener("click", () => {
                const tripId = Number(button.dataset.detail);
                if (!Number.isInteger(tripId) || tripId <= 0) {
                    toast("Không xác định được chuyến xe.");
                    return;
                }

                window.location.href =
                    `../chi-tiet-chuyen/index.html?tripId=${encodeURIComponent(tripId)}`;
            });
        });
    }


    function tripStatusClass(status) {
        const value = String(status || "").toUpperCase();
        if (value === "CANCELLED") return "cancelled";
        if (value === "SCHEDULED") return "scheduled";
        return "active";
    }

    function tripStatusText(status) {
        const value = String(status || "").toUpperCase();
        const map = {
            ACTIVE: "Đang hoạt động",
            SCHEDULED: "Đã lên lịch",
            CANCELLED: "Đã hủy"
        };
        return map[value] || status || "Chưa xác định";
    }

    function formatTripDateTime(item) {
        return {
            date: formatDate(item.tripDate),
            time: String(item.departureTime || "").slice(0, 5) || "--:--"
        };
    }

    function renderTripOverview() {
        const listElement = $("overviewTripList");
        const emptyElement = $("overviewTripEmpty");

        if (!listElement) return;

        const trips = [...state.trips].sort((a, b) => {
            const aValue = `${a.tripDate || ""} ${a.departureTime || ""}`;
            const bValue = `${b.tripDate || ""} ${b.departureTime || ""}`;
            return aValue.localeCompare(bValue);
        });

        const visibleTrips = trips.slice(0, 6);
        listElement.innerHTML = "";

        if (!visibleTrips.length) {
            emptyElement.classList.remove("hidden");
            $("tripListCount").textContent = "0 chuyến";
        } else {
            emptyElement.classList.add("hidden");
            $("tripListCount").textContent = `${trips.length} chuyến`;
        }

        visibleTrips.forEach(item => {
            const row = document.createElement("div");
            row.className = "trip-list-item";

            const dateTime = formatTripDateTime(item);
            const totalSeats = Number(item.totalSeats || 0);
            const availableSeats = Number(item.availableSeats || 0);
            const bookedSeats = Math.max(0, totalSeats - availableSeats);

            row.innerHTML = `
                <div class="trip-time-box">
                    <strong>${escapeHtml(dateTime.time)}</strong>
                    <span>${escapeHtml(dateTime.date)}</span>
                </div>

                <div class="trip-route-info">
                    <span class="trip-route-code">${escapeHtml(item.routeCode || "—")}</span>
                    <div class="trip-route-name">
                        ${escapeHtml(item.routeName || "Chưa có tên tuyến")}
                    </div>
                    <div class="trip-bus-info">
                        ${escapeHtml(item.busCode || "Chưa phân xe")} ·
                        ${bookedSeats}/${totalSeats} ghế đã đặt
                    </div>
                </div>

                <div class="trip-seat-box">
                    <strong>${availableSeats}/${totalSeats} ghế</strong>
                    <span>${escapeHtml(item.tripCode || "—")}</span>
                    <span class="trip-status ${tripStatusClass(item.status)}">
                        ${escapeHtml(tripStatusText(item.status))}
                    </span>
                </div>
            `;

            listElement.appendChild(row);
        });

        const total = trips.length;
        const active = trips.filter(x =>
            String(x.status || "").toUpperCase() === "ACTIVE"
        ).length;
        const scheduled = trips.filter(x =>
            String(x.status || "").toUpperCase() === "SCHEDULED"
        ).length;
        const cancelled = trips.filter(x =>
            String(x.status || "").toUpperCase() === "CANCELLED"
        ).length;

        const booked = trips.reduce((sum, x) => {
            const totalSeats = Number(x.totalSeats || 0);
            const availableSeats = Number(x.availableSeats || 0);
            return sum + Math.max(0, totalSeats - availableSeats);
        }, 0);

        const available = trips.reduce(
            (sum, x) => sum + Number(x.availableSeats || 0),
            0
        );

        const totalSeats = booked + available;
        const occupancy = totalSeats > 0
            ? Math.round((booked / totalSeats) * 100)
            : 0;

        $("overviewTotalTrips").textContent = total;
        $("overviewActiveTrips").textContent = active;
        $("overviewScheduledTrips").textContent = scheduled;
        $("overviewCancelledTrips").textContent = cancelled;
        $("overviewOccupancy").textContent = `${occupancy}%`;
        $("overviewOccupancyBar").style.width = `${occupancy}%`;
        $("overviewBookedSeats").textContent = `${booked} ghế đã đặt`;
        $("overviewAvailableSeats").textContent = `${available} ghế còn trống`;

        $("statusActivePercent").textContent = `${total ? Math.round(active / total * 100) : 0}%`;
        $("statusScheduledPercent").textContent = `${total ? Math.round(scheduled / total * 100) : 0}%`;
        $("statusCancelledPercent").textContent = `${total ? Math.round(cancelled / total * 100) : 0}%`;

        const now = new Date();
        $("overviewDate").textContent =
            `Cập nhật ${String(now.getDate()).padStart(2, "0")}/${String(now.getMonth() + 1).padStart(2, "0")}/${now.getFullYear()}`;
    }


    function updateStats() {

        const activeRecurring =
            state.recurring.filter(
                x => x.isActive
            ).length;

        const activeBuses =
            state.buses.length;

        const activeRoutes =
            state.routes.filter(
                x =>
                    x.isActive !== false
            ).length;

        const today =
            new Date()
                .toISOString()
                .slice(0, 10);

        const futureTrips =
            state.trips.filter(
                x =>
                    x.tripDate >= today &&
                    x.isActive
            ).length;

        $("recurringCount")
            .textContent =
            activeRecurring;

        $("busCount")
            .textContent =
            activeBuses;

        $("routeCount")
            .textContent =
            activeRoutes;

        $("tripCount")
            .textContent =
            futureTrips;
    }


    function setDefaultStartDate() {

        const today =
            new Date()
                .toISOString()
                .slice(0, 10);

        $("startDate").value =
            today;
    }


    function updateDayVisibility() {

        const type =
            $("scheduleType").value;

        $("daysWrapper")
            .style.display =
            type === "WEEKLY"
                ? "block"
                : "none";
    }


    function resetForm() {

        state.editingId = 0;

        $("editingScheduleId").value =
            "0";

        $("modalTitle").textContent =
            "Tạo lịch cố định";

        $("routeSelect").value =
            "";

        $("busSelect").value =
            "";

        $("scheduleType").value =
            "DAILY";

        $("departureTime").value =
            "08:00";

        $("startDate").value =
            new Date()
                .toISOString()
                .slice(0, 10);

        $("endDate").value =
            "";

        $("isActive").checked =
            true;

        document
            .querySelectorAll(
                "#daysOfWeek input"
            )
            .forEach(
                checkbox =>
                    checkbox.checked =
                    false
            );

        updateDayVisibility();
    }


    function openCreateModal() {

        resetForm();

        renderRouteOptions();
        renderBusOptions();

        $("recurringModal")
            .classList.remove(
                "hidden"
            );
    }


    function openEditModal(id) {

        const item =
            state.recurring.find(
                x => x.id === id
            );

        if (!item)
            return;

        state.editingId =
            id;

        $("editingScheduleId")
            .value = id;

        $("modalTitle").textContent =
            "Sửa lịch cố định";

        renderRouteOptions();
        renderBusOptions();

        $("routeSelect").value =
            item.routeId;

        $("busSelect").value =
            item.busId;

        $("scheduleType").value =
            item.scheduleType;

        $("departureTime").value =
            item.departureTime;

        $("startDate").value =
            item.startDate;

        $("endDate").value =
            item.endDate || "";

        $("isActive").checked =
            item.isActive;

        const selectedDays =
            String(
                item.daysOfWeek || ""
            )
                .split(",")
                .map(x => x.trim());

        document
            .querySelectorAll(
                "#daysOfWeek input"
            )
            .forEach(
                checkbox => {

                    checkbox.checked =
                        selectedDays.includes(
                            checkbox.value
                        );
                }
            );

        updateDayVisibility();

        $("recurringModal")
            .classList.remove(
                "hidden"
            );
    }


    function closeModal() {

        $("recurringModal")
            .classList.add(
                "hidden"
            );
    }


    function getSelectedDays() {

        return [
            ...document.querySelectorAll(
                "#daysOfWeek input:checked"
            )
        ]
            .map(
                checkbox =>
                    checkbox.value
            )
            .join(",");
    }


    async function saveSchedule(event) {

        event.preventDefault();

        const scheduleType =
            $("scheduleType").value;

        let days =
            null;

        if (scheduleType === "WEEKLY") {

            days =
                getSelectedDays();

            if (!days) {

                toast(
                    "Hãy chọn ít nhất một thứ."
                );

                return;
            }
        }


        const payload = {

            routeId:
                Number(
                    $("routeSelect").value
                ),

            busId:
                Number(
                    $("busSelect").value
                ),

            scheduleType:
                scheduleType,

            daysOfWeek:
                days,

            startDate:
                $("startDate").value,

            endDate:
                $("endDate").value ||
                null,

            departureTime:
                $("departureTime").value,

            isActive:
                $("isActive").checked
        };


        if (
            !payload.routeId ||
            !payload.busId
        ) {

            toast(
                "⚠️ Hãy chọn tuyến và xe."
            );

            return;
        }


        const editingId =
            state.editingId;


        const url =
            editingId
                ? `${API.recurring}/${editingId}`
                : API.recurring;

        const method =
            editingId
                ? "PUT"
                : "POST";


        const response =
            await fetch(
                url,
                {
                    method,
                    headers:
                        authHeaders(),
                    body:
                        JSON.stringify(
                            payload
                        )
                }
            );


        const data =
            await response.json()
                .catch(
                    () => ({})
                );


        if (!response.ok) {

            toast(
                "❌ " +
                (
                    data.message ||
                    "Không thể lưu lịch."
                )
            );

            return;
        }

        toast(
            data.message ||
            "Đã lưu lịch cố định."
        );

        closeModal();

        await loadAll();
    }


    async function disableSchedule(id) {

        if (
            !confirm(
                "Ngừng lịch này? Các chuyến tương lai được sinh từ lịch sẽ bị hủy."
            )
        ) {
            return;
        }


        const response =
            await fetch(
                `${API.recurring}/${id}`,
                {
                    method:
                        "DELETE",

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

            toast(
                "❌ " +
                (
                    data.message ||
                    "Không thể ngừng lịch."
                )
            );
            return;
        }


        toast(
            data.message ||
            "Đã ngừng lịch."
        );

        await loadAll();
    }

    function setDefaultTripDate() {

        const today =
            new Date()
                .toISOString()
                .slice(0, 10);

        $("tripDate").value =
            today;
    }


    function renderTripRouteOptions() {

        const select =
            $("tripRouteSelect");

        if (!select)
            return;

        select.innerHTML = `
        <option value="">
            -- Chọn tuyến --
        </option>
    `;

        state.routes
            .filter(x =>
                x.isActive !== false
            )
            .forEach(route => {

                const option =
                    document.createElement(
                        "option"
                    );

                option.value =
                    route.id ||
                    route.routeId;

                option.textContent =
                    `${route.routeCode || ""} - ${route.routeName ||
                    route.name ||
                    ""
                    }`;

                select.appendChild(
                    option
                );
            });
    }


    function renderTripBusOptions() {

        const select =
            $("tripBusSelect");

        if (!select)
            return;

        select.innerHTML = `
        <option value="">
            -- Chọn xe --
        </option>
    `;

        state.buses.forEach(bus => {

            const option =
                document.createElement(
                    "option"
                );

            option.value =
                bus.id;

            option.textContent =
                `${bus.busCode} - ${bus.licensePlate
                } (${bus.seatCount} ghế)`;

            select.appendChild(
                option
            );
        });
    }


    function openCreateTripModal() {

        $("tripForm")
            ?.reset();

        renderTripRouteOptions();
        renderTripBusOptions();

        setDefaultTripDate();

        const currentRoute =
            state.routes.find(
                x =>
                    Number(
                        x.id ||
                        x.routeId
                    ) ===
                    Number(
                        state.selectedRouteId || 0
                    )
            );

        if (currentRoute) {

            $("tripRouteSelect").value =
                currentRoute.id ||
                currentRoute.routeId;
        }

        $("tripDepartureTime").value =
            "08:00";

        $("tripModal")
            .classList.remove(
                "hidden"
            );

        $("tripRouteSelect")
            ?.focus();
    }


    function closeTripModal() {

        $("tripModal")
            ?.classList.add(
                "hidden"
            );
    }


    async function saveManualTrip(event) {

        event.preventDefault();

        const routeId =
            Number(
                $("tripRouteSelect")
                    .value
            );

        const busId =
            Number(
                $("tripBusSelect")
                    .value
            );

        const tripDate =
            $("tripDate")
                .value;

        const departureTime =
            $("tripDepartureTime")
                .value;

        const tripCode =
            $("tripCode")
                .value
                .trim();

        const note =
            $("tripNote")
                .value
                .trim();

        if (!routeId) {

            toast(
                "❌ Vui lòng chọn tuyến."
            );

            return;
        }

        if (!busId) {

            toast(
                "❌ Vui lòng chọn xe."
            );

            return;
        }

        if (!tripDate) {

            toast(
                "❌ Vui lòng chọn ngày chạy."
            );

            return;
        }

        if (!departureTime) {

            toast(
                "❌ Vui lòng chọn giờ khởi hành."
            );

            return;
        }

        const saveBtn =
            $("saveTripBtn");

        if (saveBtn) {

            saveBtn.disabled =
                true;

            saveBtn.textContent =
                "Đang lưu...";
        }

        try {

            const response =
                await fetch(
                    API.trip,
                    {
                        method:
                            "POST",

                        headers:
                            authHeaders(),

                        body:
                            JSON.stringify({
                                RouteId:
                                    routeId,

                                BusId:
                                    busId,

                                TripDate:
                                    tripDate,

                                DepartureTime:
                                    departureTime,

                                TripCode:
                                    tripCode ||
                                    null,

                                Note:
                                    note ||
                                    null
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

                toast(
                    `❌ ${data.message ||
                    "Không thể lưu chuyến."
                    }`
                );

                return;
            }

            toast(
                data.message ||
                "✅ Tạo chuyến thành công."
            );

            closeTripModal();

            await loadAll();

        }
        catch (error) {

            console.error(error);

            toast(
                "❌ Không thể kết nối máy chủ."
            );

        }
        finally {

            if (saveBtn) {

                saveBtn.disabled =
                    false;

                saveBtn.textContent =
                    "Lưu chuyến";
            }
        }
    }
    function initEvents() {

        $("addTripBtn")
            ?.addEventListener(
                "click",
                openCreateTripModal
            );

        $("closeTripModal")
            ?.addEventListener(
                "click",
                closeTripModal
            );

        $("cancelTripModal")
            ?.addEventListener(
                "click",
                closeTripModal
            );

        $("tripForm")
            ?.addEventListener(
                "submit",
                saveManualTrip
            );

        $("tripModal")
            ?.addEventListener(
                "click",
                event => {

                    if (
                        event.target ===
                        $("tripModal")
                    ) {
                        closeTripModal();
                    }
                }
            );

        $("addRecurringBtn")
            .addEventListener(
                "click",
                openCreateModal
            );


        $("closeModal")
            .addEventListener(
                "click",
                closeModal
            );


        $("cancelModal")
            .addEventListener(
                "click",
                closeModal
            );


        $("recurringModal")
            .addEventListener(
                "click",
                event => {

                    if (
                        event.target ===
                        $("recurringModal")
                    ) {

                        closeModal();
                    }
                }
            );


        $("recurringForm")
            .addEventListener(
                "submit",
                saveSchedule
            );


        $("scheduleType")
            .addEventListener(
                "change",
                updateDayVisibility
            );


        $("searchInput")
            .addEventListener(
                "input",
                renderTrips
            );


        $("dateFilter")
            .addEventListener(
                "change",
                renderTrips
            );


        $("statusFilter")
            .addEventListener(
                "change",
                renderTrips
            );


        const logoutBtn =
            document.querySelector(
                ".logout-btn"
            );

        const logoutModal =
            $("logoutModal");

        const cancelLogoutBtn =
            $("cancelLogoutBtn");

        const confirmLogoutBtn =
            $("confirmLogoutBtn");


        logoutBtn?.addEventListener(
            "click",
            event => {

                event.preventDefault();

                logoutModal
                    ?.classList.remove(
                        "hidden"
                    );
            }
        );


        cancelLogoutBtn?.addEventListener(
            "click",
            () => {

                logoutModal
                    ?.classList.add(
                        "hidden"
                    );
            }
        );


        logoutModal?.addEventListener(
            "click",
            event => {

                if (
                    event.target ===
                    logoutModal
                ) {

                    logoutModal
                        .classList.add(
                            "hidden"
                        );
                }
            }
        );


        confirmLogoutBtn?.addEventListener(
            "click",
            () => {

                localStorage.removeItem(
                    "userSession"
                );

                sessionStorage.clear();

                window.location.href =
                    "../auth/dangnhap.html";
            }
        );


    }


    const viewAllTripsButton = $("viewAllTripsBtn");

    viewAllTripsButton?.addEventListener("click", () => {
        const target = $("tripTableBody");
        const card = target?.closest(".page-card");

        card?.scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
    });


    document.addEventListener(
        "DOMContentLoaded",
        async () => {

            if (
                !checkAuthorization()
            ) {
                return;
            }

            setDefaultStartDate();

            updateDayVisibility();

            initEvents();

            try {

                await loadAll();

            } catch (error) {

                console.error(error);

                toast(
                    "❌ " +
                    (
                        error.message ||
                        "Không thể tải dữ liệu lịch trình."
                    )
                );
            }
        }
    );

})();
(function syncScheduleTopbar() {
    "use strict";

    try {
        const raw = localStorage.getItem("userSession");
        if (!raw) return;

        const user = JSON.parse(raw);

        const fullName =
            user.fullName ||
            user.FullName ||
            user.name ||
            "Người dùng";

        const role = String(
            user.role ||
            user.Role ||
            user.roleCode ||
            user.RoleCode ||
            user.roleName ||
            user.RoleName ||
            ""
        ).trim().toUpperCase();

        const roleId = Number(
            user.roleId || user.RoleId || 0
        );

        let roleText = user.roleName || user.RoleName || "";

        if (
            role.includes("ADMIN") ||
            role.includes("QUẢN TRỊ") ||
            roleId === 1
        ) {
            roleText = "Quản trị hệ thống";
        } else if (
            role.includes("MANAGER") ||
            role.includes("QUẢN LÝ") ||
            roleId === 2
        ) {
            roleText = "Quản lý vận hành";
        } else if (role.includes("DRIVER")) {
            roleText = "Tài xế";
        } else if (role.includes("PASSENGER")) {
            roleText = "Hành khách";
        } else if (!roleText) {
            roleText = "Người dùng";
        }

        const parts = fullName.trim().split(/\s+/);

        const initials = parts.length > 1
            ? (parts[0][0] + parts[parts.length - 1][0])
                .toUpperCase()
            : fullName.substring(0, 2).toUpperCase();

        const avatar = document.getElementById("topAvatar");
        const roleElement = document.getElementById("topUserRole");

        if (avatar) {
            avatar.textContent = initials;
        }

        if (roleElement) {
            roleElement.textContent = roleText;
        }
    } catch (error) {
        console.error("Không thể cập nhật thông tin thanh tiêu đề:", error);
    }
})();