const $ = id => document.getElementById(id);

function getSession() {
    try {
        const raw = localStorage.getItem("userSession");
        return raw ? JSON.parse(raw) : null;
    } catch { return null; }
}

function getUserId() {
    const user = getSession();
    return user?.userId || user?.UserId || "";
}

function headers() {
    return { "X-User-Id": getUserId() };
}

function money(value) {
    return new Intl.NumberFormat("vi-VN", {
        maximumFractionDigits: 0
    }).format(Number(value || 0)) + " đ";
}

function formatDate(value) {
    const [y, m, d] = value.split("-");
    return `${d}/${m}/${y}`;
}

function localDate(date) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, "0");
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
}

function setDefaultDates(days = 30) {
    const end = new Date();
    const start = new Date();
    start.setDate(end.getDate() - days + 1);
    $("fromDate").value = localDate(start);
    $("toDate").value = localDate(end);
}

function setMonthDates() {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), 1);
    $("fromDate").value = localDate(start);
    $("toDate").value = localDate(now);
}

function toast(message) {
    const el = $("toast");
    if (!el) return;
    el.textContent = message;
    el.classList.add("show");
    clearTimeout(window.__toastTimer);
    window.__toastTimer = setTimeout(() => el.classList.remove("show"), 2600);
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function showReportError(message) {
    // Không để lại số liệu của lần tải trước khi API lỗi.
    [
        "netRevenue",
        "grossRevenue",
        "transactionCount",
        "averageTransaction"
    ].forEach(id => {
        const element = $(id);
        if (element) element.textContent = "—";
    });

    if ($("refundBadge")) {
        $("refundBadge").textContent = "Không tải được tiền hoàn";
    }

    if ($("reportPeriod")) {
        $("reportPeriod").textContent = "Không tải được báo cáo";
    }

    // Xóa biểu đồ cũ, hiển thị thông báo lỗi.
    if ($("revenueLineChart")) $("revenueLineChart").innerHTML = "";
    if ($("chartAxis")) $("chartAxis").innerHTML = "";
    if ($("chartYAxis")) $("chartYAxis").innerHTML = "";
    if ($("chartGridLines")) $("chartGridLines").innerHTML = "";
    if ($("chartNote")) $("chartNote").textContent = "Lỗi tải dữ liệu";

    if ($("chartEmpty")) {
        $("chartEmpty").hidden = false;

        const title = $("chartEmpty").querySelector("strong");
        const detail = $("chartEmpty").querySelector("span");

        if (title) title.textContent = "Không thể tải báo cáo doanh thu";
        if (detail) detail.textContent = message;
    }

    if ($("routeRevenueList")) {
        $("routeRevenueList").innerHTML = `
            <div class="data-empty">
                <strong>Không tải được dữ liệu theo tuyến</strong>
                <span>${escapeHtml(message)}</span>
            </div>`;
    }

    if ($("dailyTableBody")) {
        $("dailyTableBody").innerHTML = `
            <tr>
                <td colspan="4">
                    <div class="data-empty">
                        <strong>Không thể tải báo cáo</strong>
                        <span>${escapeHtml(message)}</span>
                    </div>
                </td>
            </tr>`;
    }
}

function renderRoutes(routes) {
    const select = $("routeFilter");
    select.innerHTML = '<option value="">Tất cả tuyến</option>';
    routes.forEach(route => {
        const option = document.createElement("option");
        option.value = route.id;
        option.textContent = `${route.code} — ${route.name}`;
        select.appendChild(option);
    });
}

function renderDaily(data) {
    const body = $("dailyTableBody");
    if (!data.length) {
        body.innerHTML = `
            <tr>
                <td colspan="4">
                    <div class="data-empty">
                        <div class="empty-icon">▤</div>
                        <strong>Không có dữ liệu doanh thu</strong>
                        <span>Chưa ghi nhận giao dịch thanh toán thành công trong khoảng thời gian này.</span>
                        <button type="button" data-empty-action="reset">Đặt lại bộ lọc</button>
                    </div>
                </td>
            </tr>`;
        return;
    }

    body.innerHTML = data.map(item => `
        <tr>
            <td>${formatDate(item.date)}</td>
            <td><strong>${money(item.revenue)}</strong></td>
            <td>${item.transactions}</td>
            <td>${money(item.transactions ? item.revenue / item.transactions : 0)}</td>
        </tr>
    `).join("");
}

function renderChart(data) {
    const svg = $("revenueLineChart");
    const axis = $("chartAxis");
    const yAxis = $("chartYAxis");
    const grid = $("chartGridLines");
    const empty = $("chartEmpty");

    $("chartNote").textContent = data.length ? `${data.length} ngày` : "Không có dữ liệu";

    if (!data.length) {
        svg.innerHTML = "";
        axis.innerHTML = "";
        yAxis.innerHTML = "";
        grid.innerHTML = "";
        empty.hidden = false;
        return;
    }

    empty.hidden = true;

    const values = data.map(item => Number(item.revenue || 0));
    const max = Math.max(...values, 1);
    const min = 0;
    const width = 1000;
    const height = 300;
    const padX = 24;
    const padY = 18;
    const usableW = width - padX * 2;
    const usableH = height - padY * 2;

    const points = values.map((value, index) => {
        const x = data.length === 1
            ? width / 2
            : padX + (index / (data.length - 1)) * usableW;
        const y = height - padY - ((value - min) / max) * usableH;
        return { x, y, value };
    });

    const line = points.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x.toFixed(2)} ${p.y.toFixed(2)}`).join(" ");
    const area = `${line} L ${points[points.length - 1].x.toFixed(2)} ${height - padY} L ${points[0].x.toFixed(2)} ${height - padY} Z`;

    svg.innerHTML = `
        <defs>
            <linearGradient id="revenueAreaGradient" x1="0" x2="0" y1="0" y2="1">
                <stop offset="0%" stop-color="#1685e8" stop-opacity=".22"></stop>
                <stop offset="100%" stop-color="#1685e8" stop-opacity=".02"></stop>
            </linearGradient>
        </defs>
        <path d="${area}" fill="url(#revenueAreaGradient)"></path>
        <path d="${line}" fill="none" stroke="#1685e8" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"></path>
        ${points.map((p, i) => `
            <circle class="chart-point" cx="${p.x}" cy="${p.y}" r="5" fill="#fff" stroke="#1685e8" stroke-width="3">
                <title>${formatDate(data[i].date)}: ${money(data[i].revenue)}</title>
            </circle>`).join("")}
    `;

    const steps = 4;
    yAxis.innerHTML = Array.from({ length: steps + 1 }, (_, i) => {
        const value = max - (max / steps) * i;
        return `<span>${money(value).replace(" đ", "")}</span>`;
    }).join("");

    grid.innerHTML = Array.from({ length: steps + 1 }, () => `<span></span>`).join("");

    axis.innerHTML = data.map((item, index) => {
        const show = data.length <= 10 || index === 0 || index === data.length - 1 || index % Math.ceil(data.length / 7) === 0;
        return `<span>${show ? item.date.slice(8, 10) + "/" + item.date.slice(5, 7) : ""}</span>`;
    }).join("");
}

function renderByRoute(data) {
    const el = $("routeRevenueList");
    if (!data.length) {
        el.innerHTML = `
            <div class="data-empty">
                <div class="empty-icon">⇄</div>
                <strong>Chưa có doanh thu theo tuyến</strong>
                <span>Không tìm thấy tuyến có giao dịch trong điều kiện hiện tại.</span>
                <button type="button" data-empty-action="reset">Đặt lại bộ lọc</button>
            </div>`;
        return;
    }

    const max = Math.max(...data.map(x => Number(x.revenue)), 1);
    el.innerHTML = data.slice(0, 8).map(item => {
        const width = Math.max(Number(item.revenue) / max * 100, 2);
        return `<div class="route-row">
            <div>
            <div class="route-code">${escapeHtml(item.routeCode)}</div>
            <div class="route-name">${escapeHtml(item.routeName)}</div></div>
            <div class="route-progress"><span style="width:${width}%"></span></div>
            <div class="route-value">${money(item.revenue)}</div>
        </div>`;
    }).join("");
}

function bindEmptyActions() {
    document.querySelectorAll("[data-empty-action='reset']").forEach(button => {
        button.addEventListener("click", () => {
            setDefaultDates(30);
            $("routeFilter").value = "";
            loadReport();
        });
    });
}


async function loadReport() {
    const from = $("fromDate").value;
    const to = $("toDate").value;
    const routeId = $("routeFilter").value;

    if (!from || !to || from > to) {
        toast("⚠️ Vui lòng chọn khoảng thời gian hợp lệ.");
        return;
    }

    if (!getUserId()) {
        window.location.href = "../auth/dangnhap.html";
        return;
    }

    const params = new URLSearchParams({ from, to });

    if (routeId) {
        params.set("routeId", routeId);
    }

    const button = $("applyFilterBtn");

    try {
        button.disabled = true;
        button.textContent = "Đang tải...";

        const response = await fetch(
            `/api/Statistics/revenue?${params.toString()}`,
            {
                method: "GET",
                headers: headers()
            }
        );

        if (response.status === 401) {
            window.location.href = "../auth/dangnhap.html";
            return;
        }

        const data = await response.json().catch(() => null);

        if (response.status === 403) {
            const message =
                data?.message ||
                "Bạn không có quyền xem báo cáo doanh thu.";

            showReportError(message);
            toast("❌ " + message);
            return;
        }

        if (!response.ok) {
            throw new Error(
                data?.message ||
                `Không tải được báo cáo (HTTP ${response.status}).`
            );
        }

        // Xác minh cấu trúc JSON trước khi vẽ giao diện.
        if (!data || typeof data !== "object") {
            throw new Error("API không trả về JSON hợp lệ.");
        }

        const requiredFields = [
            "netRevenue",
            "grossRevenue",
            "refundAmount",
            "transactionCount",
            "averageTransaction"
        ];

        for (const field of requiredFields) {
            if (
                data[field] === undefined ||
                data[field] === null ||
                !Number.isFinite(Number(data[field]))
            ) {
                throw new Error(
                    `Dữ liệu API thiếu hoặc không hợp lệ ở trường "${field}".`
                );
            }
        }

        if (
            !Array.isArray(data.routes) ||
            !Array.isArray(data.daily) ||
            !Array.isArray(data.byRoute)
        ) {
            throw new Error(
                "Cấu trúc routes, daily hoặc byRoute của API không hợp lệ."
            );
        }

        // Kiểm tra từng dòng thống kê để tránh lỗi khi vẽ biểu đồ.
        const validDaily = data.daily.every(item =>
            item &&
            typeof item.date === "string" &&
            Number.isFinite(Number(item.revenue)) &&
            Number.isFinite(Number(item.transactions))
        );

        const validRoutes = data.byRoute.every(item =>
            item &&
            Number.isFinite(Number(item.revenue))
        );

        if (!validDaily || !validRoutes) {
            throw new Error(
                "Một hoặc nhiều dòng thống kê trong API không hợp lệ."
            );
        }

        // Cập nhật bộ lọc tuyến, giữ tuyến mà người dùng đang chọn.
        renderRoutes(data.routes);

        if (routeId) {
            $("routeFilter").value = routeId;
        }

        // Cập nhật 4 thẻ tổng quan.
        $("netRevenue").textContent = money(data.netRevenue);
        $("grossRevenue").textContent = money(data.grossRevenue);

        $("transactionCount").textContent =
            Number(data.transactionCount).toLocaleString("vi-VN");

        $("averageTransaction").textContent =
            money(data.averageTransaction);

        $("refundBadge").textContent =
            `Hoàn tiền: ${money(data.refundAmount)}`;

        $("reportPeriod").textContent =
            `Từ ${formatDate(data.from)} đến ${formatDate(data.to)}`;

        // Cập nhật biểu đồ và các bảng.
        renderChart(data.daily);
        renderByRoute(data.byRoute);
        renderDaily(data.daily);

        // Các nút đặt lại bộ lọc chỉ được gắn một lần trên mỗi phần tử.
        bindEmptyActions();

    } catch (error) {
        console.error("Lỗi tải báo cáo doanh thu:", error);

        showReportError(
            error.message || "Đã xảy ra lỗi khi tải báo cáo doanh thu."
        );

        toast("❌ " + (error.message || "Không tải được báo cáo."));

    } finally {
        button.disabled = false;
        button.textContent = "⌕ Xem báo cáo";
    }
}

function bindEvents() {
    $("applyFilterBtn").addEventListener("click", loadReport);
    $("resetFilterBtn").addEventListener("click", () => {
        setDefaultDates(30);
        $("routeFilter").value = "";
        document.querySelectorAll("[data-range]").forEach(x => x.classList.remove("selected"));
        document.querySelector("[data-range='30']")?.classList.add("selected");
        loadReport();
    });

    document.querySelectorAll("[data-range]").forEach(button => {
        button.addEventListener("click", () => {
            document.querySelectorAll("[data-range]").forEach(x => x.classList.remove("selected"));
            button.classList.add("selected");
            const range = button.dataset.range;
            if (range === "month") setMonthDates();
            else setDefaultDates(Number(range));
            loadReport();
        });
    });
}

document.addEventListener("DOMContentLoaded", () => {
    setDefaultDates(30);
    bindEvents();
    loadReport();
});
