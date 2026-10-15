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
        body.innerHTML = '<tr><td colspan="4" class="empty-state">Không có giao dịch trong khoảng thời gian đã chọn.</td></tr>';
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
    const chart = $("revenueChart");
    const axis = $("chartAxis");
    $("chartNote").textContent = `${data.length} ngày có dữ liệu`;

    if (!data.length) {
        chart.innerHTML = '<div class="empty-state" style="width:100%">Chưa có dữ liệu biểu đồ.</div>';
        axis.innerHTML = "";
        return;
    }

    const max = Math.max(...data.map(x => Number(x.revenue)), 1);
    chart.innerHTML = data.map(item => {
        const height = Math.max((Number(item.revenue) / max) * 100, 2);
        return `<div class="chart-bar-wrap" title="${formatDate(item.date)}: ${money(item.revenue)}">
            <div class="chart-bar" style="height:${height}%"></div>
        </div>`;
    }).join("");

    axis.innerHTML = data.map((item, index) => {
        const show = data.length <= 12 || index % Math.ceil(data.length / 8) === 0 || index === data.length - 1;
        return `<span>${show ? item.date.slice(8, 10) + "/" + item.date.slice(5, 7) : ""}</span>`;
    }).join("");
}

function renderByRoute(data) {
    const el = $("routeRevenueList");
    if (!data.length) {
        el.innerHTML = '<div class="empty-state">Chưa có doanh thu theo tuyến.</div>';
        return;
    }

    const max = Math.max(...data.map(x => Number(x.revenue)), 1);
    el.innerHTML = data.slice(0, 8).map(item => {
        const width = Math.max(Number(item.revenue) / max * 100, 2);
        return `<div class="route-row">
            <div><div class="route-code">${item.routeCode}</div><div class="route-name">${item.routeName}</div></div>
            <div class="route-progress"><span style="width:${width}%"></span></div>
            <div class="route-value">${money(item.revenue)}</div>
        </div>`;
    }).join("");
}

async function loadReport() {
    const from = $("fromDate").value;
    const to = $("toDate").value;
    const routeId = $("routeFilter").value;

    if (!from || !to || from > to) {
        toast("⚠️ Vui lòng chọn khoảng thời gian hợp lệ.");
        return;
    }

    const params = new URLSearchParams({ from, to });
    if (routeId) params.set("routeId", routeId);

    try {
        $("applyFilterBtn").disabled = true;
        $("applyFilterBtn").textContent = "Đang tải...";

        const response = await fetch(`/api/Statistics/revenue?${params}`, {
            headers: headers()
        });

        if (response.status === 403) {
            toast("❌ Bạn không có quyền xem báo cáo doanh thu.");
            return;
        }

        if (!response.ok) {
            const error = await response.json().catch(() => null);
            throw new Error(error?.message || "Không tải được báo cáo.");
        }

        const data = await response.json();

        renderRoutes(data.routes || []);
        if (routeId) $("routeFilter").value = routeId;

        $("netRevenue").textContent = money(data.netRevenue);
        $("grossRevenue").textContent = money(data.grossRevenue);
        $("transactionCount").textContent = Number(data.transactionCount || 0).toLocaleString("vi-VN");
        $("averageTransaction").textContent = money(data.averageTransaction);
        $("refundBadge").textContent = `Hoàn tiền: ${money(data.refundAmount)}`;
        $("reportPeriod").textContent = `Từ ${formatDate(data.from)} đến ${formatDate(data.to)}`;

        renderChart(data.daily || []);
        renderByRoute(data.byRoute || []);
        renderDaily(data.daily || []);
    } catch (error) {
        console.error(error);
        toast("❌ " + error.message);
    } finally {
        $("applyFilterBtn").disabled = false;
        $("applyFilterBtn").textContent = "⌕ Xem báo cáo";
    }
}

function bindEvents() {
    $("applyFilterBtn").addEventListener("click", loadReport);
    $("resetFilterBtn").addEventListener("click", () => {
        setDefaultDates(30);
        $("routeFilter").value = "";
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
