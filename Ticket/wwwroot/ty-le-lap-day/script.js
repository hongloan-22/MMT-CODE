(() => {
    "use strict";

    const $ = id => document.getElementById(id);
    const PAGE_SIZE = 20;

    const state = {
        page: 1,
        totalPages: 1,
        totalCount: 0,
        rows: [],
        loading: false
    };

    function getSession() {
        try {
            const raw = localStorage.getItem("userSession");
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    }

    function getUserId() {
        const user = getSession();
        return user?.userId || user?.UserId || "";
    }

    function requestHeaders() {
        return { "X-User-Id": getUserId() };
    }

    function valueOf(object, ...keys) {
        for (const key of keys) {
            if (object &&
                object[key] !== undefined &&
                object[key] !== null) {
                return object[key];
            }
        }
        return null;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function formatNumber(value) {
        return new Intl.NumberFormat("vi-VN", {
            maximumFractionDigits: 0
        }).format(Number(value || 0));
    }

    function formatPercent(value) {
        const number = Number(value || 0);

        return `${number.toLocaleString("vi-VN", {
            minimumFractionDigits: 0,
            maximumFractionDigits: 2
        })}%`;
    }

    function formatDateTime(value) {
        if (!value) return "—";

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return escapeHtml(value);
        }

        return date.toLocaleString("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    function localDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");

        return `${year}-${month}-${day}`;
    }

    function toast(message) {
        const el = $("toast");
        if (!el) return;

        el.textContent = message;
        el.classList.add("show");

        clearTimeout(window.__occupancyToastTimer);

        window.__occupancyToastTimer = setTimeout(
            () => el.classList.remove("show"),
            3000
        );
    }

    async function fetchJson(url) {
        const response = await fetch(url, {
            method: "GET",
            headers: requestHeaders(),
            cache: "no-store"
        });

        const data = await response.json().catch(() => null);

        if (!response.ok) {
            const message =
                valueOf(data, "message", "Message") ||
                `Yêu cầu thất bại (HTTP ${response.status}).`;

            throw new Error(message);
        }

        return data;
    }

    function normalizeTrip(trip) {
        return {
            tripId: valueOf(trip, "tripId", "TripId"),
            tripCode: valueOf(trip, "tripCode", "TripCode") || "",
            routeCode: valueOf(trip, "routeCode", "RouteCode") || "",
            routeName: valueOf(trip, "routeName", "RouteName") || "",
            originStopName:
                valueOf(trip, "originStopName", "OriginStopName") || "",
            destinationStopName:
                valueOf(trip, "destinationStopName", "DestinationStopName") || "",
            departureDateTime:
                valueOf(trip, "departureDateTime", "DepartureDateTime"),
            totalSeats:
                valueOf(trip, "totalSeats", "TotalSeats"),
            availableSeats:
                valueOf(trip, "availableSeats", "AvailableSeats")
        };
    }

    function normalizeOccupancy(occupancy) {
        return {
            totalSeats: Number(
                valueOf(occupancy, "totalSeats", "TotalSeats") ?? 0
            ),
            bookedSeats: Number(
                valueOf(occupancy, "bookedSeats", "BookedSeats") ?? 0
            ),
            heldSeats: Number(
                valueOf(occupancy, "heldSeats", "HeldSeats") ?? 0
            ),
            availableSeats: Number(
                valueOf(occupancy, "availableSeats", "AvailableSeats") ?? 0
            ),
            occupancyRate: Number(
                valueOf(occupancy, "occupancyRate", "OccupancyRate") ?? 0
            )
        };
    }

    // Tải danh sách chuyến rồi lấy số liệu tỷ lệ lấp đầy từng chuyến.
    async function loadTrips(page = state.page) {
        if (state.loading) return;

        state.loading = true;
        state.page = page;

        $("applyFilterBtn").disabled = true;
        $("refreshBtn").disabled = true;
        $("loadSummary").textContent = "Đang tải dữ liệu từ API...";

        $("occupancyTableBody").innerHTML = `
            <tr>
                <td colspan="10">
                    <div class="table-message">
                        <span class="loading-spinner"></span>
                        Đang tải dữ liệu chuyến xe...
                    </div>
                </td>
            </tr>`;

        $("errorBanner").hidden = true;
        $("errorBanner").textContent = "";

        try {
            const params = new URLSearchParams({
                page: String(state.page),
                pageSize: String(PAGE_SIZE),
                sortBy: "time",
                sortOrder: "asc"
            });

            const selectedDate = $("departureDate").value;

            if (selectedDate) {
                params.set("ngay", selectedDate);
            }

            const listResult = await fetchJson(
                `/api/TraCuuTuyenXe/danh-sach?${params.toString()}`
            );

            const trips =
                valueOf(listResult, "items", "Items") || [];

            state.totalCount = Number(
                valueOf(listResult, "totalCount", "TotalCount")
                ?? trips.length
            );

            state.totalPages = Math.max(
                1,
                Number(
                    valueOf(listResult, "totalPages", "TotalPages") ?? 1
                )
            );

            if (state.page > state.totalPages) {
                state.page = state.totalPages;
            }

            // Mỗi trang tối đa 20 chuyến để giới hạn số request.
            state.rows = await Promise.all(
                trips.map(async rawTrip => {
                    const trip = normalizeTrip(rawTrip);

                    if (!trip.tripId) {
                        return {
                            ...trip,
                            occupancyError:
                                "Không có TripId trong dữ liệu API."
                        };
                    }

                    try {
                        const occupancy = await fetchJson(
                            `/api/Statistics/trip/${encodeURIComponent(
                                trip.tripId
                            )}/occupancy`
                        );

                        return {
                            ...trip,
                            ...normalizeOccupancy(occupancy),
                            tripCode:
                                valueOf(
                                    occupancy,
                                    "tripCode",
                                    "TripCode"
                                ) || trip.tripCode,
                            occupancyError: ""
                        };
                    } catch (error) {
                        return {
                            ...trip,
                            occupancyError:
                                error.message ||
                                "Không tải được số liệu tỷ lệ lấp đầy."
                        };
                    }
                })
            );

            render();
        } catch (error) {
            state.rows = [];
            state.totalCount = 0;
            state.totalPages = 1;

            $("occupancyTableBody").innerHTML = `
                <tr>
                    <td colspan="10">
                        <div class="table-message">
                            <strong>Không thể tải dữ liệu</strong>
                            <span>${escapeHtml(
                error.message || "Lỗi khi gọi API."
            )}</span>
                        </div>
                    </td>
                </tr>`;

            renderKpis([]);
            renderDistribution([]);

            $("loadSummary").textContent = "Tải dữ liệu thất bại.";
            $("errorBanner").textContent =
                error.message || "Không thể kết nối API.";
            $("errorBanner").hidden = false;

            toast("Không tải được danh sách chuyến xe.");
        } finally {
            state.loading = false;

            $("applyFilterBtn").disabled = false;
            $("refreshBtn").disabled = false;
            $("prevPageBtn").disabled = state.page <= 1;
            $("nextPageBtn").disabled = state.page >= state.totalPages;
        }
    }

    // Phân nhóm chuyến theo tỷ lệ lấp đầy.
    function getBand(row) {
        if (row.occupancyError) return "error";
        if (!Number(row.totalSeats)) return "nodata";
        if (Number(row.availableSeats) <= 0) return "full";
        if (Number(row.occupancyRate) < 40) return "low";
        if (Number(row.occupancyRate) < 75) return "medium";

        return "high";
    }

    function bandLabel(band) {
        return ({
            low: "Thấp",
            medium: "Trung bình",
            high: "Cao",
            full: "Hết chỗ",
            error: "Lỗi dữ liệu",
            nodata: "Chưa có cấu hình"
        })[band] || "—";
    }

    // Tìm kiếm và lọc trong trang hiện tại.
    function filteredRows() {
        const search = $("searchInput")
            .value.trim().toLocaleLowerCase("vi-VN");

        const bandFilter = $("rateFilter").value;

        return state.rows.filter(row => {
            const searchable = [
                row.tripCode,
                row.routeCode,
                row.routeName,
                row.originStopName,
                row.destinationStopName
            ].join(" ").toLocaleLowerCase("vi-VN");

            if (search && !searchable.includes(search)) {
                return false;
            }

            if (bandFilter !== "all" &&
                getBand(row) !== bandFilter) {
                return false;
            }

            return true;
        });
    }

    function renderKpis(rows) {
        const validRows = rows.filter(
            row => !row.occupancyError && Number(row.totalSeats) > 0
        );

        $("kpiTrips").textContent = formatNumber(rows.length);

        if (!validRows.length) {
            [
                "kpiTotalSeats",
                "kpiBookedSeats",
                "kpiHeldSeats",
                "kpiAvailableSeats",
                "kpiOccupancyRate"
            ].forEach(id => {
                $(id).textContent = rows.length ? "—" : "0";
            });

            return;
        }

        const totals = validRows.reduce((acc, row) => {
            acc.total += Number(row.totalSeats || 0);
            acc.booked += Number(row.bookedSeats || 0);
            acc.held += Number(row.heldSeats || 0);
            acc.available += Number(row.availableSeats || 0);

            return acc;
        }, {
            total: 0,
            booked: 0,
            held: 0,
            available: 0
        });

        $("kpiTotalSeats").textContent = formatNumber(totals.total);
        $("kpiBookedSeats").textContent = formatNumber(totals.booked);
        $("kpiHeldSeats").textContent = formatNumber(totals.held);
        $("kpiAvailableSeats").textContent =
            formatNumber(totals.available);

        $("kpiOccupancyRate").textContent = formatPercent(
            totals.total > 0
                ? totals.booked / totals.total * 100
                : 0
        );
    }

    function renderDistribution(rows) {
        const counts = {
            low: 0,
            medium: 0,
            high: 0,
            full: 0
        };

        rows.forEach(row => {
            const band = getBand(row);

            if (Object.prototype.hasOwnProperty.call(counts, band)) {
                counts[band]++;
            }
        });

        $("distLow").textContent = `${counts.low} chuyến`;
        $("distMedium").textContent = `${counts.medium} chuyến`;
        $("distHigh").textContent = `${counts.high} chuyến`;
        $("distFull").textContent = `${counts.full} chuyến`;
    }

    function renderTable(rows) {
        const body = $("occupancyTableBody");

        if (!rows.length) {
            body.innerHTML = `
                <tr>
                    <td colspan="10">
                        <div class="table-message">
                            <strong>Không có chuyến phù hợp</strong>
                            <span>
                                Thử đổi từ khóa hoặc bộ lọc để xem dữ liệu khác.
                            </span>
                        </div>
                    </td>
                </tr>`;

            return;
        }

        body.innerHTML = rows.map(row => {
            const band = getBand(row);
            const isValid =
                !row.occupancyError && Number(row.totalSeats) > 0;

            const rate = isValid
                ? Math.max(
                    0,
                    Math.min(100, Number(row.occupancyRate || 0))
                )
                : 0;

            const rateHtml = isValid
                ? `
                    <div class="rate-cell">
                        <div class="rate-cell-top">
                            <strong>${formatPercent(rate)}</strong>
                        </div>
                        <div class="rate-track">
                            <span class="${band}"
                                  style="width:${rate}%"></span>
                        </div>
                    </div>`
                : `
                    <span title="${escapeHtml(
                    row.occupancyError || "Xe chưa có cấu hình ghế"
                )}">—</span>`;

            const routeName =
                row.routeName || "Chưa có tên tuyến";

            const routeText =
                `${row.originStopName || "—"} → ` +
                `${row.destinationStopName || "—"}`;

            const detailsUrl =
                `../chi-tiet-chuyen/index.html?tripId=` +
                encodeURIComponent(row.tripId || "");

            const seats = field =>
                isValid ? formatNumber(row[field]) : "—";

            return `
                <tr>
                    <td>
                        <div class="trip-code-cell">
                            <strong>${escapeHtml(
                row.tripCode || `Chuyến #${row.tripId}`
            )}</strong>
                            <small>ID: ${escapeHtml(row.tripId)}</small>
                        </div>
                    </td>

                    <td>
                        <div class="route-cell">
                            <strong>${escapeHtml(routeName)}</strong>
                            <small>${escapeHtml(row.routeCode || "")}</small>
                            <small>${escapeHtml(routeText)}</small>
                        </div>
                    </td>

                    <td>${formatDateTime(row.departureDateTime)}</td>
                    <td><span class="seat-number">${seats("totalSeats")}</span></td>
                    <td><span class="seat-number">${seats("bookedSeats")}</span></td>
                    <td><span class="seat-number">${seats("heldSeats")}</span></td>
                    <td><span class="seat-number available">${seats("availableSeats")}</span></td>
                    <td>${rateHtml}</td>
                    <td>
                        <span class="occupancy-status ${band}">
                            ${bandLabel(band)}
                        </span>
                    </td>
                    <td>
                        <a class="detail-link"
                           href="${detailsUrl}"
                           title="Mở chi tiết chuyến">Chi tiết</a>
                    </td>
                </tr>`;
        }).join("");
    }

    function render() {
        const rows = filteredRows();

        renderKpis(rows);
        renderDistribution(rows);
        renderTable(rows);

        const failedCount =
            state.rows.filter(row => row.occupancyError).length;

        const dateText = $("departureDate").value
            ? `Ngày ${$("departureDate").value}`
            : "các chuyến đang hoạt động";

        $("loadSummary").textContent =
            `Đang xem trang ${state.page}/${state.totalPages} — ` +
            `${state.rows.length} chuyến được tải (${dateText}).`;

        $("tableCaption").textContent =
            `Đang hiển thị ${rows.length} chuyến sau bộ lọc ` +
            `trên trang hiện tại; tổng danh sách API: ` +
            `${formatNumber(state.totalCount)} chuyến.`;

        $("paginationInfo").textContent =
            `Trang ${state.page}/${state.totalPages} · ` +
            `${formatNumber(state.totalCount)} chuyến`;

        $("prevPageBtn").disabled =
            state.page <= 1 || state.loading;

        $("nextPageBtn").disabled =
            state.page >= state.totalPages || state.loading;

        if (failedCount > 0) {
            $("errorBanner").textContent =
                `${failedCount} chuyến không tải được dữ liệu tỷ lệ lấp đầy. ` +
                `Kiểm tra X-User-Id và quyền ADMIN/MANAGER, ` +
                `sau đó nhấn Làm mới.`;

            $("errorBanner").hidden = false;
        } else {
            $("errorBanner").hidden = true;
        }
    }

    function init() {
        $("departureDate").min = localDate(new Date());

        $("applyFilterBtn").addEventListener(
            "click",
            () => loadTrips(1)
        );

        $("refreshBtn").addEventListener(
            "click",
            () => loadTrips(state.page)
        );

        $("resetFilterBtn").addEventListener("click", () => {
            $("departureDate").value = "";
            $("searchInput").value = "";
            $("rateFilter").value = "all";

            loadTrips(1);
        });

        $("searchInput").addEventListener("input", render);
        $("rateFilter").addEventListener("change", render);

        $("prevPageBtn").addEventListener("click", () => {
            if (state.page > 1) {
                loadTrips(state.page - 1);
            }
        });

        $("nextPageBtn").addEventListener("click", () => {
            if (state.page < state.totalPages) {
                loadTrips(state.page + 1);
            }
        });

        if (!getUserId()) {
            $("loadSummary").textContent =
                "Không tìm thấy ID tài khoản trong phiên đăng nhập.";

            $("errorBanner").textContent =
                "Vui lòng đăng xuất và đăng nhập lại, sau đó tải lại trang thống kê.";

            $("errorBanner").hidden = false;
        }

        loadTrips(1);
    }

    document.addEventListener("DOMContentLoaded", init);
})();