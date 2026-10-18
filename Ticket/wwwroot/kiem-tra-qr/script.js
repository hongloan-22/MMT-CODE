let selectedTripId = null;
let verifiedTicket = null;
let verifiedQRToken = null;
let isVerifying = false;
let scanProcessing = false;
let scanner = null;
let scannerStarting = false;
let scannerRunning = false;

const tripSelect = document.getElementById("tripSelect");
const qrToken = document.getElementById("qrToken");
const verifyBtn = document.getElementById("verifyBtn");
const useBtn = document.getElementById("useBtn");
const restartScannerBtn = document.getElementById("restartScannerBtn");
const scannerStatus = document.getElementById("scannerStatus");

function getSession() {
    try {
        return JSON.parse(localStorage.getItem("userSession") || "{}");
    } catch {
        return {};
    }
}

function getUserId() {
    const session = getSession();
    return session.userId || session.UserId || "";
}

function showToast(message, duration = 2200) {
    const toast = document.getElementById("appToast");
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add("show");
    clearTimeout(window.appToastTimer);
    window.appToastTimer = setTimeout(() => toast.classList.remove("show"), duration);
}

function headers() {
    return {
        "Content-Type": "application/json",
        "X-User-Id": getUserId()
    };
}

function setScannerStatus(message, type = "info") {
    if (!scannerStatus) return;
    scannerStatus.textContent = message;
    scannerStatus.classList.remove("error", "success");
    if (type === "error" || type === "success") {
        scannerStatus.classList.add(type);
    }
}

function checkLogin() {
    const session = getSession();
    const userId = session.userId || session.UserId || "";
    if (!userId) {
        showToast("⚠️ Vui lòng đăng nhập trước.");
        setTimeout(() => window.location.href = "../auth/dangnhap.html", 700);
        return false;
    }

    const role = String(
        session.role || session.roleCode || session.Role ||
        session.RoleCode || session.roleName || session.RoleName || ""
    ).trim().toUpperCase();

    if (!["ADMIN", "MANAGER", "DRIVER"].includes(role)) {
        showToast("❌ Bạn không có quyền truy cập khu vực kiểm tra vé QR.");
        setTimeout(() => window.location.href = "../index.html", 700);
        return false;
    }
    return true;
}

async function readResponse(response) {
    return response.json().catch(() => ({}));
}

async function loadTrips() {
    const response = await fetch("/api/Ticket/today-trips", { headers: headers() });
    const data = await readResponse(response);
    if (!response.ok) {
        throw new Error(data.message || "Không thể tải danh sách chuyến.");
    }

    tripSelect.innerHTML = '<option value="">-- Chọn chuyến hôm nay --</option>';
    data.forEach(trip => {
        const option = document.createElement("option");
        option.value = trip.tripId;
        option.textContent = `${trip.tripCode} | ${trip.departureTime} | ${trip.routeCode}`;
        tripSelect.appendChild(option);
    });

    if (!data.length) {
        setScannerStatus("Hôm nay không có chuyến được phân công/đang hoạt động.", "error");
    }
}

function resetResult() {
    verifiedTicket = null;
    verifiedQRToken = null;
    document.getElementById("emptyResult").style.display = "block";
    document.getElementById("resultBox").className = "result";
    useBtn.disabled = true;
    useBtn.textContent = "✓ Xác nhận hành khách đã lên xe";
    useBtn.dataset.confirmed = "false";
    useBtn.dataset.confirming = "false";
    useBtn.classList.remove("confirming");
}

function updateVerifyButton() {
    verifyBtn.disabled = isVerifying || !selectedTripId || !qrToken.value.trim();
}

tripSelect.addEventListener("change", () => {
    selectedTripId = Number(tripSelect.value) || null;
    const selected = [...tripSelect.options].find(option => Number(option.value) === selectedTripId);
    const selectedTrip = document.getElementById("selectedTrip");
    selectedTrip.style.display = selectedTripId ? "block" : "none";

    if (selected) {
        document.getElementById("selectedTripCode").textContent = selected.textContent;
        document.getElementById("selectedTripInfo").textContent = "Đang kiểm tra vé cho chuyến này.";
    }

    resetResult();
    updateVerifyButton();
});

qrToken.addEventListener("input", () => {
    // Mỗi khi mã nhập thay đổi, kết quả xác minh trước đó không còn đáng tin.
    if (verifiedTicket || verifiedQRToken) resetResult();
    updateVerifyButton();
});

qrToken.addEventListener("keydown", event => {
    if (event.key === "Enter") {
        event.preventDefault();
        verifyQR();
    }
});

verifyBtn.addEventListener("click", () => verifyQR());

async function verifyQR() {
    if (isVerifying) return;
    const token = qrToken.value.trim();

    if (!selectedTripId) {
        showToast("❌ Hãy chọn chuyến xe.");
        return;
    }
    if (!token) {
        showToast("❌ Hãy nhập hoặc quét mã QR.");
        return;
    }

    isVerifying = true;
    verifiedTicket = null;
    verifiedQRToken = null;
    verifyBtn.disabled = true;
    useBtn.disabled = true;

    try {
        const response = await fetch("/api/Ticket/verify-qr", {
            method: "POST",
            headers: headers(),
            body: JSON.stringify({ qrToken: token, tripId: selectedTripId })
        });
        const data = await readResponse(response);

        if (!response.ok || data.valid === false || !data.ticket) {
            showInvalid(data.message || "Vé không hợp lệ.");
            return;
        }

        verifiedTicket = data.ticket;
        verifiedQRToken = token;
        showValid(verifiedTicket);
    } catch (error) {
        console.error(error);
        showInvalid("Không thể kết nối máy chủ.");
    } finally {
        isVerifying = false;
        updateVerifyButton();
    }
}

function showValid(ticket) {
    document.getElementById("emptyResult").style.display = "none";
    const box = document.getElementById("resultBox");
    box.className = "result valid";
    document.getElementById("resultTitle").textContent = "✓ Vé hợp lệ";
    document.getElementById("ticketCode").textContent = ticket.ticketCode || "—";
    document.getElementById("passengerName").textContent = ticket.passengerName || "—";
    document.getElementById("seatCode").textContent = ticket.seatCode || "—";
    document.getElementById("routeCode").textContent = ticket.routeCode || "—";
    document.getElementById("departureStop").textContent = ticket.departureStop || "—";
    document.getElementById("arrivalStop").textContent = ticket.arrivalStop || "—";
    document.getElementById("ticketStatus").textContent = ticket.status || "VALID";
    document.getElementById("statusMessage").textContent = "Có thể xác nhận hành khách đã lên xe.";
    useBtn.disabled = false;
    useBtn.textContent = "✓ Xác nhận hành khách đã lên xe";
    useBtn.dataset.confirmed = "false";
    useBtn.dataset.confirming = "false";
    useBtn.classList.remove("confirming");
}

function showInvalid(message) {
    verifiedTicket = null;
    verifiedQRToken = null;
    document.getElementById("emptyResult").style.display = "none";
    const box = document.getElementById("resultBox");
    box.className = "result invalid";
    document.getElementById("resultTitle").textContent = "✕ Vé không hợp lệ";
    document.getElementById("ticketCode").textContent = "—";
    document.getElementById("passengerName").textContent = "—";
    document.getElementById("seatCode").textContent = "—";
    document.getElementById("routeCode").textContent = "—";
    document.getElementById("departureStop").textContent = "—";
    document.getElementById("arrivalStop").textContent = "—";
    document.getElementById("ticketStatus").textContent = "INVALID";
    document.getElementById("statusMessage").textContent = message;
    useBtn.disabled = true;
}

useBtn.addEventListener("click", async () => {
    if (!verifiedTicket || !verifiedQRToken || !selectedTripId || useBtn.disabled) return;

    if (useBtn.dataset.confirmed !== "true") {
        useBtn.dataset.confirming = "true";
        useBtn.dataset.confirmed = "true";
        useBtn.textContent = "Bấm lần nữa để xác nhận";
        useBtn.classList.add("confirming");
        setTimeout(() => {
            if (useBtn.dataset.confirmed === "true") {
                useBtn.dataset.confirmed = "false";
                useBtn.dataset.confirming = "false";
                useBtn.textContent = "✓ Xác nhận hành khách đã lên xe";
                useBtn.classList.remove("confirming");
            }
        }, 3000);
        return;
    }

    useBtn.dataset.confirmed = "false";
    useBtn.disabled = true;
    try {
        const response = await fetch(
            `/api/Ticket/${verifiedTicket.ticketId}/use?tripId=${selectedTripId}`,
            {
                method: "POST",
                headers: headers(),
                body: JSON.stringify({ qrToken: verifiedQRToken })
            }
        );
        const data = await readResponse(response);
        if (!response.ok || data.valid === false) {
            throw new Error(data.message || "Không thể xác nhận vé.");
        }

        document.getElementById("ticketStatus").textContent = "USED";
        document.getElementById("statusMessage").textContent = data.message || "Đã xác nhận vé đã sử dụng.";
        document.getElementById("resultBox").className = "result valid";
        useBtn.textContent = "✓ Đã xác nhận sử dụng";
        verifiedTicket = null;
        verifiedQRToken = null;
        qrToken.value = "";
        updateVerifyButton();
        setScannerStatus("Vé đã được xác nhận sử dụng. Có thể chọn ‘Quét mã khác’ để tiếp tục.", "success");
    } catch (error) {
        console.error(error);
        showInvalid(error.message || "Không thể xác nhận vé.");
    }
});

async function stopScanner() {
    const currentScanner = scanner;
    if (!currentScanner) {
        scannerRunning = false;
        scannerStarting = false;
        return;
    }

    try {
        // stop() là bất đồng bộ; cần chờ camera dừng trước khi tạo scanner mới.
        await currentScanner.stop();
    } catch (error) {
        // Có thể scanner chưa bắt đầu hoàn toàn; tiếp tục dọn trạng thái.
        console.debug("Scanner đã dừng hoặc chưa chạy:", error);
    }
    try {
        currentScanner.clear();
    } catch (error) {
        console.debug("Không cần clear scanner:", error);
    }

    if (scanner === currentScanner) scanner = null;
    scannerRunning = false;
    scannerStarting = false;
}

async function handleDecodedQR(decodedText) {
    if (scanProcessing) return;
    scanProcessing = true;
    qrToken.value = decodedText;
    updateVerifyButton();
    setScannerStatus("Đã đọc mã QR. Đang dừng camera và xác minh vé...");
    restartScannerBtn.style.display = "none";

    await stopScanner();
    restartScannerBtn.textContent = "Quét mã khác";
    restartScannerBtn.style.display = "inline-block";

    try {
        if (!selectedTripId) {
            resetResult();
            showToast("⚠️ Hãy chọn chuyến xe trước khi kiểm tra mã QR.");
            setScannerStatus("Đã đọc mã QR. Hãy chọn chuyến rồi bấm Kiểm tra vé.", "error");
            return;
        }
        await verifyQR();
    } finally {
        scanProcessing = false;
    }
}

async function startScanner() {
    if (scannerStarting || scannerRunning) return;
    if (typeof Html5Qrcode === "undefined") {
        setScannerStatus("Không tải được thư viện quét QR. Bạn vẫn có thể nhập mã thủ công.", "error");
        restartScannerBtn.textContent = "Thử mở camera lại";
        restartScannerBtn.style.display = "inline-block";
        return;
    }
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        setScannerStatus("Trình duyệt không hỗ trợ camera hoặc trang không chạy trên HTTPS. Bạn có thể nhập mã QR thủ công.", "error");
        restartScannerBtn.textContent = "Thử mở camera lại";
        restartScannerBtn.style.display = "inline-block";
        return;
    }

    scannerStarting = true;
    setScannerStatus("Đang mở camera...");
    const currentScanner = new Html5Qrcode("reader");
    scanner = currentScanner;
    try {
        await currentScanner.start(
            { facingMode: "environment" },
            { fps: 10, qrbox: { width: 240, height: 240 } },
            decodedText => { void handleDecodedQR(decodedText); },
            () => { /* Không xử lý lỗi lặp ở từng frame quét. */ }
        );

        // Nếu callback đã dừng scanner trong lúc start hoàn tất,
        // không được đánh dấu scanner cũ thành đang chạy trở lại.
        if (scanner !== currentScanner) return;

        scannerRunning = true;
        scannerStarting = false;
        setScannerStatus("Camera đã sẵn sàng. Đưa mã QR vào khung quét.", "success");
        restartScannerBtn.style.display = "none";
    } catch (error) {
        console.warn("Không mở được camera:", error);
        if (scanner === currentScanner) {
            await stopScanner();
        }
        scannerRunning = false;
        scannerStarting = false;
        setScannerStatus("Không thể mở camera. Hãy cấp quyền sử dụng camera hoặc nhập mã QR thủ công.", "error");
        restartScannerBtn.textContent = "Thử mở camera lại";
        restartScannerBtn.style.display = "inline-block";
    }
}

restartScannerBtn.addEventListener("click", async () => {
    if (scannerStarting || scannerRunning) await stopScanner();
    qrToken.value = "";
    resetResult();
    updateVerifyButton();
    restartScannerBtn.style.display = "none";
    await startScanner();
});

async function init() {
    if (!checkLogin()) return;

    try {
        await loadTrips();
    } catch (error) {
        console.error(error);
        showToast("❌ " + (error.message || "Không thể tải chuyến xe."));
        setScannerStatus(error.message || "Không thể tải chuyến xe.", "error");
    }

    // Logout đã được xử lý trong inline script của index.html.
    await startScanner();
}

void init();
