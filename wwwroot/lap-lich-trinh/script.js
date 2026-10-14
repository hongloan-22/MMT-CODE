// @ts-nocheck
(() => {
    const API = {
        schedules: "/api/LichTrinh",
        trips: "/api/LichTrinh/trips",
        routes: "/api/TuyenXe",
        quickCreate: "/api/LichTrinh/quick-create",
        deleteTrip: "/api/LichTrinh/trip"
    };

    const state = { schedules: [], trips: [], routes: [], selectedTripId: null };
    const byId = id => document.getElementById(id);
    const escapeHtml = value => String(value ?? "").replace(/[&<>'"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[c]));
    const pad = n => String(n).padStart(2, "0");

    const timeText = value => {
        if (!value) return "—";
        const match = String(value).match(/(\d{1,2}):(\d{2})/);
        return match ? `${pad(match[1])}:${match[2]}` : value;
    };

    const dateText = value => {
        if (!value) return "—";
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return String(value).slice(0, 10);
        return d.toLocaleDateString("vi-VN");
    };

    const dateKey = value => value ? String(value).slice(0, 10) : "";
    const todayKey = () => new Date().toISOString().slice(0, 10);
    const statusLabel = s => ({ SCHEDULED: "Đã lên lịch", ACTIVE: "Đang chạy", CANCELLED: "Đã hủy" }[s] || s || "—");
    const statusClass = s => ({ SCHEDULED: "scheduled", ACTIVE: "active", CANCELLED: "cancelled" }[s] || "muted");

    // Thuật toán tìm số hiệu trống nhỏ nhất (Reset về 1 nếu trống, tự động lấp chỗ trống 1, 2, 3...)
    function getNextAvailableTripCode() {
        const usedNumbers = new Set();
        state.trips.forEach(t => {
            const code = String(t.tripCode || "");
            const match = code.match(/TRIP[-_]?(\d+)/i) || code.match(/(\d+)/);
            if (match) {
                const num = parseInt(match[1], 10);
                if (!isNaN(num) && num > 0) usedNumbers.add(num);
            }
        });

        let candidate = 1;
        while (usedNumbers.has(candidate)) {
            candidate++;
        }
        return `TRIP${pad(candidate)}`;
    }

    async function getJson(url) {
        const res = await fetch(url);
        if (!res.ok) throw new Error(`Không thể tải dữ liệu (${res.status}).`);
        return res.json();
    }

    async function loadData() {
        setLoading(true);
        try {
            const [schedules, trips, routes] = await Promise.all([
                getJson(API.schedules),
                getJson(API.trips),
                getJson(API.routes)
            ]);
            state.schedules = Array.isArray(schedules) ? schedules : [];
            state.trips = Array.isArray(trips) ? trips : [];
            state.routes = Array.isArray(routes) ? routes : [];

            buildFilters();
            updateStats();
            renderTable();

            if (state.trips.length > 0) {
                const initialId = (state.selectedTripId && state.trips.some(t => t.id === state.selectedTripId))
                    ? state.selectedTripId
                    : state.trips[0].id;
                selectTrip(initialId);
            } else {
                state.selectedTripId = null;
                selectTrip(null);
            }
        } catch (error) {
            const errBox = byId("errorState");
            if (errBox) {
                errBox.textContent = error.message + " Hãy kiểm tra API ASP.NET Core đang chạy.";
                errBox.classList.remove("hidden");
            }
        } finally {
            setLoading(false);
        }
    }

    function setLoading(show) {
        const loading = byId("loading");
        const tableWrap = byId("tableWrap");
        if (loading) loading.classList.toggle("hidden", !show);
        if (tableWrap) tableWrap.classList.toggle("hidden", show);
    }

    function buildFilters() {
        const routeFilter = byId("routeFilter");
        if (routeFilter) {
            routeFilter.innerHTML = '<option value="all">Tất cả tuyến</option>' + state.routes.map(r =>
                `<option value="${r.id || r.routeId}">${escapeHtml(r.routeName || r.name)}</option>`).join("");
        }
    }

    function tripForSchedule(s) { return state.trips.find(t => t.id === s.tripId) || s.trip || {}; }
    function routeForTrip(t) { return state.routes.find(r => (r.id || r.routeId) === t.routeId) || t.route || {}; }

    function schedulesForTrip(tripId) {
        return state.schedules.filter(s => Number(s.tripId) === Number(tripId)).sort((a, b) => a.stopOrder - b.stopOrder);
    }

    function updateStats() {
        const today = todayKey();
        const todayTrips = new Set(state.trips.filter(t => dateKey(t.tripDate) === today).map(t => t.id));
        const activeRoutes = state.routes.filter(r => r.isActive !== false).length;
        if (byId("todayCount")) byId("todayCount").textContent = todayTrips.size;
        if (byId("routeCount")) byId("routeCount").textContent = activeRoutes;
        if (byId("scheduleCount")) byId("scheduleCount").textContent = state.schedules.length;
        if (byId("activeCount")) byId("activeCount").textContent = state.schedules.filter(s => (s.status || "SCHEDULED").toUpperCase() === "SCHEDULED").length;
    }

    function groupTripsForTable() {
        const tripMap = new Map();

        state.trips.forEach(t => {
            const route = routeForTrip(t);
            tripMap.set(Number(t.id), {
                tripId: Number(t.id),
                tripCode: t.tripCode || `TRIP-${t.id}`,
                routeId: t.routeId,
                routeName: route.routeName || route.name || "Chưa xác định tuyến",
                tripDate: t.tripDate,
                departureTime: t.departureTime,
                status: "SCHEDULED",
                stops: []
            });
        });

        state.schedules.forEach(s => {
            const tripId = Number(s.tripId);
            if (!tripMap.has(tripId)) {
                const trip = tripForSchedule(s);
                const route = routeForTrip(trip);
                tripMap.set(tripId, {
                    tripId: tripId,
                    tripCode: trip.tripCode || `TRIP-${tripId}`,
                    routeId: trip.routeId,
                    routeName: route.routeName || route.name || "Chưa xác định tuyến",
                    tripDate: trip.tripDate,
                    departureTime: trip.departureTime || s.departureTime,
                    status: s.status || "SCHEDULED",
                    stops: []
                });
            }
            tripMap.get(tripId).stops.push(s);
        });

        tripMap.forEach(item => {
            item.stops.sort((a, b) => a.stopOrder - b.stopOrder);
            if (item.stops.some(s => s.status === "ACTIVE")) {
                item.status = "ACTIVE";
            } else if (item.stops.length > 0) {
                item.status = item.stops[0].status || "SCHEDULED";
            }
        });

        return Array.from(tripMap.values());
    }

    function renderTable() {
        const searchInput = byId("searchInput");
        const dateFilter = byId("dateFilter");
        const routeFilter = byId("routeFilter");
        const statusFilter = byId("statusFilter");

        const q = searchInput ? searchInput.value.trim().toLowerCase() : "";
        const dVal = dateFilter ? dateFilter.value : "all";
        const rVal = routeFilter ? routeFilter.value : "all";
        const sVal = statusFilter ? statusFilter.value : "all";
        const today = todayKey();

        const allTrips = groupTripsForTable();

        const rows = allTrips.filter(t => {
            const tripCode = String(t.tripCode || "").toLowerCase();
            const routeName = String(t.routeName || "").toLowerCase();
            const stopNames = t.stops.map(s => s.busStop?.name || "").join(" ").toLowerCase();

            let matchesSearch = false;
            if (!q) {
                matchesSearch = true;
            } else {
                // Xử lý khi người dùng chỉ gõ số (vd: "2", "3", "03")
                const isNumeric = /^\d+$/.test(q);
                if (isNumeric) {
                    const tripNumMatch = tripCode.match(/\d+/);
                    const tripNum = tripNumMatch ? parseInt(tripNumMatch[0], 10) : null;
                    const searchNum = parseInt(q, 10);

                    // Khớp chính xác số hiệu chuyến hoặc chuỗi mã chuyến chứa cụm số đó
                    matchesSearch = (tripNum !== null && tripNum === searchNum) || tripCode.includes(q);
                } else {
                    // Khi gõ chữ thông thường (vd: "trip", "gia lâm", "yên nghĩa")
                    const fullText = [tripCode, routeName, stopNames].join(" ");
                    matchesSearch = fullText.includes(q);
                }
            }

            const tripDate = dateKey(t.tripDate);
            const matchesDate = dVal === "all" || (dVal === "today" && tripDate === today) || (dVal === "upcoming" && tripDate >= today);
            return matchesSearch && matchesDate && (rVal === "all" || String(t.routeId) === rVal) && (sVal === "all" || String(t.status || "SCHEDULED").toUpperCase() === sVal);
        });

        if (byId("emptyState")) byId("emptyState").classList.toggle("hidden", rows.length !== 0);
        if (byId("tableWrap")) byId("tableWrap").classList.toggle("hidden", rows.length === 0);

        const tbody = byId("scheduleTableBody") || document.querySelector("tbody");
        if (!tbody) return;

        tbody.innerHTML = rows.map(t => {
            const firstStop = t.stops[0]?.busStop?.name;
            const lastStop = t.stops[t.stops.length - 1]?.busStop?.name;
            const routeDisplay = t.stops.length > 0
                ? `<strong>${escapeHtml(firstStop)}</strong> → <strong>${escapeHtml(lastStop)}</strong>`
                : '<span class="muted-text">Chưa lập trạm nào</span>';

            return `<tr class="${Number(state.selectedTripId) === Number(t.tripId) ? "selected" : ""}" data-trip-id="${t.tripId}" style="cursor:pointer;">
        <td><span class="trip-code" style="color:#0d6efd;font-weight:700;">${escapeHtml(t.tripCode)}</span></td>
        <td><span class="route-name" style="font-weight:600;">${escapeHtml(t.routeName)}</span></td>
        <td>${dateText(t.tripDate)}</td>
        <td><strong>${timeText(t.departureTime)}</strong></td>
        <td>
          <div>${routeDisplay}</div>
          <small style="color:#6c757d;">${t.stops.length} điểm dừng phục vụ</small>
        </td>
        <td>${t.stops.length > 0 ? `${timeText(t.stops[0].arrivalTime)} →${timeText(t.stops[t.stops.length - 1].departureTime)}` : "—"}</td>
        <td><span class="status ${statusClass(t.status)}">${escapeHtml(statusLabel(t.status))}</span></td>
        <td>
          <button class="mini-btn delete-trip-btn" data-trip-id="${t.tripId}" title="Xóa toàn bộ lịch trình chuyến này" style="color:#dc3545;border:none;background:none;cursor:pointer;font-size:18px;line-height:1;padding:4px 8px;">×</button>
        </td>
      </tr>`;
        }).join("");

        tbody.querySelectorAll("tr[data-trip-id]").forEach(row => row.addEventListener("click", e => {
            if (e.target.closest(".delete-trip-btn")) return;
            selectTrip(Number(row.dataset.tripId));
            renderTable();
        }));

        tbody.querySelectorAll(".delete-trip-btn").forEach(btn => btn.addEventListener("click", e => {
            e.stopPropagation();
            deleteWholeTrip(Number(btn.dataset.tripId));
        }));
    }

    function selectTrip(tripId) {
        state.selectedTripId = tripId;

        if (!tripId) {
            if (byId("detailTitle")) byId("detailTitle").textContent = "Chọn một lịch trình để xem chi tiết";
            if (byId("detailMeta")) byId("detailMeta").textContent = "Thời gian biểu các trạm được sắp xếp theo thứ tự phục vụ.";
            if (byId("detailStatus")) {
                byId("detailStatus").textContent = "—";
                byId("detailStatus").className = "status muted";
            }
            const timeline = byId("timeline");
            if (timeline) {
                timeline.className = "timeline empty-timeline";
                timeline.innerHTML = '<div style="padding:20px; color:#8a99a5;">Chưa chọn chuyến xe.</div>';
            }
            return;
        }

        const trip = state.trips.find(t => Number(t.id) === Number(tripId));
        const route = trip ? routeForTrip(trip) : {};
        const schedules = schedulesForTrip(tripId);

        if (byId("detailTitle")) byId("detailTitle").textContent = trip ? `${trip.tripCode} — ${route.routeName || route.name || "Chưa xác định tuyến"}` : "Chọn một lịch trình để xem chi tiết";
        if (byId("detailMeta")) byId("detailMeta").textContent = trip ? `${dateText(trip.tripDate)} · Xuất phát ${timeText(trip.departureTime)} · ${schedules.length} điểm dừng` : "Thời gian biểu các trạm được sắp xếp theo thứ tự phục vụ.";

        const status = schedules.some(s => s.status === "ACTIVE") ? "ACTIVE" : (schedules[0]?.status || "SCHEDULED");
        if (byId("detailStatus")) {
            byId("detailStatus").textContent = trip ? statusLabel(status) : "—";
            byId("detailStatus").className = `status ${trip ? statusClass(status) : "muted"}`;
        }

        const timeline = byId("timeline");
        if (timeline) {
            timeline.classList.toggle("empty-timeline", !trip || !schedules.length);
            timeline.innerHTML = trip && schedules.length ? schedules.map(s => `
        <div class="timeline-item">
          <div class="timeline-time">${timeText(s.arrivalTime)}</div>
          <div class="timeline-line"><span class="timeline-dot"></span></div>
          <div class="timeline-info">
            <strong>${escapeHtml(s.busStop?.name || `Trạm #${s.stopId}`)}</strong>
            <small>Rời trạm: ${timeText(s.departureTime)} · Thứ tự ${s.stopOrder}</small>
          </div>
          <div class="timeline-meta">
            <span class="status ${statusClass(s.status)}">${escapeHtml(statusLabel(s.status))}</span>
          </div>
        </div>`).join("") : `<div style="padding:20px;color:#8a99a5;">Chưa có điểm dừng nào được lập cho chuyến này.</div>`;
        }
    }

    function openModal() {
        if (!state.routes.length) {
            toast("Bạn chưa có Tuyến xe nào. Vui lòng sang trang Tuyến & Trạm tạo tuyến trước!");
            return;
        }

        const routeSelect = byId("routeSelectModal");
        if (routeSelect) {
            routeSelect.innerHTML = state.routes.map(r => {
                const id = r.id || r.routeId;
                const code = r.routeCode ? `[${r.routeCode}] ` : "";
                const name = r.routeName || r.name;
                return `<option value="${id}">${escapeHtml(code)}${escapeHtml(name)}</option>`;
            }).join("");

            updateDefaultDistance();
            routeSelect.onchange = updateDefaultDistance;
        }

        // Tự động gán mã chuyến nhỏ nhất còn trống và khóa ô nhập (không cho sửa tay gây nhảy cóc)
        const tripCodeInput = byId("tripCodeModal");
        if (tripCodeInput) {
            tripCodeInput.value = getNextAvailableTripCode();
            tripCodeInput.readOnly = true;
            tripCodeInput.style.backgroundColor = "#f1f3f5";
            tripCodeInput.style.cursor = "not-allowed";
        }

        if (byId("tripDateModal")) byId("tripDateModal").value = todayKey();
        if (byId("departureTimeModal")) byId("departureTimeModal").value = "08:00";

        const modal = byId("scheduleModal");
        if (modal) modal.classList.remove("hidden");
    }

    function updateDefaultDistance() {
        const routeSelect = byId("routeSelectModal");
        const distInput = byId("tripDistanceModal");
        if (!routeSelect || !distInput) return;
        const r = state.routes.find(x => (x.id || x.routeId) === Number(routeSelect.value));
        if (r) {
            distInput.value = r.totalDistanceKm || 25;
        }
    }

    function closeModal() {
        const modal = byId("scheduleModal");
        if (modal) modal.classList.add("hidden");
    }

    async function handleCreateSchedule(e) {
        e.preventDefault();

        const routeSelect = byId("routeSelectModal");
        const tripCodeInput = byId("tripCodeModal");
        const tripDateInput = byId("tripDateModal");
        const departureTimeInput = byId("departureTimeModal");
        const distInput = byId("tripDistanceModal");
        const saveBtn = byId("saveBtn");

        const payload = {
            routeId: Number(routeSelect ? routeSelect.value : 0),
            tripCode: tripCodeInput ? tripCodeInput.value.trim() : "",
            tripDate: tripDateInput ? tripDateInput.value : todayKey(),
            departureTime: departureTimeInput ? departureTimeInput.value : "08:00",
            totalDistanceKm: distInput ? Number(distInput.value) : 0
        };

        if (!payload.routeId) {
            toast("Vui lòng chọn tuyến xe.");
            return;
        }

        if (saveBtn) saveBtn.disabled = true;
        try {
            const res = await fetch(API.quickCreate, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const body = await res.json().catch(() => ({}));
            if (!res.ok) throw new Error(body.message || "Không thể tạo lịch trình.");

            closeModal();
            toast("Đã lập lịch trình thành công!");
            state.selectedTripId = body.tripId;
            await loadData();
        } catch (err) {
            toast(err.message);
        } finally {
            if (saveBtn) saveBtn.disabled = false;
        }
    }

    async function deleteWholeTrip(tripId) {
        if (!confirm("Bạn có chắc chắn muốn xóa toàn bộ lịch trình chuyến xe này?")) return;
        try {
            const res = await fetch(`${API.deleteTrip}/${tripId}`, { method: "DELETE" });
            const body = await res.json().catch(() => ({}));
            if (!res.ok) throw new Error(body.message || "Không thể xóa.");

            toast("Đã xóa lịch trình thành công.");
            state.selectedTripId = null;
            await loadData();
        } catch (err) {
            toast(err.message);
        }
    }

    function toast(message) {
        const el = byId("toast");
        if (el) {
            el.textContent = message;
            el.classList.add("show");
            clearTimeout(window.toastTimer);
            window.toastTimer = setTimeout(() => el.classList.remove("show"), 2500);
        } else {
            alert(message);
        }
    }

    document.addEventListener("DOMContentLoaded", () => {
        const addBtn = byId("addScheduleBtn");
        const closeBtn = byId("closeModal");
        const cancelBtn = byId("cancelModal");
        const form = byId("scheduleForm");
        const modal = byId("scheduleModal");

        if (addBtn) addBtn.addEventListener("click", openModal);
        if (closeBtn) closeBtn.addEventListener("click", closeModal);
        if (cancelBtn) cancelBtn.addEventListener("click", closeModal);
        if (form) form.addEventListener("submit", handleCreateSchedule);

        if (modal) {
            modal.addEventListener("click", e => {
                if (e.target.id === "scheduleModal") closeModal();
            });
        }

        ["searchInput", "dateFilter", "routeFilter", "statusFilter"].forEach(id => {
            const elem = byId(id);
            if (elem) elem.addEventListener(id === "searchInput" ? "input" : "change", renderTable);
        });

        loadData();
    });
    document.querySelectorAll("#logoutBtn, .btn-logout, .logout-btn, a[href*='dangnhap']").forEach(btn => {
        btn.addEventListener("click", e => {
            e.preventDefault();
            e.stopPropagation();
            openModal("logoutModal");
        });
    });
    if ($("cancelLogoutBtn")) $("cancelLogoutBtn").addEventListener("click", () => closeModal("logoutModal"));
    if ($("confirmLogoutBtn")) {
        $("confirmLogoutBtn").addEventListener("click", () => {
            localStorage.removeItem("userSession");
            sessionStorage.clear();
            window.location.href = "../auth/dangnhap.html";
        });
    }
})();