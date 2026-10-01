// @ts-nocheck
let routes = [];
let selectedRouteId = null;

const $ = id => document.getElementById(id);
const money = n => new Intl.NumberFormat("vi-VN").format(n || 0) + " đ";

async function fetchAllData() {
    try {
        const res = await fetch("/api/TuyenXe");
        if (!res.ok) throw new Error("Lỗi nạp danh sách tuyến");
        const apiRoutes = await res.json();

        routes = apiRoutes.map(r => {
            const stops = (r.routeStops || []).sort((a, b) => a.stopOrder - b.stopOrder);

            let first = "Chưa có trạm";
            let last = "Chưa có trạm";

            if (stops.length > 0) {
                first = stops[0]?.busStop?.name || "Trạm đầu";
                last = stops[stops.length - 1]?.busStop?.name || "Trạm cuối";
            } else if (r.routeName && r.routeName.includes("-")) {
                const parts = r.routeName.replace(/Tuyến\s*\d+[:\s]*/i, "").split("-");
                first = parts[0]?.trim();
                last = parts[1]?.trim();
            }

            return {
                id: r.id,
                code: `T${String(r.id).padStart(2, "0")}`,
                name: r.routeName || `Tuyến ${r.id}`,
                start: first,
                end: last,
                fare: r.totalDistanceKm ? Math.round(r.totalDistanceKm * 1500) : 15000,
                status: r.isActive ? "Hoạt động" : "Tạm dừng",
                totalDistanceKm: r.totalDistanceKm || 25,
                stations: stops.map((rs, idx) => ({
                    id: rs.busStop?.id || rs.stopId,
                    name: rs.busStop?.name || `Trạm ${idx + 1}`,
                    address: rs.busStop?.address || "Việt Nam",
                    order: rs.stopOrder,
                    distance: rs.distanceFromStartKm || 0
                }))
            };
        });

        if (routes.length > 0) {
            if (!selectedRouteId || !routes.some(r => r.id === selectedRouteId)) {
                selectedRouteId = routes[0].id;
            }
        }

        updateStats();
        renderRoutes();
        renderSelectedRoute();
    } catch (err) {
        console.error(err);
        toast("❌ Lỗi nạp dữ liệu SQLite");
    }
}

function updateStats() {
    $("routeCount").textContent = routes.length;
    $("activeRouteCount").textContent = routes.filter(r => r.status === "Hoạt động").length;
    const stationSet = new Set(routes.flatMap(r => r.stations.map(s => s.name)));
    $("stationCount").textContent = stationSet.size;
    const avg = routes.reduce((sum, r) => sum + Number(r.fare), 0) / (routes.length || 1);
    $("avgFare").textContent = money(Math.round(avg));
}

function renderRoutes() {
    const q = $("routeSearch").value.trim().toLowerCase();
    const status = $("routeStatusFilter").value;

    const rows = routes.filter(r => {
        const match = [r.code, r.name, r.start, r.end].some(v => v.toLowerCase().includes(q));
        return match && (status === "all" || r.status === status);
    });

    $("routeTableBody").innerHTML = rows.length ? rows.map(r => `
        <tr class="${r.id === selectedRouteId ? "selected" : ""}" data-route="${r.id}">
            <td><span class="route-code">${r.code}</span></td>
            <td><strong>${r.name}</strong></td>
            <td>${r.start}</td>
            <td>${r.end}</td>
            <td><strong>${money(r.fare)}</strong></td>
            <td><span class="status ${r.status === "Hoạt động" ? "active" : "paused"}">${r.status}</span></td>
            <td>
                <div class="row-actions">
                    <button class="mini-btn edit-route" data-id="${r.id}">✎</button>
                    <button class="mini-btn delete delete-route" data-id="${r.id}">×</button>
                </div>
            </td>
        </tr>`).join("") : `<tr><td colspan="7" style="text-align:center;padding:25px;color:#8a99a5">Không có tuyến nào.</td></tr>`;

    document.querySelectorAll("tr[data-route]").forEach(row => {
        row.addEventListener("click", e => {
            if (e.target.closest(".row-actions")) return;
            selectedRouteId = Number(row.dataset.route);
            renderRoutes();
            renderSelectedRoute();
        });
    });

    document.querySelectorAll(".edit-route").forEach(btn => btn.addEventListener("click", e => {
        e.stopPropagation();
        openRouteModal(Number(btn.dataset.id));
    }));

    document.querySelectorAll(".delete-route").forEach(btn => btn.addEventListener("click", e => {
        e.stopPropagation();
        deleteRoute(Number(btn.dataset.id));
    }));
}

function renderSelectedRoute() {
    const route = routes.find(r => r.id === selectedRouteId) || routes[0];
    if (!route) {
        $("selectedRouteTitle").textContent = "Chưa chọn tuyến";
        $("stationList").innerHTML = "";
        return;
    }

    $("selectedRouteTitle").textContent = `${route.name} — ${route.start} → ${route.end}`;
    $("selectedRouteMeta").textContent = `Danh sách ${route.stations.length} trạm theo thứ tự phục vụ.`;
    $("detailCode").textContent = route.code;
    $("detailFare").textContent = money(route.fare);
    $("detailStationCount").textContent = route.stations.length;
    $("detailStatus").textContent = route.status;
    $("detailStatus").className = `status ${route.status === "Hoạt động" ? "active" : "paused"}`;

    // Sắp xếp tăng dần theo đúng thứ tự order
    const sortedStations = route.stations.slice().sort((a, b) => a.order - b.order);

    $("stationList").innerHTML = sortedStations.length ? sortedStations
        .map(s => `
            <div class="station-item">
                <div class="station-main">
                    <div class="station-number">${s.order}</div>
                    <div>
                        <strong>${s.name}</strong>
                        <small>${s.address}</small>
                    </div>
                </div>
                <div class="station-meta">
                    <span>↔ <b>${s.distance ? s.distance + " km" : "Điểm xuất phát"}</b></span>
                    <div class="station-actions">
                        <button class="mini-btn delete delete-station" data-id="${s.id}">×</button>
                    </div>
                </div>
            </div>`).join("") : `<div style="text-align:center;padding:20px;color:#8a99a5;">Tuyến chưa có trạm.</div>`;

    document.querySelectorAll(".delete-station").forEach(btn => btn.addEventListener("click", () => deleteStation(Number(btn.dataset.id))));
}

function openModal(id) { $(id).classList.add("open"); }
function closeModal(id) { $(id).classList.remove("open"); }

function openRouteModal(id = null) {
    $("routeForm").reset();
    $("routeId").value = "";
    $("routeModalTitle").textContent = id ? "Chỉnh sửa tuyến" : "Thêm tuyến";

    if (id) {
        const r = routes.find(x => x.id === id);
        if (r) {
            $("routeId").value = r.id;
            $("routeCode").value = r.code;
            $("routeName").value = r.name;
            $("routeStart").value = r.start;
            $("routeEnd").value = r.end;
            $("routeFare").value = r.fare;
            $("routeStatus").value = r.status;
        }
    }
    openModal("routeModal");
    $("routeCode").focus();
}

function openStationModal() {
    const route = routes.find(r => r.id === selectedRouteId);
    if (!route) {
        toast("Vui lòng chọn tuyến xe trước!");
        return;
    }
    $("stationForm").reset();
    $("stationModalTitle").textContent = `Thêm trạm vào ${route.name}`;
    $("stationOrder").value = route.stations.length + 1;
    openModal("stationModal");
    $("stationName").focus();
}

// 5. THÊM TUYẾN MỚI
$("routeForm").addEventListener("submit", async e => {
    e.preventDefault();
    const id = $("routeId").value;
    let name = $("routeName").value.trim();
    const start = $("routeStart").value.trim();
    const end = $("routeEnd").value.trim();
    const status = $("routeStatus").value === "Hoạt động";
    const fare = Number($("routeFare").value);
    const distanceKm = Math.round(fare / 1500) || 30;

    if (start && end && !name.includes("-")) {
        name = `${name}: ${start} - ${end}`;
    }

    try {
        if (id) {
            await fetch(`/api/TuyenXe/${id}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    Id: Number(id),
                    RouteName: name,
                    TotalDistanceKm: distanceKm,
                    EstimatedDuration: "01:30:00",
                    IsActive: status
                })
            });
            toast("✅ Cập nhật tuyến thành công!");
        } else {
            const res = await fetch("/api/TuyenXe", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    RouteName: name,
                    TotalDistanceKm: distanceKm,
                    EstimatedDuration: "01:30:00",
                    IsActive: status,
                    StartStation: start,
                    EndStation: end
                })
            });
            if (!res.ok) throw new Error("Lỗi khi thêm tuyến");
            toast("✅ Đã tạo tuyến và tự động gán 2 trạm dừng!");
        }

        closeModal("routeModal");
        await fetchAllData();
    } catch (err) {
        console.error(err);
        toast("❌ Lỗi khi lưu dữ liệu");
    }
});

// 6. THÊM TRẠM DỪNG VÀO TUYẾN (HỖ TRỢ CHÈN VÀO GIỮA)
$("stationForm").addEventListener("submit", async e => {
    e.preventDefault();
    const route = routes.find(r => r.id === selectedRouteId);
    if (!route) return;

    const name = $("stationName").value.trim();
    const order = Number($("stationOrder").value) || 1;
    const distance = Number($("stationDistance").value) || 0;

    try {
        const res = await fetch("/api/TuyenXe/them-tram-vao-tuyen", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                RouteId: route.id,
                StopName: name,
                StopOrder: order,
                DistanceKm: distance
            })
        });

        if (res.ok) {
            toast(`✅ Đã chèn trạm "${name}" vào thứ tự ${order}!`);
            closeModal("stationModal");
            await fetchAllData(); // Tải lại toàn bộ dữ liệu từ DB để nhận thứ tự mới
        } else {
            toast("❌ Lỗi thêm trạm");
        }
    } catch (err) {
        console.error(err);
        toast("❌ Lỗi kết nối");
    }
});

// 7. XÓA TUYẾN
async function deleteRoute(id) {
    if (!confirm("Bạn có chắc chắn muốn xóa tuyến này?")) return;
    try {
        const res = await fetch(`/api/TuyenXe/${id}`, { method: "DELETE" });
        if (res.ok) {
            toast("✅ Đã xóa tuyến khỏi CSDL");
            selectedRouteId = null;
            await fetchAllData();
        }
    } catch (e) { toast("❌ Lỗi xóa tuyến"); }
}

// 8. XÓA TRẠM KHỎI TUYẾN
async function deleteStation(stopId) {
    if (!confirm("Bạn có chắc chắn muốn xóa trạm này khỏi tuyến?")) return;
    try {
        const res = await fetch(`/api/TuyenXe/xoa-tram-khoi-tuyen?routeId=${selectedRouteId}&stopId=${stopId}`, {
            method: "DELETE"
        });
        if (res.ok) {
            toast("✅ Đã xóa trạm khỏi tuyến");
            await fetchAllData();
        }
    } catch (e) { toast("❌ Lỗi xóa trạm"); }
}

function toast(msg) {
    const el = $("toast");
    if (!el) return;
    el.textContent = msg;
    el.classList.add("show");
    clearTimeout(window.toastTimer);
    window.toastTimer = setTimeout(() => el.classList.remove("show"), 2200);
}

$("addRouteBtn").addEventListener("click", () => openRouteModal());
$("addStationBtn").addEventListener("click", () => openStationModal());
$("routeSearch").addEventListener("input", renderRoutes);
$("routeStatusFilter").addEventListener("change", renderRoutes);

document.querySelectorAll("[data-close]").forEach(btn => btn.addEventListener("click", () => closeModal(btn.dataset.close)));
document.querySelectorAll(".modal-backdrop").forEach(b => b.addEventListener("click", e => { if (e.target === b) closeModal(b.id); }));

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".side-nav .side-link").forEach(link => {
        link.addEventListener("click", e => {
            if (link.textContent.includes("Tuyến & trạm")) return;
            e.preventDefault();
            toast("Mục này thuộc Sprint kế tiếp.");
        });
    });

    const logoutBtn = document.querySelector(".logout-btn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", () => {
            if (confirm("Đăng xuất?")) {
                localStorage.removeItem("userSession");
                window.location.href = "/auth/dangnhap.html";
            }
        });
    }

    fetchAllData();
});