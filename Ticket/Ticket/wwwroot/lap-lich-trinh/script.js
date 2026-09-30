const API = {
  schedules: "/api/LichTrinh",
  trips: "/api/LichTrinh/trips",
  routes: "/api/TuyenXe"
};

const state = { schedules: [], trips: [], routes: [], selectedTripId: null };
const $ = id => document.getElementById(id);
const escapeHtml = value => String(value ?? "").replace(/[&<>'"]/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;","'":"&#39;",'"':"&quot;"}[c]));
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
const statusLabel = s => ({SCHEDULED:"Đã lên lịch", ACTIVE:"Đang chạy", CANCELLED:"Đã hủy"}[s] || s || "—");
const statusClass = s => ({SCHEDULED:"scheduled", ACTIVE:"active", CANCELLED:"cancelled"}[s] || "muted");

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
    if (state.schedules.length) selectTrip(state.schedules[0].tripId);
  } catch (error) {
    $("errorState").textContent = error.message + " Hãy kiểm tra API ASP.NET Core đang chạy.";
    $("errorState").classList.remove("hidden");
  } finally {
    setLoading(false);
  }
}

function setLoading(show) {
  $("loading").classList.toggle("hidden", !show);
  $("tableWrap").classList.toggle("hidden", show);
}

function buildFilters() {
  const routeFilter = $("routeFilter");
  routeFilter.innerHTML = '<option value="all">Tất cả tuyến</option>' + state.routes.map(r =>
    `<option value="${r.id}">${escapeHtml(r.routeName)}</option>`).join("");
}

function tripForSchedule(s) { return state.trips.find(t => t.id === s.tripId) || s.trip || {}; }
function routeForTrip(t) { return state.routes.find(r => r.id === t.routeId) || t.route || {}; }

function schedulesForTrip(tripId) {
  return state.schedules.filter(s => Number(s.tripId) === Number(tripId)).sort((a,b) => a.stopOrder - b.stopOrder);
}

function updateStats() {
  const today = todayKey();
  const todayTrips = new Set(state.trips.filter(t => dateKey(t.tripDate) === today).map(t => t.id));
  const activeRoutes = state.routes.filter(r => r.isActive).length;
  $("todayCount").textContent = todayTrips.size;
  $("routeCount").textContent = activeRoutes;
  $("scheduleCount").textContent = state.schedules.length;
  $("activeCount").textContent = state.schedules.filter(s => (s.status || "SCHEDULED").toUpperCase() === "SCHEDULED").length;
}

function renderTable() {
  const q = $("searchInput").value.trim().toLowerCase();
  const dateFilter = $("dateFilter").value;
  const routeId = $("routeFilter").value;
  const status = $("statusFilter").value;
  const today = todayKey();

  const rows = state.schedules.filter(s => {
    const trip = tripForSchedule(s);
    const route = routeForTrip(trip);
    const stop = s.busStop || {};
    const text = [trip.tripCode, route.routeName, stop.name].join(" ").toLowerCase();
    const tripDate = dateKey(trip.tripDate);
    const matchesDate = dateFilter === "all" || (dateFilter === "today" && tripDate === today) || (dateFilter === "upcoming" && tripDate >= today);
    return text.includes(q) && matchesDate && (routeId === "all" || String(trip.routeId) === routeId) && (status === "all" || String(s.status || "SCHEDULED").toUpperCase() === status);
  });

  $("emptyState").classList.toggle("hidden", rows.length !== 0);
  $("tableWrap").classList.toggle("hidden", rows.length === 0);
  $("scheduleTableBody").innerHTML = rows.map(s => {
    const trip = tripForSchedule(s);
    const route = routeForTrip(trip);
    const stop = s.busStop || {};
    return `<tr class="${Number(state.selectedTripId) === Number(s.tripId) ? "selected" : ""}" data-trip-id="${s.tripId}">
      <td><span class="trip-code">${escapeHtml(trip.tripCode || `TRIP-${s.tripId}`)}</span></td>
      <td><span class="route-name">${escapeHtml(route.routeName || "Chưa xác định")}</span></td>
      <td>${dateText(trip.tripDate)}</td>
      <td><strong>${timeText(trip.departureTime)}</strong></td>
      <td>${escapeHtml(stop.name || `Trạm #${s.stopId}`)}<div class="muted-text">Trạm ${s.stopOrder}</div></td>
      <td>${timeText(s.arrivalTime)} → ${timeText(s.departureTime)}</td>
      <td><span class="status ${statusClass(s.status)}">${escapeHtml(statusLabel(s.status))}</span></td>
      <td><div class="row-actions"><button class="mini-btn edit" data-id="${s.id}" title="Sửa">✎</button><button class="mini-btn delete" data-id="${s.id}" title="Xóa">×</button></div></td>
    </tr>`;
  }).join("");

  document.querySelectorAll("tr[data-trip-id]").forEach(row => row.addEventListener("click", e => {
    if (e.target.closest(".row-actions")) return;
    selectTrip(Number(row.dataset.tripId));
    renderTable();
  }));
  document.querySelectorAll(".edit").forEach(btn => btn.addEventListener("click", e => { e.stopPropagation(); openModal(Number(btn.dataset.id)); }));
  document.querySelectorAll(".delete").forEach(btn => btn.addEventListener("click", e => { e.stopPropagation(); deleteSchedule(Number(btn.dataset.id)); }));
}

function selectTrip(tripId) {
  state.selectedTripId = tripId;
  const trip = state.trips.find(t => Number(t.id) === Number(tripId));
  const route = trip ? routeForTrip(trip) : {};
  const schedules = schedulesForTrip(tripId);
  $("detailTitle").textContent = trip ? `${trip.tripCode} — ${route.routeName || "Chưa xác định tuyến"}` : "Chọn một lịch trình để xem chi tiết";
  $("detailMeta").textContent = trip ? `${dateText(trip.tripDate)} · Xuất phát ${timeText(trip.departureTime)} · ${schedules.length} điểm dừng` : "Thời gian biểu các trạm được sắp xếp theo thứ tự phục vụ.";
  const status = schedules.some(s => s.status === "ACTIVE") ? "ACTIVE" : (schedules[0]?.status || "SCHEDULED");
  $("detailStatus").textContent = trip ? statusLabel(status) : "—";
  $("detailStatus").className = `status ${trip ? statusClass(status) : "muted"}`;
  $("timeline").classList.toggle("empty-timeline", !trip || !schedules.length);
  $("timeline").innerHTML = trip && schedules.length ? schedules.map(s => `
    <div class="timeline-item">
      <div class="timeline-time">${timeText(s.arrivalTime)}</div>
      <div class="timeline-line"><span class="timeline-dot"></span></div>
      <div class="timeline-info"><strong>${escapeHtml(s.busStop?.name || `Trạm #${s.stopId}`)}</strong><small>Rời trạm: ${timeText(s.departureTime)} · Thứ tự ${s.stopOrder}</small></div>
      <div class="timeline-meta">${escapeHtml(statusLabel(s.status))}</div>
    </div>`).join("") : `<div>Chưa có điểm dừng trong lịch trình này.</div>`;
}

function populateTrips(selectedId = null) {
  $("tripId").innerHTML = state.trips.length ? state.trips.map(t => {
    const route = routeForTrip(t);
    return `<option value="${t.id}" ${Number(t.id) === Number(selectedId) ? "selected" : ""}>${escapeHtml(t.tripCode)} · ${escapeHtml(route.routeName || "Chưa có tuyến")} · ${dateText(t.tripDate)} ${timeText(t.departureTime)}</option>`;
  }).join("") : '<option value="">Chưa có chuyến xe</option>';
  updateStopsForTrip();
}

function updateStopsForTrip(selectedStopId = null) {
  const trip = state.trips.find(t => Number(t.id) === Number($("tripId").value));
  const route = trip ? routeForTrip(trip) : {};
  const stops = (route.routeStops || []).slice().sort((a,b) => a.stopOrder - b.stopOrder);
  $("stopId").innerHTML = stops.length ? stops.map(rs => `<option value="${rs.stopId}" ${Number(rs.stopId) === Number(selectedStopId) ? "selected" : ""}>${rs.stopOrder}. ${escapeHtml(rs.busStop?.name || `Trạm #${rs.stopId}`)}</option>`).join("") : '<option value="">Tuyến chưa có trạm</option>';
  if (!$('stopOrder').value) $('stopOrder').value = stops.length + 1;
}

function openModal(id = null) {
  if (!state.trips.length) { toast("Chưa có chuyến xe để lập lịch trình."); return; }
  $("scheduleForm").reset();
  $("scheduleId").value = "";
  $("modalTitle").textContent = id ? "Chỉnh sửa lịch trình" : "Lập lịch trình";
  const current = id ? state.schedules.find(s => Number(s.id) === Number(id)) : null;
  populateTrips(current?.tripId || state.selectedTripId || state.trips[0].id);
  if (current) {
    $("tripId").value = current.tripId;
    updateStopsForTrip(current.stopId);
    $("scheduleId").value = current.id;
    $("stopOrder").value = current.stopOrder;
    $("arrivalTime").value = timeText(current.arrivalTime);
    $("departureTime").value = timeText(current.departureTime);
    $("scheduleStatus").value = current.status || "SCHEDULED";
  }
  $("scheduleModal").classList.remove("hidden");
}
function closeModal() { $("scheduleModal").classList.add("hidden"); }

async function saveSchedule(e) {
  e.preventDefault();
  const id = Number($("scheduleId").value);
  const data = {
    id: id || 0,
    tripId: Number($("tripId").value),
    stopId: Number($("stopId").value),
    stopOrder: Number($("stopOrder").value),
    arrivalTime: `${$("arrivalTime").value}:00`,
    departureTime: `${$("departureTime").value}:00`,
    status: $("scheduleStatus").value
  };
  if (!data.tripId || !data.stopId) { toast("Vui lòng chọn chuyến xe và điểm dừng."); return; }
  $("saveBtn").disabled = true;
  try {
    const res = await fetch(id ? `${API.schedules}/${id}` : API.schedules, {
      method: id ? "PUT" : "POST", headers: {"Content-Type":"application/json"}, body: JSON.stringify(data)
    });
    const body = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(body.message || "Không thể lưu lịch trình.");
    closeModal();
    toast(id ? "Đã cập nhật lịch trình." : "Đã lập lịch trình mới.");
    await loadData();
    selectTrip(data.tripId);
    renderTable();
  } catch (error) { toast(error.message); }
  finally { $("saveBtn").disabled = false; }
}

async function deleteSchedule(id) {
  const item = state.schedules.find(s => Number(s.id) === Number(id));
  if (!item || !confirm("Bạn có chắc muốn xóa lịch trình này?")) return;
  try {
    const res = await fetch(`${API.schedules}/${id}`, {method:"DELETE"});
    const body = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(body.message || "Không thể xóa lịch trình.");
    toast("Đã xóa lịch trình.");
    await loadData();
  } catch (error) { toast(error.message); }
}

function toast(message) {
  const el = $("toast"); el.textContent = message; el.classList.add("show");
  clearTimeout(window.toastTimer); window.toastTimer = setTimeout(() => el.classList.remove("show"), 2400);
}

$("addScheduleBtn").addEventListener("click", () => openModal());
$("closeModal").addEventListener("click", closeModal);
$("cancelModal").addEventListener("click", closeModal);
$("scheduleForm").addEventListener("submit", saveSchedule);
$("tripId").addEventListener("change", () => updateStopsForTrip());
["searchInput","dateFilter","routeFilter","statusFilter"].forEach(id => $(id).addEventListener(id === "searchInput" ? "input" : "change", renderTable));
$("scheduleModal").addEventListener("click", e => { if (e.target.id === "scheduleModal") closeModal(); });

loadData();
