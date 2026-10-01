// @ts-nocheck
let routes = [];
let selectedRouteId = null;

const $ = id => document.getElementById(id);
const money = n => new Intl.NumberFormat("vi-VN").format(n || 0) + " đ";

// Thuật toán đề xuất mã tuyến nhỏ nhất chưa dùng (T01, T02...)
// Nếu bảng rỗng -> luôn luôn trả về T01
function getNextRouteCode() {
    const used = new Set();
    routes.forEach(r => {
        const m = String(r.code || "").match(/\d+/);
        if (m) used.add(parseInt(m[0], 10));
    });
    let c = 1;
    while (used.has(c)) c++;
    return `T${String(c).padStart(2, "0")}`;
}

async function fetchAllData() {
    try {
        const res = await fetch("/api/TuyenXe");
        if (!res.ok) throw new Error("Lỗi nạp danh sách tuyến");
        const apiRoutes = await res.json();

        // Đánh số thứ tự T01, T02, T03... liên tục theo danh sách thực tế
        routes = apiRoutes.map((r, index) => {
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

            // Luôn đánh số liên tục theo vị trí thực tế: Tuyến 1, 2, 3...
            const seqNum = index + 1;
            const routeCode = `T${String(seqNum).padStart(2, "0")}`;

            // Chuẩn hóa tên hiển thị: Loại bỏ các số cũ bị nhảy cóc
            let cleanName = r.routeName || `Tuyến ${seqNum}`;
            if (cleanName.match(/Tuyến\s*\d+/i)) {
                cleanName = cleanName.replace(/Tuyến\s*\d+/i, `Tuyến ${String(seqNum).padStart(2, "0")}`);
            }

            return {
                id: r.id,
                code: routeCode,
                name: cleanName,
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
        } else {
            selectedRouteId = null;
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
            <td><strong>${money(r.fare)}</strong> <small style="color:#6c757d">(${r.totalDistanceKm} km)</small></td>
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
    const route = routes.find(r => r.id === selectedRouteId);
    if (!route) {
        $("selectedRouteTitle").textContent = "Chưa chọn tuyến";
        $("selectedRouteMeta").textContent = "Chọn tuyến từ bảng danh sách bên trên.";
        $("detailCode").textContent = "—";
        $("detailFare").textContent = "0 đ";
        $("detailStationCount").textContent = "0";
        $("detailStatus").textContent = "—";
        $("detailStatus").className = "status muted";
        $("stationList").innerHTML = `<div style="text-align:center;padding:20px;color:#8a99a5;">Chưa chọn tuyến nào.</div>`;
        return;
    }

    $("selectedRouteTitle").textContent = `${route.name} — ${route.start} → ${route.end}`;
    $("selectedRouteMeta").textContent = `Tổng chiều dài: ${route.totalDistanceKm} km · ${route.stations.length} trạm dừng.`;
    $("detailCode").textContent = route.code;
    $("detailFare").textContent = money(route.fare);
    $("detailStationCount").textContent = route.stations.length;
    $("detailStatus").textContent = route.status;
    $("detailStatus").className = `status ${route.status === "Hoạt động" ? "active" : "paused"}`;

    const sortedStations = route.stations.slice().sort((a, b) => a.order - b.order);

    $("stationList").innerHTML = sortedStations.length ? sortedStations
        .map((s, idx) => {
            let distBetween = 0;
            if (idx > 0) {
                distBetween = Math.max(0, s.distance - sortedStations[idx - 1].distance).toFixed(1);
            }

            const stepText = idx === 0
                ? "Trạm xuất phát (0 km)"
                : `+${distBetween} km từ trạm trước (${s.distance} km từ đầu)`;

            return `
            <div class="station-item">
                <div class="station-main">
                    <div class="station-number">${idx + 1}</div>
                    <div>
                        <strong>${s.name}</strong>
                        <small>${s.address}</small>
                    </div>
                </div>
                <div class="station-meta">
                    <span>↔ <b>${stepText}</b></span>
                    <div class="station-actions">
                        <button class="mini-btn delete delete-station" data-id="${s.id}">×</button>
                    </div>
                </div>
            </div>`;
        }).join("") : `<div style="text-align:center;padding:20px;color:#8a99a5;">Tuyến chưa có trạm.</div>`;

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
            if ($("routeTotalDistance")) $("routeTotalDistance").value = r.totalDistanceKm;
        }
    } else {
        const nextCode = getNextRouteCode();
        $("routeCode").value = nextCode;
        const numOnly = nextCode.replace(/\D/g, "");
        $("routeName").value = `Tuyến ${numOnly}`;
        if ($("routeTotalDistance")) $("routeTotalDistance").value = 25;
        if ($("routeFare")) $("routeFare").value = 15000;
    }
    openModal("routeModal");
    $("routeStart").focus();
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

    const sorted = route.stations.slice().sort((a, b) => a.order - b.order);
    const refSelect = $("stationRefStop");
    if (refSelect) {
        let options = `<option value="0" data-dist="0">Trạm xuất phát đầu tiên (Mốc 0 km)</option>`;
        sorted.forEach(s => {
            options += `<option value="${s.id}" data-dist="${s.distance}">${s.order}. ${s.name} (${s.distance} km từ đầu)</option>`;
        });
        refSelect.innerHTML = options;

        if (sorted.length > 0) {
            refSelect.value = sorted[sorted.length - 1].id;
        }
    }

    $("stationDistance").value = "2.0";

    openModal("stationModal");
    $("stationName").focus();
}

// Lưu Tuyến
$("routeForm").addEventListener("submit", async e => {
    e.preventDefault();
    const id = $("routeId").value;
    let name = $("routeName").value.trim();
    const start = $("routeStart").value.trim();
    const end = $("routeEnd").value.trim();
    const status = $("routeStatus").value === "Hoạt động";
    const fare = Number($("routeFare").value);
    const distanceKm = $("routeTotalDistance") ? Number($("routeTotalDistance").value) : (Math.round(fare / 1500) || 25);

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

// Thêm Trạm vào Tuyến
$("stationForm").addEventListener("submit", async e => {
    e.preventDefault();
    const route = routes.find(r => r.id === selectedRouteId);
    if (!route) return;

    const name = $("stationName").value.trim();
    const order = Number($("stationOrder").value) || 1;
    const distanceBetween = Number($("stationDistance").value) || 0;

    const refSelect = $("stationRefStop");
    let baseDist = 0;
    if (refSelect && refSelect.selectedIndex >= 0) {
        const selectedOpt = refSelect.options[refSelect.selectedIndex];
        baseDist = Number(selectedOpt.dataset.dist) || 0;
    }

    const finalDistanceFromStart = Number((baseDist + distanceBetween).toFixed(2));

    try {
        const res = await fetch("/api/TuyenXe/them-tram-vao-tuyen", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                RouteId: route.id,
                StopName: name,
                StopOrder: order,
                DistanceKm: finalDistanceFromStart
            })
        });

        const body = await res.json().catch(() => ({}));
        if (res.ok) {
            toast(`✅ Đã thêm "${name}" (+${distanceBetween} km từ mốc)!`);
            closeModal("stationModal");
            await fetchAllData();
        } else {
            toast(body.message || "❌ Lỗi thêm trạm");
        }
    } catch (err) {
        console.error(err);
        toast("❌ Lỗi kết nối");
    }
});

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

// Xử lý nút Đăng xuất popup modal
const logoutBtn = $("logoutBtn");
if (logoutBtn) {
    logoutBtn.addEventListener("click", e => {
        e.preventDefault();
        openModal("logoutModal");
    });
}
if ($("cancelLogoutBtn")) $("cancelLogoutBtn").addEventListener("click", () => closeModal("logoutModal"));
if ($("confirmLogoutBtn")) {
    $("confirmLogoutBtn").addEventListener("click", () => {
        localStorage.removeItem("userSession");
        sessionStorage.clear();
        window.location.href = "../auth/dangnhap.html";
    });
}

document.addEventListener("DOMContentLoaded", () => {
    fetchAllData();
});