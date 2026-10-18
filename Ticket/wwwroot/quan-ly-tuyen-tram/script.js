// @ts-nocheck

let routes = [];
let selectedRouteId = null;

const $ = id => document.getElementById(id);

const money = value =>
    new Intl.NumberFormat("vi-VN")
        .format(Number(value) || 0) + " đ";


// =========================================================
// SESSION / AUTH
// =========================================================

function getCurrentUser() {
    try {
        const raw =
            localStorage.getItem("userSession");

        return raw ? JSON.parse(raw) : null;
    }
    catch {
        return null;
    }
}

function getCurrentUserId() {
    const user = getCurrentUser();
    return user?.userId || "";
}

function getAuthHeaders(includeJson = false) {
    const headers = {
        "X-User-Id": getCurrentUserId()
    };

    if (includeJson) {
        headers["Content-Type"] =
            "application/json";
    }

    return headers;
}


// =========================================================
// KIỂM TRA QUYỀN
// ADMIN + MANAGER
// =========================================================

function checkAuthAndRole() {

    const user =
        getCurrentUser();

    if (!user) {

        window.location.replace(
            "../auth/dangnhap.html"
        );

        return false;
    }

    const role =
        String(
            user.role ||
            user.roleCode ||
            user.Role ||
            user.RoleCode ||
            ""
        )
            .trim()
            .toUpperCase();

    const roleId =
        Number(
            user.roleId ||
            user.RoleId ||
            0
        );

    const isAdmin =
        role === "ADMIN" ||
        roleId === 1;

    const isManager =
        role === "MANAGER" ||
        roleId === 2;

    if (!isAdmin && !isManager) {

        window.location.replace(
            "../index.html"
        );

        return false;
    }

    let displayRoleName =
        "Quản lý vận hành";

    if (isAdmin) {

        displayRoleName =
            "Quản trị hệ thống";
    }
    else if (isManager) {

        displayRoleName =
            "Quản lý vận hành";
    }

    const userNameEl =
        document.querySelector(
            ".account-mini strong"
        );

    const userRoleEl =
        document.querySelector(
            ".account-mini small"
        );

    const avatarEl =
        document.querySelector(
            ".account-mini .avatar"
        );

    const topAvatarEl =
        document.querySelector(
            ".user-btn .avatar"
        );

    const topNameEl =
        document.querySelector(
            ".user-btn"
        );

    const fullName =
        user.fullName ||
        "Người dùng";

    const initials =
        fullName
            .split(" ")
            .filter(Boolean)
            .map(word => word[0])
            .join("")
            .slice(0, 2)
            .toUpperCase();

    if (userNameEl) {

        userNameEl.textContent =
            fullName;
    }

    if (userRoleEl) {

        userRoleEl.textContent =
            displayRoleName;
    }

    if (avatarEl) {

        avatarEl.textContent =
            initials;
    }

    if (topAvatarEl) {

        topAvatarEl.textContent =
            initials;
    }

    if (topNameEl) {

        topNameEl.innerHTML = `
            <span class="avatar small">
                ${initials}
            </span>
            ${displayRoleName} ▾
        `;
    }

    const linkPQ =
        $("linkPhanQuyen");

    if (linkPQ) {

        linkPQ.style.display =
            isAdmin
                ? "flex"
                : "none";
    }

    return true;
}


// =========================================================
// MODAL
// =========================================================

function openModal(id) {

    const modal =
        $(id);

    if (modal) {

        modal.classList.add(
            "open"
        );
    }
}

function closeModal(id) {

    const modal =
        $(id);

    if (modal) {

        modal.classList.remove(
            "open"
        );
    }
}


// =========================================================
// ESCAPE HTML
// =========================================================

function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


// =========================================================
// SINH MÃ TUYẾN R01, R02...
// =========================================================

function getNextRouteCode() {

    const usedNumbers =
        new Set();

    routes.forEach(route => {

        const code =
            String(
                route.code || ""
            );

        const match =
            code.match(/(\d+)/);

        if (match) {

            usedNumbers.add(
                Number(match[1])
            );
        }
    });

    let number = 1;

    while (
        usedNumbers.has(number)
    ) {

        number++;
    }

    return `R${String(number).padStart(2, "0")}`;
}


// =========================================================
// LOAD ROUTES
// =========================================================

async function fetchAllData() {

    try {

        const res =
            await fetch(
                "/api/TuyenXe",
                {
                    headers:
                        getAuthHeaders()
                }
            );

        if (
            res.status === 403
        ) {

            toast(
                "❌ Bạn không có quyền xem dữ liệu tuyến."
            );

            return;
        }

        if (!res.ok) {

            throw new Error(
                "Không thể tải danh sách tuyến."
            );
        }

        const apiRoutes =
            await res.json();


        routes =
            (apiRoutes || [])
                .map(
                    (r, index) => {

                        const stops =
                            Array.isArray(
                                r.routeStops
                            )
                                ? r.routeStops
                                    .slice()
                                    .sort(
                                        (a, b) =>
                                            Number(
                                                a.stopOrder
                                            ) -
                                            Number(
                                                b.stopOrder
                                            )
                                    )
                                : [];


                        const firstStop =
                            stops[0]?.busStop;

                        const lastStop =
                            stops[
                                stops.length - 1
                            ]?.busStop;


                        const first =
                            r.startPoint ||
                            firstStop?.name ||
                            "Chưa có trạm";


                        const last =
                            r.endPoint ||
                            lastStop?.name ||
                            "Chưa có trạm";


                        return {

                            id:
                                r.id,

                            code:
                                r.routeCode ||
                                `R${String(
                                    index + 1
                                ).padStart(
                                    2,
                                    "0"
                                )}`,

                            name:
                                r.routeName ||
                                `Tuyến ${index + 1}`,

                            start:
                                first,

                            end:
                                last,

                            fare:
                                Number(
                                    r.fare
                                ) || 0,

                            status:
                                r.isActive
                                    ? "Hoạt động"
                                    : "Tạm dừng",

                            totalDistanceKm:
                                Number(
                                    r.totalDistanceKm
                                ) || 0,

                            estimatedDuration:
                                r.estimatedDuration ||
                                "01:30:00",

                            stations:
                                stops.map(
                                    (rs, idx) => ({

                                        id:
                                            rs.busStop?.id ??
                                            rs.stopId,

                                        code:
                                            rs.busStop?.stopCode ||
                                            `S${String(
                                                idx + 1
                                            ).padStart(
                                                2,
                                                "0"
                                            )}`,

                                        name:
                                            rs.busStop?.name ||
                                            `Trạm ${idx + 1}`,

                                        address:
                                            rs.busStop?.address ||
                                            "",

                                        description:
                                            rs.busStop?.description ||
                                            "",

                                        order:
                                            Number(
                                                rs.stopOrder
                                            ) || idx + 1,

                                        distance:
                                            Number(
                                                rs.distanceFromStartKm
                                            ) || 0,

                                        travelTime:
                                            Number(
                                                rs.travelTimeFromStartMinutes
                                            ) || 0
                                    })
                                )
                        };
                    }
                );


        if (routes.length > 0) {

            if (
                !selectedRouteId ||
                !routes.some(
                    r =>
                        Number(r.id) ===
                        Number(
                            selectedRouteId
                        )
                )
            ) {

                selectedRouteId =
                    routes[0].id;
            }
        }
        else {

            selectedRouteId =
                null;
        }


        updateStats();

        renderRoutes();

        renderSelectedRoute();

    }
    catch (err) {

        console.error(err);

        toast(
            "❌ Không thể tải dữ liệu từ SQL Server."
        );
    }
}


// =========================================================
// THỐNG KÊ
// =========================================================

function updateStats() {

    const routeCount =
        $("routeCount");

    const activeRouteCount =
        $("activeRouteCount");

    const stationCount =
        $("stationCount");

    const avgFare =
        $("avgFare");


    if (routeCount) {

        routeCount.textContent =
            routes.length;
    }


    if (activeRouteCount) {

        activeRouteCount.textContent =
            routes.filter(
                r =>
                    r.status ===
                    "Hoạt động"
            ).length;
    }


    const stationSet =
        new Set();

    routes.forEach(
        route => {

            route.stations.forEach(
                station => {

                    if (
                        station.id != null
                    ) {

                        stationSet.add(
                            station.id
                        );
                    }
                }
            );
        }
    );


    if (stationCount) {

        stationCount.textContent =
            stationSet.size;
    }


    const totalFare =
        routes.reduce(
            (sum, route) =>
                sum +
                Number(
                    route.fare || 0
                ),
            0
        );


    const averageFare =
        routes.length
            ? Math.round(
                totalFare /
                routes.length
            )
            : 0;


    if (avgFare) {

        avgFare.textContent =
            money(
                averageFare
            );
    }
}


// =========================================================
// RENDER ROUTES
// =========================================================

function renderRoutes() {

    const tbody =
        $("routeTableBody");

    if (!tbody)
        return;


    const keyword =
        (
            $("routeSearch")
                ?.value ||
            ""
        )
            .trim()
            .toLowerCase();


    const statusFilter =
        $("routeStatusFilter")
            ?.value ||
        "all";


    const rows =
        routes.filter(
            route => {

                const text = [
                    route.code,
                    route.name,
                    route.start,
                    route.end
                ]
                    .filter(Boolean)
                    .join(" ")
                    .toLowerCase();


                const matchKeyword =
                    !keyword ||
                    text.includes(
                        keyword
                    );


                const matchStatus =
                    statusFilter ===
                    "all" ||
                    (
                        statusFilter ===
                        "Hoạt động" &&
                        route.status ===
                        "Hoạt động"
                    ) ||
                    (
                        statusFilter ===
                        "Tạm dừng" &&
                        route.status ===
                        "Tạm dừng"
                    );


                return (
                    matchKeyword &&
                    matchStatus
                );
            }
        );


    if (!rows.length) {

        tbody.innerHTML = `
            <tr>
                <td colspan="7"
                    style="
                    text-align:center;
                    padding:25px;
                    color:#8a99a5;
                    ">
                    Không có tuyến nào.
                </td>
            </tr>
        `;

        return;
    }


    tbody.innerHTML =
        rows
            .map(
                route => {

                    const selected =
                        Number(
                            route.id
                        ) ===
                        Number(
                            selectedRouteId
                        );


                    const statusClass =
                        route.status ===
                            "Hoạt động"
                            ? "active"
                            : "paused";


                    return `
                        <tr
                            class="${selected
                            ? "selected"
                            : ""
                        }"
                            data-route="${route.id
                        }">

                            <td>
                                <span class="route-code">
                                    ${escapeHtml(
                            route.code
                        )}
                                </span>
                            </td>

                            <td>
                                <strong>
                                    ${escapeHtml(
                            route.name
                        )}
                                </strong>
                            </td>

                            <td>
                                ${escapeHtml(
                            route.start
                        )}
                            </td>

                            <td>
                                ${escapeHtml(
                            route.end
                        )}
                            </td>

                            <td>
                                <strong>
                                    ${money(
                            route.fare
                        )}
                                </strong>

                                <small
                                    style="
                                    color:#6c757d;
                                    display:block;
                                    ">
                                    ${route.totalDistanceKm
                            ? `${route.totalDistanceKm} km`
                            : ""
                        }
                                </small>
                            </td>

                            <td>
                                <span
                                    class="status ${statusClass
                        }">
                                    ${route.status
                        }
                                </span>
                            </td>

                            <td>
                                <div
                                    class="row-actions">

                                    <button
                                        class="mini-btn edit-route"
                                        data-id="${route.id
                        }"
                                        title="Sửa tuyến">
                                        ✎
                                    </button>

                                    <button
                                        class="mini-btn delete delete-route"
                                        data-id="${route.id
                        }"
                                        title="Xóa / vô hiệu hóa tuyến">
                                        ×
                                    </button>

                                </div>
                            </td>

                        </tr>
                    `;
                }
            )
            .join("");


    document
        .querySelectorAll(
            "tr[data-route]"
        )
        .forEach(
            row => {

                row.addEventListener(
                    "click",
                    event => {

                        if (
                            event.target.closest(
                                ".row-actions"
                            )
                        ) {
                            return;
                        }

                        selectedRouteId =
                            Number(
                                row.dataset.route
                            );

                        renderRoutes();

                        renderSelectedRoute();
                    }
                );
            }
        );


    document
        .querySelectorAll(
            ".edit-route"
        )
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    event => {

                        event.stopPropagation();

                        openRouteModal(
                            Number(
                                button.dataset.id
                            )
                        );
                    }
                );
            }
        );


    document
        .querySelectorAll(
            ".delete-route"
        )
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    event => {

                        event.stopPropagation();

                        deleteRoute(
                            Number(
                                button.dataset.id
                            )
                        );
                    }
                );
            }
        );
}


// =========================================================
// RENDER SELECTED ROUTE
// =========================================================

function renderSelectedRoute() {

    const route =
        routes.find(
            r =>
                Number(r.id) ===
                Number(selectedRouteId)
        );


    if (!route) {

        if ($("selectedRouteTitle")) {

            $("selectedRouteTitle")
                .textContent =
                "Chưa chọn tuyến";
        }


        if ($("selectedRouteMeta")) {

            $("selectedRouteMeta")
                .textContent =
                "Chọn tuyến từ bảng danh sách bên trên.";
        }


        if ($("detailCode")) {

            $("detailCode")
                .textContent =
                "—";
        }


        if ($("detailFare")) {

            $("detailFare")
                .textContent =
                "0 đ";
        }


        if ($("detailStationCount")) {

            $("detailStationCount")
                .textContent =
                "0";
        }


        if ($("detailStatus")) {

            $("detailStatus")
                .textContent =
                "—";

            $("detailStatus")
                .className =
                "status muted";
        }


        if ($("stationList")) {

            $("stationList")
                .innerHTML = `
                    <div
                        style="
                        text-align:center;
                        padding:20px;
                        color:#8a99a5;
                        ">
                        Chưa chọn tuyến nào.
                    </div>
                `;
        }

        return;
    }


    $("selectedRouteTitle")
        .textContent =
        `${route.name} — ${route.start} → ${route.end}`;


    $("selectedRouteMeta")
        .textContent =
        `Tổng chiều dài: ${route.totalDistanceKm
        } km · ${route.stations.length
        } trạm dừng.`;


    $("detailCode")
        .textContent =
        route.code;


    $("detailFare")
        .textContent =
        money(
            route.fare
        );


    $("detailStationCount")
        .textContent =
        route.stations.length;


    $("detailStatus")
        .textContent =
        route.status;


    $("detailStatus")
        .className =
        `status ${route.status ===
            "Hoạt động"
            ? "active"
            : "paused"
        }`;


    const sortedStations =
        route.stations
            .slice()
            .sort(
                (a, b) =>
                    a.order -
                    b.order
            );


    if (!$("stationList"))
        return;


    $("stationList").innerHTML =
        sortedStations.length
            ? sortedStations
                .map(
                    (station, index) => {

                        let distanceFromPrevious =
                            0;


                        if (index > 0) {

                            distanceFromPrevious =
                                Math.max(
                                    0,
                                    station.distance -
                                    sortedStations[
                                        index - 1
                                    ].distance
                                ).toFixed(
                                    1
                                );
                        }


                        const stepText =
                            index === 0
                                ? "Trạm xuất phát (0 km)"
                                : `+${distanceFromPrevious
                                } km từ trạm trước (${station.distance
                                } km từ đầu)`;


                        return `
                            <div
                                class="station-item">

                                <div
                                    class="station-main">

                                    <div
                                        class="station-number">

                                        ${station.order
                            }

                                    </div>

                                    <div>

                                        <strong>
                                            ${escapeHtml(
                                station.code ||
                                ""
                            )
                            }

                                            ${station.code
                                ? " - "
                                : ""
                            }

                                            ${escapeHtml(
                                station.name
                            )
                            }
                                        </strong>

                                        <small>
                                            ${escapeHtml(
                                station.address ||
                                ""
                            )
                            }
                                        </small>

                                        ${station.description
                                ? `
                                                    <small>
                                                        ${escapeHtml(
                                    station.description
                                )
                                }
                                                    </small>
                                                  `
                                : ""
                            }

                                    </div>

                                </div>

                                <div
                                    class="station-meta">

                                    <span>
                                        ↔
                                        <b>
                                            ${stepText
                            }
                                        </b>
                                    </span>

                                    <div
                                        class="station-actions">

                                        <button
                                            class="mini-btn edit-station"
                                            data-id="${station.id
                            }"
                                            title="Sửa trạm">

                                            ✎

                                        </button>

                                        <button
                                            class="mini-btn delete delete-station"
                                            data-id="${station.id
                            }"
                                            title="Xóa trạm khỏi tuyến">

                                            ×

                                        </button>

                                    </div>

                                </div>

                            </div>
                        `;
                    }
                )
                .join("")
            : `
                <div
                    style="
                    text-align:center;
                    padding:20px;
                    color:#8a99a5;
                    ">
                    Tuyến chưa có trạm.
                </div>
            `;


    document
        .querySelectorAll(
            ".delete-station"
        )
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    event => {

                        event.stopPropagation();

                        deleteStation(
                            Number(
                                button.dataset.id
                            )
                        );
                    }
                );
            }
        );


    document
        .querySelectorAll(
            ".edit-station"
        )
        .forEach(
            button => {

                button.addEventListener(
                    "click",
                    event => {

                        event.stopPropagation();

                        openEditStationModal(
                            Number(
                                button.dataset.id
                            )
                        );
                    }
                );
            }
        );
}


// =========================================================
// ROUTE MODAL
// =========================================================

function openRouteModal(
    id = null
) {

    $("routeForm")
        ?.reset();


    if ($("routeId")) {

        $("routeId")
            .value =
            id
                ? id
                : "";
    }


    if ($("routeModalTitle")) {

        $("routeModalTitle")
            .textContent =
            id
                ? "Chỉnh sửa tuyến"
                : "Thêm tuyến";
    }


    if (id) {

        const route =
            routes.find(
                r =>
                    Number(r.id) ===
                    Number(id)
            );


        if (!route)
            return;


        $("routeCode")
            .value =
            route.code;


        $("routeName")
            .value =
            route.name;


        $("routeStart")
            .value =
            route.start;


        $("routeEnd")
            .value =
            route.end;


        $("routeFare")
            .value =
            route.fare;


        if ($("routeTotalDistance")) {

            $("routeTotalDistance")
                .value =
                route.totalDistanceKm;
        }


        $("routeStatus")
            .value =
            route.status;

    }
    else {

        const nextCode =
            getNextRouteCode();


        $("routeCode")
            .value =
            nextCode;


        const number =
            nextCode.replace(
                /\D/g,
                ""
            );


        $("routeName")
            .value =
            `Tuyến ${number}`;


        $("routeStart")
            .value =
            "";


        $("routeEnd")
            .value =
            "";


        if ($("routeFare")) {

            $("routeFare")
                .value =
                15000;
        }


        if ($("routeTotalDistance")) {

            $("routeTotalDistance")
                .value =
                25;
        }


        $("routeStatus")
            .value =
            "Hoạt động";
    }


    openModal(
        "routeModal"
    );


    $("routeStart")
        ?.focus();
}


// =========================================================
// STATION MODAL
// =========================================================

function openStationModal() {

    const route =
        routes.find(
            r =>
                Number(r.id) ===
                Number(selectedRouteId)
        );


    if (!route) {

        toast(
            "❌ Vui lòng chọn tuyến xe trước."
        );

        return;
    }


    $("stationForm")
        ?.reset();


    if ($("stationId")) {

        $("stationId")
            .value =
            "";
    }


    if ($("stationModalTitle")) {

        $("stationModalTitle")
            .textContent =
            `Thêm trạm vào ${route.name}`;
    }


    if ($("stationCode")) {

        $("stationCode")
            .value =
            "";
    }


    $("stationOrder")
        .value =
        route.stations.length + 1;


    if ($("stationAddress")) {

        $("stationAddress")
            .value =
            "";
    }


    if ($("stationDescription")) {

        $("stationDescription")
            .value =
            "";
    }


    const sorted =
        route.stations
            .slice()
            .sort(
                (a, b) =>
                    a.order -
                    b.order
            );


    const refSelect =
        $("stationRefStop");


    if (refSelect) {

        let options = `
            <option
                value="0"
                data-dist="0">
                Trạm xuất phát đầu tiên (Mốc 0 km)
            </option>
        `;


        sorted.forEach(
            station => {

                options += `
                    <option
                        value="${station.id}"
                        data-dist="${station.distance}">

                        ${station.order}.
                        ${escapeHtml(
                    station.name
                )}

                        (
                        ${station.distance}
                        km từ đầu
                        )

                    </option>
                `;
            }
        );


        refSelect.innerHTML =
            options;


        if (sorted.length) {

            refSelect.value =
                sorted[
                    sorted.length - 1
                ].id;
        }
    }


    $("stationDistance")
        .value =
        "2.0";


    openModal(
        "stationModal"
    );


    $("stationCode")
        ?.focus();
}


// =========================================================
// EDIT STATION
// =========================================================

function openEditStationModal(
    stopId
) {

    const route =
        routes.find(
            r =>
                Number(r.id) ===
                Number(selectedRouteId)
        );


    if (!route) {

        toast(
            "❌ Chưa chọn tuyến."
        );

        return;
    }


    const station =
        route.stations.find(
            s =>
                Number(s.id) ===
                Number(stopId)
        );


    if (!station) {

        toast(
            "❌ Không tìm thấy trạm."
        );

        return;
    }


    $("stationForm")
        ?.reset();


    if ($("stationId")) {

        $("stationId")
            .value =
            station.id;
    }


    if ($("stationModalTitle")) {

        $("stationModalTitle")
            .textContent =
            "Chỉnh sửa trạm";
    }


    $("stationCode")
        .value =
        station.code || "";


    $("stationName")
        .value =
        station.name || "";


    $("stationAddress")
        .value =
        station.address || "";


    $("stationDescription")
        .value =
        station.description || "";


    $("stationOrder")
        .value =
        station.order || 1;


    $("stationDistance")
        .value =
        "0";


    const refSelect =
        $("stationRefStop");


    if (refSelect) {

        refSelect.innerHTML = `
            <option value="0">
                Không thay đổi khoảng cách
            </option>
        `;
    }


    openModal(
        "stationModal"
    );


    $("stationCode")
        ?.focus();
}


// =========================================================
// SAVE ROUTE
// =========================================================

$("routeForm")
    ?.addEventListener(
        "submit",
        async event => {

            event.preventDefault();


            const id =
                $("routeId")
                    .value
                    .trim();


            const routeCode =
                $("routeCode")
                    .value
                    .trim()
                    .toUpperCase();


            const routeName =
                $("routeName")
                    .value
                    .trim();


            const startPoint =
                $("routeStart")
                    .value
                    .trim();


            const endPoint =
                $("routeEnd")
                    .value
                    .trim();


            const fare =
                Number(
                    $("routeFare")
                        .value
                );


            const totalDistanceKm =
                Number(
                    $("routeTotalDistance")
                        ?.value ||
                    0
                );


            const isActive =
                $("routeStatus")
                    .value ===
                "Hoạt động";


            if (!routeCode) {

                toast(
                    "❌ Mã tuyến không được để trống."
                );

                return;
            }


            if (!routeName) {

                toast(
                    "❌ Tên tuyến không được để trống."
                );

                return;
            }


            if (!startPoint) {

                toast(
                    "❌ Điểm đầu không được để trống."
                );

                return;
            }


            if (!endPoint) {

                toast(
                    "❌ Điểm cuối không được để trống."
                );

                return;
            }


            if (
                Number.isNaN(fare) ||
                fare < 0
            ) {

                toast(
                    "❌ Giá vé không hợp lệ."
                );

                return;
            }


            if (
                Number.isNaN(
                    totalDistanceKm
                ) ||
                totalDistanceKm < 0
            ) {

                toast(
                    "❌ Tổng chiều dài tuyến không hợp lệ."
                );

                return;
            }


            const payload = {

                RouteCode:
                    routeCode,

                RouteName:
                    routeName,

                StartPoint:
                    startPoint,

                EndPoint:
                    endPoint,

                Fare:
                    fare,

                TotalDistanceKm:
                    totalDistanceKm,

                EstimatedDuration:
                    "01:30:00",

                IsActive:
                    isActive
            };


            try {

                const response =
                    await fetch(
                        id
                            ? `/api/TuyenXe/${id}`
                            : "/api/TuyenXe",
                        {
                            method:
                                id
                                    ? "PUT"
                                    : "POST",

                            headers:
                                getAuthHeaders(
                                    true
                                ),

                            body:
                                JSON.stringify(
                                    payload
                                )
                        }
                    );


                const body =
                    await response
                        .json()
                        .catch(
                            () => ({})
                        );


                if (
                    response.status ===
                    403
                ) {

                    toast(
                        "❌ Bạn không có quyền thực hiện thao tác này."
                    );

                    return;
                }


                if (!response.ok) {

                    toast(
                        `❌ ${body.message ||
                        "Không thể lưu tuyến."
                        }`
                    );

                    return;
                }


                toast(
                    id
                        ? "✅ Cập nhật tuyến thành công."
                        : "✅ Tạo tuyến thành công."
                );


                closeModal(
                    "routeModal"
                );


                await fetchAllData();

            }
            catch (error) {

                console.error(error);

                toast(
                    "❌ Không thể kết nối máy chủ."
                );
            }
        }
    );


// =========================================================
// SAVE STATION
// =========================================================

$("stationForm")
    ?.addEventListener(
        "submit",
        async event => {

            event.preventDefault();


            const route =
                routes.find(
                    r =>
                        Number(r.id) ===
                        Number(
                            selectedRouteId
                        )
                );


            if (!route) {

                toast(
                    "❌ Chưa chọn tuyến."
                );

                return;
            }


            const stationId =
                $("stationId")
                    ?.value
                    .trim() ||
                "";


            const stopCode =
                $("stationCode")
                    .value
                    .trim()
                    .toUpperCase();


            const name =
                $("stationName")
                    .value
                    .trim();


            const address =
                $("stationAddress")
                    .value
                    .trim();


            const description =
                $("stationDescription")
                    ?.value
                    .trim() ||
                "";


            const order =
                Number(
                    $("stationOrder")
                        .value
                ) || 1;


            const distanceBetween =
                Number(
                    $("stationDistance")
                        .value
                ) || 0;


            if (!stopCode) {

                toast(
                    "❌ Mã trạm không được để trống."
                );

                return;
            }


            if (!name) {

                toast(
                    "❌ Tên trạm không được để trống."
                );

                return;
            }


            if (!address) {

                toast(
                    "❌ Địa chỉ trạm không được để trống."
                );

                return;
            }


            // =================================================
            // EDIT
            // =================================================

            if (stationId) {

                try {

                    const response =
                        await fetch(
                            `/api/BusStops/${stationId}`,
                            {
                                method:
                                    "PUT",

                                headers:
                                    getAuthHeaders(
                                        true
                                    ),

                                body:
                                    JSON.stringify({

                                        Id:
                                            Number(
                                                stationId
                                            ),

                                        StopCode:
                                            stopCode,

                                        Name:
                                            name,

                                        Address:
                                            address,

                                        Description:
                                            description,

                                        Latitude:
                                            null,

                                        Longitude:
                                            null,

                                        IsActive:
                                            true
                                    })
                            }
                        );


                    const body =
                        await response
                            .json()
                            .catch(
                                () => ({})
                            );


                    if (
                        response.status ===
                        403
                    ) {

                        toast(
                            "❌ Bạn không có quyền sửa trạm."
                        );

                        return;
                    }


                    if (!response.ok) {

                        toast(
                            `❌ ${body.message ||
                            "Không thể sửa trạm."
                            }`
                        );

                        return;
                    }


                    toast(
                        "✅ Cập nhật trạm thành công."
                    );


                    closeModal(
                        "stationModal"
                    );


                    await fetchAllData();

                }
                catch (error) {

                    console.error(
                        error
                    );

                    toast(
                        "❌ Lỗi kết nối máy chủ."
                    );
                }

                return;
            }


            // =================================================
            // CREATE
            // =================================================

            const refSelect =
                $("stationRefStop");


            let baseDistance =
                0;


            if (
                refSelect &&
                refSelect.selectedIndex >= 0
            ) {

                const option =
                    refSelect.options[
                    refSelect.selectedIndex
                    ];


                baseDistance =
                    Number(
                        option.dataset.dist
                    ) || 0;
            }


            const finalDistance =
                Number(
                    (
                        baseDistance +
                        distanceBetween
                    ).toFixed(2)
                );


            const travelTime =
                Math.max(
                    1,
                    Math.round(
                        finalDistance *
                        1.5
                    )
                );


            const payload = {

                RouteId:
                    route.id,

                StopCode:
                    stopCode,

                StopName:
                    name,

                StopAddress:
                    address,

                StopDescription:
                    description,

                StopOrder:
                    order,

                DistanceKm:
                    finalDistance,

                TravelTimeFromStartMinutes:
                    travelTime
            };


            try {

                const response =
                    await fetch(
                        "/api/TuyenXe/them-tram-vao-tuyen",
                        {
                            method:
                                "POST",

                            headers:
                                getAuthHeaders(
                                    true
                                ),

                            body:
                                JSON.stringify(
                                    payload
                                )
                        }
                    );


                const body =
                    await response
                        .json()
                        .catch(
                            () => ({})
                        );


                if (
                    response.status ===
                    403
                ) {

                    toast(
                        "❌ Bạn không có quyền thêm trạm."
                    );

                    return;
                }


                if (!response.ok) {

                    toast(
                        `❌ ${body.message ||
                        "Không thể thêm trạm."
                        }`
                    );

                    return;
                }


                toast(
                    `✅ Đã thêm trạm ${stopCode} - ${name}.`
                );


                closeModal(
                    "stationModal"
                );


                await fetchAllData();

            }
            catch (error) {

                console.error(
                    error
                );

                toast(
                    "❌ Không thể kết nối máy chủ."
                );
            }
        }
    );

// =========================================================
// CREATE TRIP
// =========================================================

async function loadTripBuses() {

    const response =
        await fetch(
            "/api/LichTrinh/buses",
            {
                headers:
                    getAuthHeaders()
            }
        );

    if (!response.ok)
        throw new Error(
            "Không thể tải danh sách xe."
        );

    return await response.json();
}


function renderTripRouteOptions() {

    const select =
        $("tripRoute");

    if (!select)
        return;

    select.innerHTML = `
        <option value="">
            -- Chọn tuyến --
        </option>
    `;

    routes
        .filter(route =>
            route.status === "Hoạt động"
        )
        .forEach(route => {

            const option =
                document.createElement(
                    "option"
                );

            option.value =
                route.id;

            option.textContent =
                `${route.code} - ${route.name}`;

            select.appendChild(
                option
            );
        });
}


async function openTripModal() {

    if (!selectedRouteId) {

        toast(
            "❌ Vui lòng chọn tuyến trước."
        );

        return;
    }

    $("tripForm")
        ?.reset();

    renderTripRouteOptions();

    $("tripRoute").value =
        selectedRouteId;

    const buses =
        await loadTripBuses();

    const busSelect =
        $("tripBus");

    busSelect.innerHTML = `
        <option value="">
            -- Chọn xe --
        </option>
    `;

    buses.forEach(bus => {

        const option =
            document.createElement(
                "option"
            );

        option.value =
            bus.id;

        option.textContent =
            `${bus.busCode} - ${bus.licensePlate
            } (${bus.seatCount} ghế)`;

        busSelect.appendChild(
            option
        );
    });

    $("tripDate").value =
        new Date()
            .toISOString()
            .slice(0, 10);

    $("tripDepartureTime").value =
        "08:00";

    openModal(
        "tripModal"
    );
}


async function saveTrip(event) {

    event.preventDefault();

    const routeId =
        Number(
            $("tripRoute").value
        );

    const busId =
        Number(
            $("tripBus").value
        );

    const tripDate =
        $("tripDate").value;

    const departureTime =
        $("tripDepartureTime").value;

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
                "/api/LichTrinh/trip",
                {
                    method:
                        "POST",

                    headers:
                        getAuthHeaders(
                            true
                        ),

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
            "✅ Đã tạo chuyến."
        );

        closeModal(
            "tripModal"
        );

        await fetchAllData();

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
// =========================================================
// DELETE ROUTE
// =========================================================

async function deleteRoute(
    id
) {

    const route =
        routes.find(
            r =>
                Number(r.id) ===
                Number(id)
        );


    if (!route)
        return;


    if (!(await SmartbusDialog.confirm(
        `Bạn có chắc muốn xóa tuyến "${route.code} - ${route.name}"?`,
        { title: "Xác nhận xóa tuyến", confirmText: "Xóa tuyến", danger: true }
    ))) {

        return;
    }


    try {

        const response =
            await fetch(
                `/api/TuyenXe/${id}`,
                {
                    method:
                        "DELETE",

                    headers:
                        getAuthHeaders()
                }
            );


        const body =
            await response
                .json()
                .catch(
                    () => ({})
                );


        if (
            response.status ===
            403
        ) {

            toast(
                "❌ Bạn không có quyền xóa tuyến."
            );

            return;
        }


        if (!response.ok) {

            toast(
                `❌ ${body.message ||
                "Không thể xóa tuyến."
                }`
            );

            return;
        }


        toast(
            `✅ ${body.message ||
            "Đã xử lý tuyến thành công."
            }`
        );


        if (
            Number(
                selectedRouteId
            ) ===
            Number(id)
        ) {

            selectedRouteId =
                null;
        }


        await fetchAllData();

    }
    catch (error) {

        console.error(error);

        toast(
            "❌ Không thể kết nối máy chủ."
        );
    }
}


// =========================================================
// DELETE STATION FROM ROUTE
// =========================================================

async function deleteStation(
    stopId
) {

    if (!selectedRouteId) {

        toast(
            "❌ Chưa chọn tuyến."
        );

        return;
    }


    if (!(await SmartbusDialog.confirm(
        "Bạn có chắc chắn muốn xóa trạm này khỏi tuyến?",
        { title: "Xác nhận xóa trạm", confirmText: "Xóa trạm", danger: true }
    ))) {

        return;
    }


    try {

        const response =
            await fetch(
                `/api/TuyenXe/xoa-tram-khoi-tuyen?routeId=${selectedRouteId}&stopId=${stopId}`,
                {
                    method:
                        "DELETE",

                    headers:
                        getAuthHeaders()
                }
            );


        const body =
            await response
                .json()
                .catch(
                    () => ({})
                );


        if (
            response.status ===
            403
        ) {

            toast(
                "❌ Bạn không có quyền xóa trạm."
            );

            return;
        }


        if (!response.ok) {

            toast(
                `❌ ${body.message ||
                "Không thể xóa trạm."
                }`
            );

            return;
        }


        toast(
            `✅ ${body.message ||
            "Đã xóa trạm khỏi tuyến."
            }`
        );


        await fetchAllData();

    }
    catch (error) {

        console.error(error);

        toast(
            "❌ Không thể kết nối máy chủ."
        );
    }
}


// =========================================================
// SEARCH / FILTER
// =========================================================

$("routeSearch")
    ?.addEventListener(
        "input",
        renderRoutes
    );


$("routeStatusFilter")
    ?.addEventListener(
        "change",
        renderRoutes
    );


// =========================================================
// BUTTONS
// =========================================================

$("addRouteBtn")
    ?.addEventListener(
        "click",
        () =>
            openRouteModal()
    );


$("addStationBtn")
    ?.addEventListener(
        "click",
        () =>
            openStationModal()
    );

$("addTripBtn")
    ?.addEventListener(
        "click",
        () =>
            openTripModal()
    );

$("tripForm")
    ?.addEventListener(
        "submit",
        saveTrip
    );

// =========================================================
// MODAL CLOSE
// =========================================================

document
    .querySelectorAll(
        "[data-close]"
    )
    .forEach(
        button => {

            button.addEventListener(
                "click",
                () =>
                    closeModal(
                        button.dataset.close
                    )
            );
        }
    );


document
    .querySelectorAll(
        ".modal-backdrop"
    )
    .forEach(
        backdrop => {

            backdrop.addEventListener(
                "click",
                event => {

                    if (
                        event.target ===
                        backdrop
                    ) {

                        closeModal(
                            backdrop.id
                        );
                    }
                }
            );
        }
    );


// =========================================================
// LOGOUT
// =========================================================

const logoutBtn =
    $("logoutBtn");


if (logoutBtn) {

    logoutBtn.addEventListener(
        "click",
        event => {

            event.preventDefault();

            openModal(
                "logoutModal"
            );
        }
    );
}


$("cancelLogoutBtn")
    ?.addEventListener(
        "click",
        () =>
            closeModal(
                "logoutModal"
            )
    );


$("confirmLogoutBtn")
    ?.addEventListener(
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


// =========================================================
// INIT
// =========================================================

document.addEventListener(
    "DOMContentLoaded",
    async () => {

        const allowed =
            checkAuthAndRole();

        if (!allowed)
            return;

        await fetchAllData();
    }
);
