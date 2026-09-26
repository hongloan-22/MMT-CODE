const routes = [
  {
    id: 1, code: "T01", name: "Tuyến 01", start: "Bến xe Trung tâm",
    end: "Đại học ICTU", fare: 12000, status: "Hoạt động",
    stations: [
      {id:101, code:"TR001", name:"Bến xe Trung tâm", address:"P. Hoàng Văn Thụ, TP. Thái Nguyên", order:1, lat:21.5947, lng:105.8442, distance:0},
      {id:102, code:"TR002", name:"Chợ Thái", address:"Đường Lương Ngọc Quyến", order:2, lat:21.5908, lng:105.8454, distance:1.1},
      {id:103, code:"TR003", name:"Bệnh viện Trung ương", address:"Đường Lương Ngọc Quyến", order:3, lat:21.5881, lng:105.8328, distance:1.7},
      {id:104, code:"TR004", name:"Cổng trường ICTU", address:"Đường Z115, TP. Thái Nguyên", order:4, lat:21.5917, lng:105.8097, distance:2.8}
    ]
  },
  {
    id: 2, code: "T02", name: "Tuyến 02", start: "Bến xe phía Nam",
    end: "Khu công nghiệp Sông Công", fare: 15000, status: "Hoạt động",
    stations: [
      {id:201, code:"TR010", name:"Bến xe phía Nam", address:"P. Tân Lập, TP. Thái Nguyên", order:1, lat:21.5631, lng:105.8352, distance:0},
      {id:202, code:"TR011", name:"Ngã ba Tân Long", address:"Đường Cách Mạng Tháng Tám", order:2, lat:21.5561, lng:105.8244, distance:1.4},
      {id:203, code:"TR012", name:"KCN Sông Công", address:"TP. Sông Công", order:3, lat:21.4852, lng:105.8447, distance:7.8}
    ]
  },
  {
    id: 3, code: "T03", name: "Tuyến 03", start: "Đại học ICTU",
    end: "Khu du lịch Hồ Núi Cốc", fare: 18000, status: "Tạm dừng",
    stations: [
      {id:301, code:"TR020", name:"Đại học ICTU", address:"Đường Z115", order:1, lat:21.5917, lng:105.8097, distance:0},
      {id:302, code:"TR021", name:"Ngã tư Quán Triều", address:"P. Quán Triều", order:2, lat:21.6031, lng:105.7871, distance:2.5},
      {id:303, code:"TR022", name:"Cổng Hồ Núi Cốc", address:"Đường vào Hồ Núi Cốc", order:3, lat:21.6485, lng:105.7173, distance:9.2}
    ]
  }
];

let selectedRouteId = 1;

const $ = id => document.getElementById(id);
const money = n => new Intl.NumberFormat("vi-VN").format(n) + " đ";

function updateStats() {
  $("routeCount").textContent = routes.length;
  $("activeRouteCount").textContent = routes.filter(r => r.status === "Hoạt động").length;
  const stationSet = new Set(routes.flatMap(r => r.stations.map(s => s.code)));
  $("stationCount").textContent = stationSet.size;
  const avg = routes.reduce((sum, r) => sum + Number(r.fare), 0) / (routes.length || 1);
  $("avgFare").textContent = money(Math.round(avg));
}

function renderRoutes() {
  const q = $("routeSearch").value.trim().toLowerCase();
  const status = $("routeStatusFilter").value;
  const rows = routes.filter(r => {
    const matchesText = [r.code, r.name, r.start, r.end].some(v => v.toLowerCase().includes(q));
    return matchesText && (status === "all" || r.status === status);
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
          <button class="mini-btn edit-route" data-id="${r.id}" title="Sửa">✎</button>
          <button class="mini-btn delete delete-route" data-id="${r.id}" title="Xóa">×</button>
        </div>
      </td>
    </tr>`).join("") : `<tr><td colspan="7" style="text-align:center;padding:30px;color:#8a99a5">Không tìm thấy tuyến phù hợp.</td></tr>`;

  document.querySelectorAll("tr[data-route]").forEach(row => {
    row.addEventListener("click", e => {
      if (e.target.closest(".row-actions")) return;
      selectedRouteId = Number(row.dataset.route);
      renderRoutes();
      renderSelectedRoute();
    });
  });

  document.querySelectorAll(".edit-route").forEach(btn => btn.addEventListener("click", e => {
    e.stopPropagation(); openRouteModal(Number(btn.dataset.id));
  }));

  document.querySelectorAll(".delete-route").forEach(btn => btn.addEventListener("click", e => {
    e.stopPropagation();
    deleteRoute(Number(btn.dataset.id));
  }));
}

function renderSelectedRoute() {
  const route = routes.find(r => r.id === selectedRouteId) || routes[0];
  if (!route) return;

  $("selectedRouteTitle").textContent = `${route.name} — ${route.start} → ${route.end}`;
  $("selectedRouteMeta").textContent = `Danh sách ${route.stations.length} trạm theo thứ tự phục vụ của tuyến.`;
  $("detailCode").textContent = route.code;
  $("detailFare").textContent = money(route.fare);
  $("detailStationCount").textContent = route.stations.length;
  $("detailStatus").textContent = route.status;
  $("detailStatus").className = `status ${route.status === "Hoạt động" ? "active" : "paused"}`;

  $("stationList").innerHTML = route.stations
    .slice().sort((a,b) => a.order - b.order)
    .map((s, index) => `
      <div class="station-item">
        <div class="station-main">
          <div class="station-number">${s.order}</div>
          <div>
            <strong>${s.name} <span style="color:#a2b0b9;font-weight:500">(${s.code})</span></strong>
            <small>${s.address}</small>
          </div>
        </div>
        <div class="station-meta">
          <span>📍 <b>${s.lat ?? "—"}, ${s.lng ?? "—"}</b></span>
          <span>↔ <b>${s.distance ? s.distance + " km" : "Điểm đầu"}</b></span>
          <div class="station-actions">
            <button class="mini-btn edit-station" data-id="${s.id}" title="Sửa trạm">✎</button>
            <button class="mini-btn delete delete-station" data-id="${s.id}" title="Xóa trạm">×</button>
          </div>
        </div>
      </div>`).join("");

  document.querySelectorAll(".edit-station").forEach(btn => btn.addEventListener("click", () => openStationModal(Number(btn.dataset.id))));
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
    $("routeId").value = r.id;
    $("routeCode").value = r.code;
    $("routeName").value = r.name;
    $("routeStart").value = r.start;
    $("routeEnd").value = r.end;
    $("routeFare").value = r.fare;
    $("routeStatus").value = r.status;
  }
  openModal("routeModal");
  $("routeCode").focus();
}

function openStationModal(id = null) {
  const route = routes.find(r => r.id === selectedRouteId);
  if (!route) return;
  $("stationForm").reset();
  $("stationId").value = "";
  $("stationModalTitle").textContent = id ? "Chỉnh sửa trạm" : `Thêm trạm vào ${route.code}`;

  if (id) {
    const s = route.stations.find(x => x.id === id);
    $("stationId").value = s.id;
    $("stationCode").value = s.code;
    $("stationName").value = s.name;
    $("stationAddress").value = s.address;
    $("stationOrder").value = s.order;
    $("stationLat").value = s.lat ?? "";
    $("stationLng").value = s.lng ?? "";
    $("stationDistance").value = s.distance ?? "";
  } else {
    $("stationOrder").value = route.stations.length + 1;
  }
  openModal("stationModal");
  $("stationCode").focus();
}

$("routeForm").addEventListener("submit", e => {
  e.preventDefault();
  const id = Number($("routeId").value);
  const data = {
    code: $("routeCode").value.trim(),
    name: $("routeName").value.trim(),
    start: $("routeStart").value.trim(),
    end: $("routeEnd").value.trim(),
    fare: Number($("routeFare").value),
    status: $("routeStatus").value
  };

  if (id) {
    Object.assign(routes.find(r => r.id === id), data);
    selectedRouteId = id;
    toast("Đã cập nhật thông tin tuyến.");
  } else {
    const newId = Math.max(0, ...routes.map(r => r.id)) + 1;
    routes.push({id:newId, ...data, stations:[]});
    selectedRouteId = newId;
    toast("Đã thêm tuyến mới.");
  }

  closeModal("routeModal");
  updateStats(); renderRoutes(); renderSelectedRoute();
});

$("stationForm").addEventListener("submit", e => {
  e.preventDefault();
  const route = routes.find(r => r.id === selectedRouteId);
  const id = Number($("stationId").value);
  const data = {
    code: $("stationCode").value.trim(),
    name: $("stationName").value.trim(),
    address: $("stationAddress").value.trim(),
    order: Number($("stationOrder").value),
    lat: $("stationLat").value ? Number($("stationLat").value) : null,
    lng: $("stationLng").value ? Number($("stationLng").value) : null,
    distance: $("stationDistance").value ? Number($("stationDistance").value) : 0
  };

  if (id) {
    Object.assign(route.stations.find(s => s.id === id), data);
    toast("Đã cập nhật thông tin trạm.");
  } else {
    const newId = Date.now();
    route.stations.push({id:newId, ...data});
    toast("Đã thêm trạm vào tuyến.");
  }

  route.stations.sort((a,b) => a.order - b.order);
  closeModal("stationModal");
  updateStats(); renderRoutes(); renderSelectedRoute();
});

function deleteRoute(id) {
  const r = routes.find(x => x.id === id);
  if (!r) return;
  if (!confirm(`Xóa tuyến ${r.code} - ${r.name}?`)) return;
  const index = routes.findIndex(x => x.id === id);
  routes.splice(index, 1);
  selectedRouteId = routes[Math.max(0, index - 1)]?.id || routes[0]?.id;
  updateStats(); renderRoutes(); renderSelectedRoute();
  toast("Đã xóa tuyến.");
}

function deleteStation(id) {
  const route = routes.find(r => r.id === selectedRouteId);
  const s = route.stations.find(x => x.id === id);
  if (!s) return;
  if (!confirm(`Xóa trạm "${s.name}" khỏi tuyến ${route.code}?`)) return;
  route.stations = route.stations.filter(x => x.id !== id);
  updateStats(); renderRoutes(); renderSelectedRoute();
  toast("Đã xóa trạm khỏi tuyến.");
}

function toast(message) {
  const el = $("toast");
  el.textContent = message;
  el.classList.add("show");
  clearTimeout(window.toastTimer);
  window.toastTimer = setTimeout(() => el.classList.remove("show"), 2200);
}

$("addRouteBtn").addEventListener("click", () => openRouteModal());
$("addStationBtn").addEventListener("click", () => openStationModal());
$("routeSearch").addEventListener("input", renderRoutes);
$("routeStatusFilter").addEventListener("change", renderRoutes);

document.querySelectorAll("[data-close]").forEach(btn => {
  btn.addEventListener("click", () => closeModal(btn.dataset.close));
});

document.querySelectorAll(".modal-backdrop").forEach(backdrop => {
  backdrop.addEventListener("click", e => {
    if (e.target === backdrop) closeModal(backdrop.id);
  });
});

updateStats();
renderRoutes();
renderSelectedRoute();
