
"use strict";

const $ = id => document.getElementById(id);

let drivers = [];

// =====================================================
// LẤY TÀI KHOẢN ĐANG ĐĂNG NHẬP
// =====================================================

function getCurrentUser() {
    try {
        const raw = localStorage.getItem("userSession");
        return raw ? JSON.parse(raw) : null;
    } catch (error) {
        console.error("Không đọc được userSession:", error);
        return null;
    }
}

function getUserId() {
    const user = getCurrentUser();
    return user?.userId || user?.UserId || "";
}

function getAuthHeaders() {
    return {
        "X-User-Id": getUserId()
    };
}

// =====================================================
// BẢO VỆ TRANG VÀ HIỂN THỊ THÔNG TIN TÀI KHOẢN
// =====================================================

function checkPageAccess() {
    const user = getCurrentUser();

    if (!user) {
        window.location.replace("../auth/dangnhap.html");
        return false;
    }

    const role = String(
        user.role ||
        user.Role ||
        user.roleCode ||
        user.RoleCode ||
        user.roleName ||
        user.RoleName ||
        ""
    ).trim().toUpperCase();

    const roleId = Number(user.roleId || user.RoleId || 0);

    const isAllowed =
        role === "ADMIN" ||
        role === "MANAGER" ||
        role === "QUẢN TRỊ VIÊN" ||
        role === "QUẢN LÝ VẬN HÀNH" ||
        roleId === 1 ||
        roleId === 2;

    if (!isAllowed) {
        window.location.replace("../index.html");
        return false;
    }

    const fullName = user.fullName || user.FullName || "Người dùng";

    const initials = fullName
        .trim()
        .split(/\s+/)
        .map(part => part[0])
        .join("")
        .substring(0, 2)
        .toUpperCase();

    if ($("topAvatar")) {
        $("topAvatar").textContent = initials;
    }

    if ($("topUserRole")) {
        $("topUserRole").textContent =
            role === "ADMIN" || roleId === 1
                ? "Quản trị hệ thống"
                : "Quản lý vận hành";
    }

    return true;
}

// =====================================================
// CHỐNG LỖI HTML KHI HIỂN THỊ DỮ LIỆU
// =====================================================

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

// =====================================================
// TẢI TÀI XẾ TỪ API ĐÃ CÓ
// GET /api/Bus/staff
// API trả về tài khoản có ROLE = DRIVER và còn hoạt động
// =====================================================

async function loadDrivers() {
    const tbody = $("driverTableBody");
    const refreshButton = $("refreshDriversBtn");

    if (tbody) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="staff-message">
                    Đang tải danh sách tài xế...
                </td>
            </tr>`;
    }

    if (refreshButton) {
        refreshButton.disabled = true;
    }

    try {
        const response = await fetch("/api/Bus/staff", {
            method: "GET",
            headers: getAuthHeaders()
        });

        const data = await response.json().catch(() => []);

        if (!response.ok) {
            const message =
                data.message ||
                "Không thể tải danh sách tài xế.";

            throw new Error(message);
        }

        drivers = Array.isArray(data) ? data : [];

        updateDriverStats();
        renderDrivers();

    } catch (error) {
        console.error("Lỗi tải danh sách tài xế:", error);

        if (tbody) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="staff-message" style="color:#ef6262">
                        ${escapeHtml(error.message || "Không thể tải danh sách tài xế.")}
                        <br><br>
                        Hãy kiểm tra đăng nhập và quyền truy cập API.
                    </td>
                </tr>`;
        }

        if ($("driverCount")) {
            $("driverCount").textContent = "Không tải được dữ liệu";
        }

    } finally {
        if (refreshButton) {
            refreshButton.disabled = false;
        }
    }
}

// =====================================================
// CẬP NHẬT CÁC THẺ THỐNG KÊ
// =====================================================

function updateDriverStats() {
    const assigned = drivers.filter(
        driver => driver.assignment != null
    ).length;

    $("totalDrivers").textContent = drivers.length;
    $("assignedDrivers").textContent = assigned;
    $("unassignedDrivers").textContent = drivers.length - assigned;
}

// =====================================================
// HIỂN THỊ DANH SÁCH TÀI XẾ
// =====================================================

function renderDrivers() {
    const tbody = $("driverTableBody");
    if (!tbody) return;

    const keyword = ($("driverSearch")?.value || "")
        .trim()
        .toLowerCase();

    const filtered = drivers.filter(driver => {
        const assignment = driver.assignment;

        const searchableText = [
            driver.fullName,
            driver.email,
            driver.phone,
            driver.roleName,
            driver.roleCode,
            assignment?.busCode,
            assignment?.assignmentType
        ].filter(Boolean).join(" ").toLowerCase();

        return !keyword || searchableText.includes(keyword);
    });

    $("driverCount").textContent =
        `${filtered.length} / ${drivers.length} tài xế`;

    if (!filtered.length) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="staff-message">
                    ${drivers.length
                ? "Không tìm thấy tài xế phù hợp."
                : "Chưa có tài khoản đang hoạt động được phân quyền DRIVER."}
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = filtered.map((driver, index) => {
        const assignment = driver.assignment;

        let assignmentHtml = `
            <span class="unassigned-badge">
                Chưa phân công xe
            </span>`;

        if (assignment) {
            const assignmentType =
                String(assignment.assignmentType || "").toUpperCase();

            const position = assignmentType === "ATTENDANT"
                ? "Phụ xe"
                : "Tài xế";

            assignmentHtml = `
                <span class="assignment-badge">
                    ${escapeHtml(position)}
                    ${assignment.busCode
                    ? " · " + escapeHtml(assignment.busCode)
                    : ""}
                </span>`;
        }

        return `
            <tr>
                <td>${index + 1}</td>

                <td>
                    <strong>${escapeHtml(driver.fullName || "—")}</strong>
                </td>

                <td>${escapeHtml(driver.email || "—")}</td>

                <td>${escapeHtml(driver.phone || "—")}</td>

                <td>
                    <span class="driver-role">DRIVER · Tài xế</span>
                </td>

                <td>${assignmentHtml}</td>
            </tr>`;
    }).join("");
}

// =====================================================
// SỰ KIỆN TÌM KIẾM VÀ LÀM MỚI
// =====================================================

$("driverSearch")?.addEventListener("input", renderDrivers);

$("refreshDriversBtn")?.addEventListener("click", loadDrivers);

// =====================================================
// KHỞI ĐỘNG TRANG
// =====================================================

document.addEventListener("DOMContentLoaded", async () => {
    if (!checkPageAccess()) return;

    await loadDrivers();
});
