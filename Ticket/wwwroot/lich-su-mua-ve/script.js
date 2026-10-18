
(() => {
    "use strict";

    const API_URL = "/api/Ticket/history";
    const PAGE_SIZE = 20;

    const $ = id => document.getElementById(id);

    const state = {
        page: 1,
        pageSize: PAGE_SIZE,
        totalCount: 0,
        totalPages: 0,
        items: [],
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

        return String(
            user?.userId ||
            user?.UserId ||
            user?.userID ||
            user?.UserID ||
            ""
        ).trim();
    }

    function getValue(object, ...keys) {
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

    function formatMoney(value) {
        if (value === null || value === undefined || value === "") {
            return "—";
        }

        return new Intl.NumberFormat("vi-VN", {
            style: "currency",
            currency: "VND",
            maximumFractionDigits: 0
        }).format(Number(value));
    }

    function formatDate(value) {
        if (!value) return "—";

        const text = String(value);
        const match = text.match(/^(\d{4})-(\d{2})-(\d{2})/);

        if (!match) return escapeHtml(text);

        return `${match[3]}/${match[2]}/${match[1]}`;
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

    function showToast(message) {
        const element = $("toast");
        if (!element) return;

        element.textContent = message;
        element.classList.add("show");

        clearTimeout(window.__ticketHistoryToastTimer);

        window.__ticketHistoryToastTimer = setTimeout(() => {
            element.classList.remove("show");
        }, 3200);
    }

    async function fetchJson(url) {
        const userId = getUserId();

        if (!userId) {
            throw new Error(
                "Không tìm thấy ID tài khoản. " +
                "Vui lòng đăng nhập lại."
            );
        }

        const response = await fetch(url, {
            method: "GET",
            headers: {
                "X-User-Id": userId,
                "Accept": "application/json"
            },
            cache: "no-store"
        });

        const data = await response.json().catch(() => null);

        if (!response.ok) {
            let message = "";

            if (response.status === 401) {
                message = "Phiên đăng nhập không hợp lệ.";
            } else if (response.status === 403) {
                message = "Bạn không có quyền xem lịch sử mua vé.";
            } else {
                message =
                    getValue(data, "message", "Message") ||
                    `Không thể tải dữ liệu (HTTP ${response.status}).`;
            }

            throw new Error(message);
        }

        return data;
    }

    function normalizeItem(item) {
        return {
            ticketId:
                getValue(item, "ticketId", "TicketId"),

            ticketCode:
                getValue(item, "ticketCode", "TicketCode") || "",

            bookingCode:
                getValue(item, "bookingCode", "BookingCode") || "",

            passengerName:
                getValue(item, "passengerName", "PassengerName") ||
                "Chưa cập nhật",

            tripCode:
                getValue(item, "tripCode", "TripCode") || "",

            routeCode:
                getValue(item, "routeCode", "RouteCode") || "",

            routeName:
                getValue(item, "routeName", "RouteName") || "",

            tripDate:
                getValue(item, "tripDate", "TripDate"),

            departureTime:
                getValue(item, "departureTime", "DepartureTime") || "",

            arrivalTime:
                getValue(item, "arrivalTime", "ArrivalTime") || "",

            departureStop:
                getValue(item, "departureStop", "DepartureStop") || "",

            arrivalStop:
                getValue(item, "arrivalStop", "ArrivalStop") || "",

            seatCode:
                getValue(item, "seatCode", "SeatCode") || "",

            fare:
                getValue(item, "fare", "Fare"),

            ticketStatus:
                getValue(item, "ticketStatus", "TicketStatus") || "",

            bookingStatus:
                getValue(item, "bookingStatus", "BookingStatus") || "",

            issuedAt:
                getValue(item, "issuedAt", "IssuedAt"),

            usedAt:
                getValue(item, "usedAt", "UsedAt"),

            paymentStatus:
                getValue(item, "paymentStatus", "PaymentStatus") ||
                "NO_TRANSACTION",

            paymentProvider:
                getValue(item, "paymentProvider", "PaymentProvider") || "",

            paymentTransactionCode:
                getValue(
                    item,
                    "paymentTransactionCode",
                    "PaymentTransactionCode"
                ) || "",

            paymentAmount:
                getValue(item, "paymentAmount", "PaymentAmount"),

            paidAt:
                getValue(item, "paidAt", "PaidAt"),

            refundStatus:
                getValue(item, "refundStatus", "RefundStatus") ||
                "NO_TRANSACTION",

            refundReason:
                getValue(item, "refundReason", "RefundReason") || "",

            refundAmount:
                getValue(item, "refundAmount", "RefundAmount"),

            refundedAt:
                getValue(item, "refundedAt", "RefundedAt")
        };
    }

    function statusInfo(kind, rawStatus) {
        const status = String(rawStatus || "")
            .trim()
            .toUpperCase();

        const maps = {
            ticket: {
                VALID: ["Còn hiệu lực", "success"],
                USED: ["Đã sử dụng", "info"],
                CANCELLED: ["Đã hủy", "danger"],
                EXPIRED: ["Hết hạn", "warning"],
                CHANGED: ["Đã đổi", "neutral"]
            },

            payment: {
                SUCCESS: ["Thành công", "success"],
                PENDING: ["Đang chờ", "pending"],
                FAILED: ["Thất bại", "danger"],
                CANCELLED: ["Đã hủy", "neutral"],
                NO_TRANSACTION: ["Chưa có giao dịch", "neutral"]
            },

            refund: {
                SUCCESS: ["Đã hoàn tiền", "success"],
                COMPLETED: ["Đã hoàn tất", "success"],
                PENDING: ["Đang chờ hoàn", "pending"],
                FAILED: ["Hoàn tiền thất bại", "danger"],
                NO_TRANSACTION: ["Không có hoàn tiền", "neutral"]
            }
        };

        const result = maps[kind]?.[status];

        return result || [
            status || "Chưa có trạng thái",
            "neutral"
        ];
    }

    function statusBadge(kind, value) {
        const [label, cssClass] = statusInfo(kind, value);

        return `
            <span class="history-status ${cssClass}">
                ${escapeHtml(label)}
            </span>`;
    }

    function renderPayment(item) {
        const amount = item.paymentAmount !== null
            ? `<span class="history-primary-text">${escapeHtml(formatMoney(item.paymentAmount))
            }</span>`
            : "";

        const provider = item.paymentProvider
            ? `<span class="history-secondary-text">${escapeHtml(item.paymentProvider)
            }</span>`
            : "";

        const transaction = item.paymentTransactionCode
            ? `<span class="history-secondary-text">${escapeHtml(item.paymentTransactionCode)
            }</span>`
            : "";

        const paidAt = item.paidAt
            ? `<span class="history-secondary-text">Thanh toán: ${escapeHtml(formatDateTime(item.paidAt))
            }</span>`
            : "";

        return `
            ${statusBadge("payment", item.paymentStatus)}
            ${amount}
            ${provider}
            ${transaction}
            ${paidAt}`;
    }

    function renderRefund(item) {
        const amount = item.refundAmount !== null
            ? `<span class="history-primary-text">${escapeHtml(formatMoney(item.refundAmount))
            }</span>`
            : "";

        const reason = item.refundReason
            ? `<span class="history-secondary-text">${escapeHtml(item.refundReason)
            }</span>`
            : "";

        const refundedAt = item.refundedAt
            ? `<span class="history-secondary-text">${escapeHtml(formatDateTime(item.refundedAt))
            }</span>`
            : "";

        return `
            ${statusBadge("refund", item.refundStatus)}
            ${amount}
            ${reason}
            ${refundedAt}`;
    }

    function renderTable() {
        const body = $("historyTableBody");

        if (!state.items.length) {
            body.innerHTML = `
                <tr>
                    <td colspan="9">
                        <div class="history-empty">
                            <strong>Không tìm thấy lịch sử mua vé</strong>
                            <span>
                                Thử thay đổi từ khóa hoặc trạng thái lọc.
                            </span>
                        </div>
                    </td>
                </tr>`;

            return;
        }

        body.innerHTML = state.items.map(item => {
            const routeName =
                item.routeName || "Chưa có tên tuyến";

            const itinerary =
                `${item.departureStop || "—"} → ` +
                `${item.arrivalStop || "—"}`;

            const tripDetails = [
                item.routeCode,
                item.tripDate ? formatDate(item.tripDate) : ""
            ].filter(Boolean).join(" · ");

            const tripTime = [
                item.departureTime,
                item.arrivalTime
            ].filter(Boolean).join(" – ");

            const bookingStatus = item.bookingStatus
                ? `<span class="history-secondary-text">Đơn: ${escapeHtml(item.bookingStatus)
                }</span>`
                : "";

            const seat = item.seatCode
                ? `<span class="history-primary-text">Ghế ${escapeHtml(item.seatCode)
                }</span>`
                : "";

            const usedAt = item.usedAt
                ? `<span class="history-secondary-text">Sử dụng: ${escapeHtml(formatDateTime(item.usedAt))
                }</span>`
                : "";

            return `
                <tr>
                    <td>
                        <span class="history-primary-text">
                            ${escapeHtml(item.ticketCode || `Vé #${item.ticketId}`)}
                        </span>

                        <span class="history-secondary-text">
                            Booking: ${escapeHtml(item.bookingCode || "—")}
                        </span>

                        ${bookingStatus}
                    </td>

                    <td>
                        <div class="history-passenger">
                            ${escapeHtml(item.passengerName)}
                        </div>
                    </td>

                    <td>
                        <div class="history-route">
                            <strong>${escapeHtml(item.tripCode || "—")}</strong>
                            <small>${escapeHtml(routeName)}</small>
                            <small>${escapeHtml(tripDetails || "—")}</small>
                        </div>
                    </td>

                    <td>
                        <div class="history-route">
                            <small>${escapeHtml(itinerary)}</small>
                            <small>${escapeHtml(tripTime || "Chưa có giờ")}</small>
                        </div>
                    </td>

                    <td>
                        ${seat}
                        <span class="history-fare">
                            ${escapeHtml(formatMoney(item.fare))}
                        </span>
                    </td>

                    <td>
                        <span class="history-primary-text">
                            ${escapeHtml(formatDateTime(item.issuedAt))}
                        </span>
                        ${usedAt}
                    </td>

                    <td>
                        ${statusBadge("ticket", item.ticketStatus)}
                    </td>

                    <td>${renderPayment(item)}</td>

                    <td>${renderRefund(item)}</td>
                </tr>`;
        }).join("");
    }

    function renderSummary() {
        $("totalCount").textContent =
            formatNumber(state.totalCount);

        $("pageCount").textContent =
            formatNumber(state.items.length);

        $("currentPage").textContent =
            `${state.page} / ${Math.max(1, state.totalPages)}`;

        $("pageDescription").textContent =
            state.totalCount === 0
                ? "Không có bản ghi"
                : `${state.pageSize} bản ghi tối đa mỗi trang`;

        $("paginationInfo").textContent =
            `Trang ${state.page} / ${Math.max(1, state.totalPages)} · ` +
            `${formatNumber(state.totalCount)} bản ghi`;

        $("prevPageBtn").disabled =
            state.loading || state.page <= 1;

        $("nextPageBtn").disabled =
            state.loading ||
            state.page >= state.totalPages ||
            state.totalPages === 0;
    }

    function showError(message) {
        const banner = $("errorBanner");

        banner.textContent = message;
        banner.hidden = false;
    }

    function clearError() {
        $("errorBanner").hidden = true;
        $("errorBanner").textContent = "";
    }

    async function loadHistory(page = 1) {
        if (state.loading) return;

        state.loading = true;
        state.page = Math.max(1, page);

        $("applyFilterBtn").disabled = true;
        $("refreshBtn").disabled = true;

        $("resultMessage").textContent = "Đang tải dữ liệu từ API...";

        $("historyTableBody").innerHTML = `
            <tr>
                <td colspan="9">
                    <div class="history-empty">
                        <span class="history-loader"></span>
                        Đang tải lịch sử mua vé...
                    </div>
                </td>
            </tr>`;

        clearError();
        renderSummary();

        try {
            const params = new URLSearchParams();

            params.set("page", String(state.page));
            params.set("pageSize", String(state.pageSize));

            const search = $("searchInput").value.trim();
            const ticketStatus = $("ticketStatusFilter").value;
            const paymentStatus = $("paymentStatusFilter").value;
            const refundStatus = $("refundStatusFilter").value;

            if (search) {
                params.set("search", search);
            }

            if (ticketStatus) {
                params.set("ticketStatus", ticketStatus);
            }

            if (paymentStatus) {
                params.set("paymentStatus", paymentStatus);
            }

            if (refundStatus) {
                params.set("refundStatus", refundStatus);
            }

            const data = await fetchJson(
                `${API_URL}?${params.toString()}`
            );

            const rawItems =
                getValue(data, "items", "Items") || [];

            state.items = rawItems.map(normalizeItem);

            state.totalCount = Number(
                getValue(data, "totalCount", "TotalCount") ?? 0
            );

            state.totalPages = Number(
                getValue(data, "totalPages", "TotalPages") ?? 0
            );

            const returnedPage = Number(
                getValue(data, "page", "Page") ?? state.page
            );

            if (returnedPage > 0) {
                state.page = returnedPage;
            }

            renderTable();
            renderSummary();

            $("resultMessage").textContent =
                `Đã tải ${formatNumber(state.items.length)} vé ` +
                `trong ${formatNumber(state.totalCount)} kết quả phù hợp.`;

        } catch (error) {
            state.items = [];
            state.totalCount = 0;
            state.totalPages = 0;

            renderTable();
            renderSummary();

            $("resultMessage").textContent = "Không thể tải dữ liệu.";

            showError(
                error.message ||
                "Đã xảy ra lỗi khi tải lịch sử mua vé."
            );

            showToast("Không tải được lịch sử mua vé.");
        } finally {
            state.loading = false;

            $("applyFilterBtn").disabled = false;
            $("refreshBtn").disabled = false;

            renderSummary();
        }
    }

    function resetFilters() {
        $("searchInput").value = "";
        $("ticketStatusFilter").value = "";
        $("paymentStatusFilter").value = "";
        $("refundStatusFilter").value = "";

        loadHistory(1);
    }

    function initAccountHeader() {
        const user = getSession();
        if (!user) return;

        const name =
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

        let roleText = user.roleName || user.RoleName || "Người dùng";

        if (role.includes("ADMIN") ||
            role.includes("QUẢN TRỊ")) {
            roleText = "Quản trị hệ thống";
        } else if (role.includes("MANAGER") ||
            role.includes("QUẢN LÝ")) {
            roleText = "Quản lý vận hành";
        }

        const parts = name.trim().split(/\s+/);

        const initials = parts.length > 1
            ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
            : name.substring(0, 2).toUpperCase();

        if ($("topAvatar")) {
            $("topAvatar").textContent = initials;
        }

        if ($("topUserRole")) {
            $("topUserRole").textContent = roleText;
        }
    }

    function initEvents() {
        $("filterForm").addEventListener("submit", event => {
            event.preventDefault();
            loadHistory(1);
        });

        $("resetFilterBtn").addEventListener(
            "click",
            resetFilters
        );

        $("refreshBtn").addEventListener("click", () => {
            loadHistory(state.page);
        });

        $("prevPageBtn").addEventListener("click", () => {
            if (state.page > 1) {
                loadHistory(state.page - 1);
            }
        });

        $("nextPageBtn").addEventListener("click", () => {
            if (state.page < state.totalPages) {
                loadHistory(state.page + 1);
            }
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        initAccountHeader();
        initEvents();
        loadHistory(1);
    });
})();
