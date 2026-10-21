(() => {
  const mapElement = document.getElementById('busMap');
  if (!mapElement || !window.L) return;
  const status = document.getElementById('gpsStatus');
  const search = document.getElementById('stopSearch');
  const results = document.getElementById('stopResults');
  const locate = document.getElementById('locateMeBtn');
  const originInput = document.getElementById('gpsOrigin');
  const destinationInput = document.getElementById('gpsDestination');
  const dateInput = document.getElementById('gpsDepartureDate');
  const tripSearchButton = document.getElementById('gpsTripSearchBtn');
  const tripResults = document.getElementById('gpsTripResults');
  const map = L.map(mapElement).setView([21.0285, 105.8542], 12);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '&copy; OpenStreetMap contributors' }).addTo(map);
  const stopLayer = L.layerGroup().addTo(map), busLayer = L.layerGroup().addTo(map);
  let routeLayer = L.layerGroup().addTo(map);
  let stops = [], stopMarkers = new Map(), busMarkers = new Map(), userMarker = null;
  const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const fmtDate = value => value ? new Date(value).toLocaleString('vi-VN') : 'Chưa có';
  async function getJson(url) { const r = await fetch(url, { headers: { Accept: 'application/json' } }); const data = await r.json().catch(() => ({})); if (!r.ok) throw new Error(data.message || `HTTP ${r.status}`); return data; }
  function nearestStop(lat, lon) {
    let nearest = null, best = Infinity;
    for (const stop of stops) {
      const dLat = (Number(stop.latitude) - lat) * Math.PI / 180, dLon = (Number(stop.longitude) - lon) * Math.PI / 180;
      const a = Math.sin(dLat/2)**2 + Math.cos(lat*Math.PI/180)*Math.cos(Number(stop.latitude)*Math.PI/180)*Math.sin(dLon/2)**2;
      const km = 6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
      if (km < best) { best = km; nearest = stop; }
    }
    return nearest && best < 30 ? { stop: nearest, km: best } : null;
  }
  function showStops() {
    const term = (search.value || '').trim().toLocaleLowerCase('vi');
    const visible = stops.filter(s => `${s.name || ''} ${s.address || ''} ${s.stopCode || ''}`.toLocaleLowerCase('vi').includes(term));
    results.innerHTML = visible.length ? visible.slice(0, 100).map(s => `<button class="stop-result" type="button" data-stop-id="${s.id}"><strong>${escapeHtml(s.name)}</strong><span>${escapeHtml(s.address || 'Chưa có địa chỉ')}</span></button>`).join('') : '<p>Không tìm thấy trạm phù hợp.</p>';
    results.querySelectorAll('[data-stop-id]').forEach(button => button.addEventListener('click', () => { const marker = stopMarkers.get(Number(button.dataset.stopId)); if (marker) { map.setView(marker.getLatLng(), 16); marker.openPopup(); } }));
  }
  async function loadStops() {
    try {
      const data = await getJson('/api/BusStops');
      stops = (Array.isArray(data) ? data : data.data || []).filter(s => s.isActive !== false && s.latitude != null && s.longitude != null && Number.isFinite(Number(s.latitude)) && Number.isFinite(Number(s.longitude)));
      stops.forEach(s => { const marker = L.marker([Number(s.latitude), Number(s.longitude)]).bindPopup(`<strong>${escapeHtml(s.name)}</strong><br>${escapeHtml(s.address || '')}`); marker.addTo(stopLayer); stopMarkers.set(Number(s.id), marker); });
      showStops(); status.textContent = `${stops.length} trạm có tọa độ · GPS xe cập nhật mỗi 5 giây`;
      if (stops.length) map.fitBounds(L.featureGroup([...stopMarkers.values()]).getBounds().pad(0.15), { maxZoom: 13 });
    } catch (_) { status.textContent = 'Không tải được danh sách trạm. Kiểm tra API /api/BusStops.'; results.innerHTML = '<p>Không thể tải dữ liệu trạm.</p>'; }
  }
  async function refreshBuses() {
    try {
      const buses = await getJson('/api/tracking/active'), currentIds = new Set();
      buses.forEach(bus => {
        if (!Number.isFinite(Number(bus.latitude)) || !Number.isFinite(Number(bus.longitude))) return;
        const id = Number(bus.tripId); currentIds.add(id);
        const age = Math.max(0, Math.round((Date.now() - new Date(bus.recordedAtUtc).getTime()) / 1000));
        const near = nearestStop(Number(bus.latitude), Number(bus.longitude));
        const popup = `<strong>Chuyến ${escapeHtml(bus.tripCode || id)}</strong><br>Tốc độ: ${bus.speedKmh == null ? 'Chưa có' : `${Math.round(bus.speedKmh)} km/h`}<br>${near ? `Trạm gần nhất: ${escapeHtml(near.stop.name)} (${near.km.toFixed(1)} km)` : 'Không có trạm gần vị trí này'}<br>Cập nhật ${age} giây trước`;
        if (busMarkers.has(id)) busMarkers.get(id).setLatLng([bus.latitude, bus.longitude]).setPopupContent(popup);
        else busMarkers.set(id, L.circleMarker([bus.latitude, bus.longitude], { radius: 9, color: '#b42318', fillColor: '#f04438', fillOpacity: .9 }).bindPopup(popup).addTo(busLayer));
      });
      for (const [id, marker] of busMarkers) if (!currentIds.has(id)) { busLayer.removeLayer(marker); busMarkers.delete(id); }
    } catch (_) { /* GPS có thể chưa có dữ liệu */ }
  }
  async function searchTrips() {
    const origin = originInput.value.trim(), destination = destinationInput.value.trim(), date = dateInput.value;
    if (!origin || !destination || !date) { tripResults.innerHTML = '<p class="gps-trip-empty">Vui lòng nhập điểm đi, điểm đến và ngày đi.</p>'; return; }
    if (origin.toLocaleLowerCase('vi') === destination.toLocaleLowerCase('vi')) { tripResults.innerHTML = '<p class="gps-trip-empty">Điểm đi và điểm đến phải khác nhau.</p>'; return; }
    tripSearchButton.disabled = true; tripResults.innerHTML = '<p class="gps-trip-empty">Đang tìm chuyến…</p>';
    try {
      const q = new URLSearchParams({ diemDi: origin, diemDen: destination, ngay: date });
      const data = await getJson(`/api/TraCuuTuyenXe/theo-diem?${q}`), trips = Array.isArray(data) ? data : data.data || [];
      if (!trips.length) { tripResults.innerHTML = '<p class="gps-trip-empty">Không tìm thấy chuyến phù hợp trong ngày đã chọn.</p>'; return; }
      tripResults.innerHTML = trips.slice(0, 20).map((t, i) => `<article class="gps-trip-card"><strong>${escapeHtml(t.routeName || t.routeCode || `Chuyến ${t.tripId}`)}</strong><p>${escapeHtml(t.originStopName)} → ${escapeHtml(t.destinationStopName)}</p><p>Khởi hành: ${escapeHtml(fmtDate(t.departureDateTime))}</p><p>Thời lượng: ${Number(t.durationMinutes || 0)} phút · ${Number(t.distanceKm || 0).toFixed(1)} km</p><p>Giá vé: ${Number(t.fare || 0).toLocaleString('vi-VN')} đ · Ghế trống: ${Number(t.availableSeats || 0)}</p><button type="button" data-trip-id="${Number(t.tripId)}" data-origin="${escapeHtml(t.originStopName)}" data-destination="${escapeHtml(t.destinationStopName)}">Xem lộ trình trên bản đồ</button></article>`).join('');
      tripResults.querySelectorAll('[data-trip-id]').forEach(btn => btn.addEventListener('click', () => showTripRoute(Number(btn.dataset.tripId), btn.dataset.origin, btn.dataset.destination)));
    } catch (e) { tripResults.innerHTML = `<p class="gps-trip-empty">Không tìm được chuyến: ${escapeHtml(e.message)}. Kiểm tra API tra cứu chuyến.</p>`; }
    finally { tripSearchButton.disabled = false; }
  }
  async function showTripRoute(tripId, originName, destinationName) {
    try {
      const data = await getJson(`/api/tracking/trips/${tripId}/route`), routeStops = data.stops || [];
      if (routeStops.length < 2) { status.textContent = 'Chuyến này chưa có đủ tọa độ trạm để vẽ lộ trình.'; return; }
      const norm = v => String(v || '').trim().toLocaleLowerCase('vi');
      let start = routeStops.findIndex(s => norm(s.name) === norm(originName));
      let end = routeStops.findIndex(s => norm(s.name) === norm(destinationName));
      if (start < 0) start = 0; if (end < 0 || end <= start) end = routeStops.length - 1;
      const segment = routeStops.slice(start, end + 1).filter(s => Number.isFinite(Number(s.latitude)) && Number.isFinite(Number(s.longitude)));
      if (segment.length < 2) { status.textContent = 'Không đủ tọa độ GPS của các trạm để vẽ lộ trình.'; return; }
      routeLayer.clearLayers();
      const line = L.polyline(segment.map(s => [Number(s.latitude), Number(s.longitude)]), { color: '#175cd3', weight: 5, opacity: .85 }).addTo(routeLayer);
      segment.forEach((s, i) => L.circleMarker([Number(s.latitude), Number(s.longitude)], { radius: 5, color: '#175cd3', fillColor: '#fff', fillOpacity: 1 }).bindPopup(`<strong>${i + 1}. ${escapeHtml(s.name)}</strong><br>Đến: ${escapeHtml(s.arrivalTime || '—')}<br>Rời: ${escapeHtml(s.departureTime || '—')}`).addTo(routeLayer));
      map.fitBounds(line.getBounds().pad(.15)); status.textContent = `Đang hiển thị lộ trình chuyến ${escapeHtml(data.tripCode || tripId)} · ${segment.length} trạm có tọa độ.`;
    } catch (e) { status.textContent = `Không tải được lộ trình: ${e.message}`; }
  }
  search.addEventListener('input', showStops); tripSearchButton.addEventListener('click', searchTrips);
  locate.addEventListener('click', () => {
    if (!navigator.geolocation) { status.textContent = 'Trình duyệt không hỗ trợ định vị.'; return; }
    status.textContent = 'Đang xin quyền truy cập vị trí…';
    navigator.geolocation.getCurrentPosition(pos => { const point = [pos.coords.latitude, pos.coords.longitude]; if (userMarker) userMarker.setLatLng(point); else userMarker = L.marker(point).addTo(map).bindPopup('Vị trí hiện tại của bạn'); map.setView(point, 15); userMarker.openPopup(); status.textContent = 'Đã xác định vị trí của bạn.'; }, err => { status.textContent = err.code === 1 ? 'Bạn chưa cấp quyền định vị.' : 'Không lấy được vị trí. Hãy bật GPS và thử lại.'; }, { enableHighAccuracy: true, timeout: 10000, maximumAge: 15000 });
  });
  const now = new Date(); dateInput.value = `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}-${String(now.getDate()).padStart(2,'0')}`;
  loadStops(); refreshBuses(); window.setInterval(refreshBuses, 5000);
})();
