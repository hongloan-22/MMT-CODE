// @ts-nocheck
const $ = id => document.getElementById(id);

function toast(msg) {
    const el = $("toast");
    if (!el) return;
    el.textContent = msg;
    el.classList.add("show");
    clearTimeout(window.toastTimer);
    window.toastTimer = setTimeout(() => el.classList.remove("show"), 2200);
}

// 1. Tải danh sách user từ PhanQuyenController
async function fetchUsers() {
    const tbody = $("userTableBody");
    try {
        const raw = localStorage.getItem("userSession");
        const user = raw ? JSON.parse(raw) : null;
        const currentUserId = user?.userId || "";

        const res = await fetch("/api/PhanQuyen/danh-sach-user", {
            headers: { "X-User-Id": currentUserId }
        });

        if (res.status === 403) {
            alert("Tài khoản không có quyền xem thông tin phân quyền!");
            window.location.replace("../quan-ly-tuyen-tram/index.html");
            return;
        }

        if (!res.ok) throw new Error("Lỗi mạng");
        const users = await res.json();

        if (!users.length) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:25px; color:#8a99a5;">Không có tài khoản nào.</td></tr>`;
            return;
        }

        tbody.innerHTML = users.map(u => `
            <tr>
                <td><strong>${u.userId}</strong></td>
                <td>${u.fullName || "—"}</td>
                <td>${u.email}</td>
                <td>${u.phone || "—"}</td>
                <td><span class="status active">${u.roleName} (${u.roleCode})</span></td>
                <td>
                    <select onchange="changeRole('${u.userId}', this.value)" style="height:36px; border:1px solid #dce7ee; border-radius:8px; padding:0 8px; font-weight:600; color:#172b3a; background:#fff; cursor:pointer;">
                        <option value="1" ${u.roleId === 1 ? "selected" : ""}>1 - Quản trị (ADMIN)</option>
                        <option value="2" ${u.roleId === 2 ? "selected" : ""}>2 - Quản lý (MANAGER)</option>
                        <option value="3" ${u.roleId === 3 ? "selected" : ""}>3 - Tài xế (DRIVER)</option>
                        <option value="4" ${u.roleId === 4 ? "selected" : ""}>4 - Người dùng (USER)</option>
                    </select>
                </td>
            </tr>
        `).join("");
    } catch (err) {
        console.error(err);
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:25px; color:#ef6262;">Lỗi kết nối tải dữ liệu.</td></tr>`;
    }
}

// 2. Cập nhật quyền trực tiếp
window.changeRole = async function (userId, newRoleId) {
    try {
        const raw = localStorage.getItem("userSession");
        const user = raw ? JSON.parse(raw) : null;
        const currentUserId = user?.userId || "";

        const res = await fetch("/api/PhanQuyen/cap-nhat-quyen", {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                "X-User-Id": currentUserId
            },
            body: JSON.stringify({
                userId: userId,
                roleId: parseInt(newRoleId, 10)
            })
        });

        const data = await res.json().catch(() => ({}));
        if (res.ok) {
            toast("✅ Cập nhật quyền thành công!");
            fetchUsers();
        } else {
            toast(`❌ ${data.message || "Lỗi cập nhật vai trò"}`);
        }
    } catch {
        toast("❌ Lỗi kết nối máy chủ");
    }
};

// 3. Khởi tạo giao diện
document.addEventListener("DOMContentLoaded", () => {
    const raw = localStorage.getItem("userSession");
    if (raw) {
        try {
            const u = JSON.parse(raw);
            if ($("sidebarUserName")) $("sidebarUserName").textContent = u.fullName || "Admin";
            if ($("sidebarUserRole")) $("sidebarUserRole").textContent = u.roleName || "Quản trị";
        } catch { }
    }

    const logoutBtn = $("logoutBtn");
    const logoutModal = $("logoutModal");
    const cancelBtn = $("cancelLogoutBtn");
    const confirmBtn = $("confirmLogoutBtn");

    const openModal = () => { if (logoutModal) logoutModal.classList.add("open"); };
    const closeModal = () => { if (logoutModal) logoutModal.classList.remove("open"); };

    if (logoutBtn) logoutBtn.addEventListener("click", e => { e.preventDefault(); openModal(); });
    if (cancelBtn) cancelBtn.addEventListener("click", closeModal);
    if (logoutModal) logoutModal.addEventListener("click", e => { if (e.target === logoutModal) closeModal(); });

    if (confirmBtn) {
        confirmBtn.addEventListener("click", () => {
            localStorage.removeItem("userSession");
            sessionStorage.clear();
            window.location.replace("../auth/dangnhap.html");
        });
    }

    fetchUsers();
});