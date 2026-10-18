// @ts-nocheck

const $ = id => document.getElementById(id);

let users = [];
let roles = [];


// =========================================================
// UTILITY
// =========================================================

function toast(message) {

    const el = $("toast");

    if (!el) return;

    el.textContent = message;

    el.classList.add("show");

    clearTimeout(window.toastTimer);

    window.toastTimer =
        setTimeout(
            () => el.classList.remove("show"),
            2500
        );
}


// =========================================================
// SESSION
// =========================================================

function getCurrentUser() {

    try {

        const raw =
            localStorage.getItem("userSession");

        return raw
            ? JSON.parse(raw)
            : null;

    }
    catch {

        return null;
    }
}


function getCurrentUserId() {

    const user =
        getCurrentUser();

    return user?.userId || "";
}


function getAuthHeaders(includeJson = false) {

    const headers = {};

    if (includeJson) {

        headers["Content-Type"] =
            "application/json";
    }

    headers["X-User-Id"] =
        getCurrentUserId();

    return headers;
}


// =========================================================
// OPEN / CLOSE MODAL
// =========================================================

function openModal(id) {

    const modal = $(id);

    if (modal) {

        modal.classList.add("open");
    }
}


function closeModal(id) {

    const modal = $(id);

    if (modal) {

        modal.classList.remove("open");
    }
}


// =========================================================
// LOAD ROLES
// =========================================================

async function fetchRoles() {

    try {

        const res =
            await fetch(
                "/api/PhanQuyen/danh-sach-role",
                {
                    headers:
                        getAuthHeaders()
                }
            );

        if (res.status === 403) {

            toast(
                "❌ Bạn không có quyền quản lý tài khoản."
            );

            setTimeout(() => {

                window.location.replace(
                    "../quan-ly-tuyen-tram/index.html"
                );

            }, 500);

            return false;
        }

        if (!res.ok) {

            throw new Error(
                "Không tải được danh sách role."
            );
        }

        roles =
            await res.json();

        const select =
            $("userRole");

        if (!select) return true;

        select.innerHTML =
            roles.map(role => `
                <option value="${role.roleId}">
                    ${role.roleName} (${role.roleCode})
                </option>
            `).join("");

        return true;

    }
    catch (err) {

        console.error(err);

        toast(
            "❌ Không thể tải danh sách vai trò."
        );

        return false;
    }
}


// =========================================================
// LOAD USERS
// =========================================================

async function fetchUsers() {

    const tbody =
        $("userTableBody");

    try {

        const res =
            await fetch(
                "/api/PhanQuyen/danh-sach-user",
                {
                    headers:
                        getAuthHeaders()
                }
            );

        if (res.status === 403) {

            toast(
                "❌ Tài khoản không có quyền xem thông tin quản trị."
            );

            setTimeout(() => {

                window.location.replace(
                    "../quan-ly-tuyen-tram/index.html"
                );

            }, 500);

            return;
        }

        if (!res.ok) {

            throw new Error(
                "Không tải được danh sách tài khoản."
            );
        }

        users =
            await res.json();

        renderUsers();

    }
    catch (err) {

        console.error(err);

        if (tbody) {

            tbody.innerHTML = `
                <tr>
                    <td colspan="7"
                        style="
                        text-align:center;
                        padding:25px;
                        color:#ef6262;
                        ">
                        Không thể tải dữ liệu tài khoản.
                    </td>
                </tr>
            `;
        }

    }
}


// =========================================================
// RENDER USERS
// =========================================================

function renderUsers() {

    const tbody =
        $("userTableBody");

    if (!tbody) return;

    const keyword =
        ($("userSearch")?.value || "")
            .trim()
            .toLowerCase();

    const statusFilter =
        $("statusFilter")?.value ||
        "all";

    const filtered =
        users.filter(user => {

            const text = [
                user.userId,
                user.fullName,
                user.email,
                user.phone,
                user.roleName,
                user.roleCode
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase();

            const matchKeyword =
                !keyword ||
                text.includes(keyword);

            const userStatus =
                user.status || "ACTIVE";

            const matchStatus =
                statusFilter === "all" ||
                userStatus === statusFilter;

            return matchKeyword &&
                matchStatus;
        });


    if (!filtered.length) {

        tbody.innerHTML = `
            <tr>
                <td colspan="7"
                    style="
                    text-align:center;
                    padding:25px;
                    color:#8a99a5;
                    ">
                    Không tìm thấy tài khoản.
                </td>
            </tr>
        `;

        return;
    }


    const currentUserId =
        getCurrentUserId();


    tbody.innerHTML =
        filtered.map(user => {

            const isActive =
                user.isActive === true &&
                String(user.status)
                    .toUpperCase() === "ACTIVE";

            const isSelf =
                user.userId ===
                currentUserId;


            const statusText =
                isActive
                    ? "Đang hoạt động"
                    : "Đã khóa";


            const statusClass =
                isActive
                    ? "active"
                    : "paused";


            return `
                <tr>

                    <td>
                        <strong>
                            ${escapeHtml(user.userId || "—")}
                        </strong>
                    </td>

                    <td>
                        ${escapeHtml(user.fullName || "—")}
                    </td>

                    <td>
                        ${escapeHtml(user.email || "—")}
                    </td>

                    <td>
                        ${escapeHtml(user.phone || "—")}
                    </td>

                    <td>

                        <span class="status active">

                            ${escapeHtml(
                user.roleName ||
                "Hành khách"
            )}

                            (${escapeHtml(
                user.roleCode ||
                "USER"
            )})

                        </span>

                    </td>

                    <td>

                        <span class="status ${statusClass}">
                            ${statusText}
                        </span>

                    </td>

                    <td>

                        <div
                            class="row-actions"
                            style="
                            gap:6px;
                            flex-wrap:wrap;
                            ">

                            <button
                                class="mini-btn edit-user"
                                data-id="${escapeHtml(user.userId)}"
                                title="Sửa tài khoản">

                                ✎

                            </button>

                            ${isActive
                    ? `
                                    <button
                                        class="mini-btn delete lock-user"
                                        data-id="${escapeHtml(user.userId)}"
                                        title="${isSelf
                        ? "Không thể tự khóa tài khoản"
                        : "Khóa tài khoản"
                    }"
                                        ${isSelf ? "disabled" : ""}>

                                        🔒

                                    </button>
                                `
                    : `
                                    <button
                                        class="mini-btn unlock-user"
                                        data-id="${escapeHtml(user.userId)}"
                                        title="Mở khóa tài khoản">

                                        🔓

                                    </button>
                                `
                }

                        </div>

                    </td>

                </tr>
            `;

        }).join("");


    // =====================================================
    // EVENTS
    // =====================================================

    document
        .querySelectorAll(".edit-user")
        .forEach(btn => {

            btn.addEventListener(
                "click",
                () => {

                    const user =
                        users.find(
                            x =>
                                x.userId ===
                                btn.dataset.id
                        );

                    if (user) {

                        openEditUserModal(
                            user
                        );
                    }

                }
            );

        });


    document
        .querySelectorAll(".lock-user")
        .forEach(btn => {

            btn.addEventListener(
                "click",
                () => {

                    lockUser(
                        btn.dataset.id
                    );

                }
            );

        });


    document
        .querySelectorAll(".unlock-user")
        .forEach(btn => {

            btn.addEventListener(
                "click",
                () => {

                    unlockUser(
                        btn.dataset.id
                    );

                }
            );

        });

}


// =========================================================
// ESCAPE HTML
// =========================================================

function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


// =========================================================
// OPEN CREATE MODAL
// =========================================================

function openCreateUserModal() {

    $("userForm").reset();

    $("editingUserId").value = "";

    $("userModalTitle").textContent =
        "Tạo tài khoản";


    $("passwordGroup").style.display =
        "block";

    $("newPasswordGroup").style.display =
        "none";

    $("statusGroup").style.display =
        "none";

    $("activeGroup").style.display =
        "none";


    $("userPassword").required =
        true;


    if (roles.length > 0) {

        const userRole =
            roles.find(
                x =>
                    String(x.roleCode).toUpperCase()
                    === "USER"
            );

        if (userRole) {

            $("userRole").value =
                userRole.roleId;

        }
        else {

            $("userRole").value =
                roles[0].roleId;
        }
    }


    openModal(
        "userModal"
    );

    $("userFullName").focus();
}


// =========================================================
// OPEN EDIT MODAL
// =========================================================

function openEditUserModal(user) {

    $("userForm").reset();

    $("editingUserId").value =
        user.userId;

    $("userModalTitle").textContent =
        "Chỉnh sửa tài khoản";


    $("userFullName").value =
        user.fullName || "";

    $("userEmail").value =
        user.email || "";

    $("userPhone").value =
        user.phone || "";

    $("userRole").value =
        user.roleId || "";


    $("userStatus").value =
        String(
            user.status || "ACTIVE"
        ).toUpperCase();


    $("userIsActive").value =
        user.isActive
            ? "true"
            : "false";


    $("passwordGroup").style.display =
        "none";

    $("newPasswordGroup").style.display =
        "block";

    $("statusGroup").style.display =
        "block";

    $("activeGroup").style.display =
        "block";


    $("userPassword").required =
        false;


    openModal(
        "userModal"
    );

    $("userFullName").focus();
}


// =========================================================
// SAVE USER
// =========================================================

$("userForm")
    ?.addEventListener(
        "submit",
        async event => {

            event.preventDefault();


            const editingUserId =
                $("editingUserId").value.trim();


            const fullName =
                $("userFullName")
                    .value.trim();

            const email =
                $("userEmail")
                    .value.trim();

            const phone =
                $("userPhone")
                    .value.trim();

            const roleId =
                Number(
                    $("userRole").value
                );


            if (!fullName) {

                toast(
                    "❌ Vui lòng nhập họ tên."
                );

                return;
            }


            if (!email) {

                toast(
                    "❌ Vui lòng nhập email."
                );

                return;
            }


            if (!roleId) {

                toast(
                    "❌ Vui lòng chọn vai trò."
                );

                return;
            }


            // =================================================
            // CREATE
            // =================================================

            if (!editingUserId) {

                const password =
                    $("userPassword")
                        .value;


                if (!password) {

                    toast(
                        "❌ Vui lòng nhập mật khẩu."
                    );

                    return;
                }


                try {

                    const res =
                        await fetch(
                            "/api/PhanQuyen/tao-tai-khoan",
                            {
                                method: "POST",

                                headers:
                                    getAuthHeaders(true),

                                body:
                                    JSON.stringify({
                                        FullName:
                                            fullName,

                                        Email:
                                            email,

                                        Password:
                                            password,

                                        Phone:
                                            phone,

                                        RoleId:
                                            roleId
                                    })
                            }
                        );


                    const data =
                        await res
                            .json()
                            .catch(
                                () => ({})
                            );


                    if (!res.ok) {

                        toast(
                            `❌ ${data.message ||
                            "Không thể tạo tài khoản."
                            }`
                        );

                        return;
                    }


                    toast(
                        "✅ Tạo tài khoản thành công."
                    );

                    closeModal(
                        "userModal"
                    );

                    await fetchUsers();

                }
                catch (err) {

                    console.error(err);

                    toast(
                        "❌ Lỗi kết nối máy chủ."
                    );
                }

                return;
            }


            // =================================================
            // UPDATE
            // =================================================

            const newPassword =
                $("userNewPassword")
                    .value;


            const status =
                $("userStatus").value;


            const isActive =
                $("userIsActive").value
                === "true";


            try {

                const res =
                    await fetch(
                        `/api/PhanQuyen/cap-nhat-tai-khoan/${encodeURIComponent(editingUserId)}`,
                        {
                            method: "PUT",

                            headers:
                                getAuthHeaders(true),

                            body:
                                JSON.stringify({

                                    FullName:
                                        fullName,

                                    Email:
                                        email,

                                    Phone:
                                        phone,

                                    RoleId:
                                        roleId,

                                    Status:
                                        status,

                                    IsActive:
                                        isActive,

                                    NewPassword:
                                        newPassword ||
                                        null
                                })
                        }
                    );


                const data =
                    await res
                        .json()
                        .catch(
                            () => ({})
                        );


                if (!res.ok) {

                    toast(
                        `❌ ${data.message ||
                        "Không thể cập nhật tài khoản."
                        }`
                    );

                    return;
                }


                toast(
                    "✅ Cập nhật tài khoản thành công."
                );

                closeModal(
                    "userModal"
                );

                await fetchUsers();

            }
            catch (err) {

                console.error(err);

                toast(
                    "❌ Lỗi kết nối máy chủ."
                );
            }

        }
    );


// =========================================================
// LOCK USER
// =========================================================

async function lockUser(userId) {

    const user =
        users.find(
            x => x.userId === userId
        );

    if (!user) return;


    if (userId === getCurrentUserId()) {

        toast(
            "❌ Không thể tự khóa tài khoản Admin."
        );

        return;
    }


    const confirmed = await SmartbusDialog.confirm(
        `Bạn có chắc muốn khóa tài khoản "${user.fullName}" không?`,
        { title: "Xác nhận khóa tài khoản", confirmText: "Khóa tài khoản", danger: true }
    );


    if (!confirmed)
        return;


    try {

        const res =
            await fetch(
                `/api/PhanQuyen/khoa-tai-khoan/${encodeURIComponent(userId)}`,
                {
                    method: "PUT",
                    headers:
                        getAuthHeaders()
                }
            );


        const data =
            await res
                .json()
                .catch(
                    () => ({})
                );


        if (!res.ok) {

            toast(
                `❌ ${data.message ||
                "Không thể khóa tài khoản."
                }`
            );

            return;
        }


        toast(
            "✅ Đã khóa tài khoản."
        );

        await fetchUsers();

    }
    catch (err) {

        console.error(err);

        toast(
            "❌ Lỗi kết nối máy chủ."
        );
    }
}


// =========================================================
// UNLOCK USER
// =========================================================

async function unlockUser(userId) {

    const user =
        users.find(
            x => x.userId === userId
        );

    if (!user) return;


    const confirmed = await SmartbusDialog.confirm(
        `Mở khóa tài khoản "${user.fullName}"?`,
        { title: "Xác nhận mở khóa tài khoản", confirmText: "Mở khóa" }
    );


    if (!confirmed)
        return;


    try {

        const res =
            await fetch(
                `/api/PhanQuyen/mo-khoa-tai-khoan/${encodeURIComponent(userId)}`,
                {
                    method: "PUT",
                    headers:
                        getAuthHeaders()
                }
            );


        const data =
            await res
                .json()
                .catch(
                    () => ({})
                );


        if (!res.ok) {

            toast(
                `❌ ${data.message ||
                "Không thể mở khóa tài khoản."
                }`
            );

            return;
        }


        toast(
            "✅ Đã mở khóa tài khoản."
        );

        await fetchUsers();

    }
    catch (err) {

        console.error(err);

        toast(
            "❌ Lỗi kết nối máy chủ."
        );
    }
}


// =========================================================
// SEARCH / FILTER
// =========================================================

$("userSearch")
    ?.addEventListener(
        "input",
        renderUsers
    );


$("statusFilter")
    ?.addEventListener(
        "change",
        renderUsers
    );


// =========================================================
// MODAL EVENTS
// =========================================================

$("addUserBtn")
    ?.addEventListener(
        "click",
        openCreateUserModal
    );


$("closeUserModal")
    ?.addEventListener(
        "click",
        () => closeModal(
            "userModal"
        )
    );


$("cancelUserBtn")
    ?.addEventListener(
        "click",
        () => closeModal(
            "userModal"
        )
    );


$("userModal")
    ?.addEventListener(
        "click",
        event => {

            if (
                event.target ===
                $("userModal")
            ) {

                closeModal(
                    "userModal"
                );
            }

        }
    );


// =========================================================
// LOGOUT
// =========================================================

function initLogout() {

    const logoutBtn =
        $("logoutBtn");

    const logoutModal =
        $("logoutModal");

    const cancelBtn =
        $("cancelLogoutBtn");

    const confirmBtn =
        $("confirmLogoutBtn");


    if (logoutBtn) {

        logoutBtn.addEventListener(
            "click",
            event => {

                event.preventDefault();

                openModal(
                    "logoutModal"
                );

            }
        );

    }


    if (cancelBtn) {

        cancelBtn.addEventListener(
            "click",
            () => closeModal(
                "logoutModal"
            )
        );

    }


    if (logoutModal) {

        logoutModal.addEventListener(
            "click",
            event => {

                if (
                    event.target ===
                    logoutModal
                ) {

                    closeModal(
                        "logoutModal"
                    );
                }

            }
        );

    }


    if (confirmBtn) {

        confirmBtn.addEventListener(
            "click",
            () => {

                localStorage.removeItem(
                    "userSession"
                );

                sessionStorage.clear();

                window.location.replace(
                    "../auth/dangnhap.html"
                );

            }
        );

    }

}


// =========================================================
// LOAD USER INFO
// =========================================================

function loadCurrentUserInfo() {
    const user = getCurrentUser();

    if (!user) return;

    const fullName =
        user.fullName ||
        user.FullName ||
        user.name ||
        "Người dùng";

    const role = String(
        user.role ||
        user.Role ||
        user.roleCode ||
        user.RoleCode ||
        user.roleName ||
        user.RoleName ||
        ""
    ).trim().toUpperCase();

    const roleId = Number(
        user.roleId || user.RoleId || 0
    );

    let roleText =
        user.roleName ||
        user.RoleName ||
        "";

    if (
        role.includes("ADMIN") ||
        role.includes("QUẢN TRỊ") ||
        roleId === 1
    ) {
        roleText = "Quản trị hệ thống";
    } else if (
        role.includes("MANAGER") ||
        role.includes("QUẢN LÝ") ||
        roleId === 2
    ) {
        roleText = "Quản lý vận hành";
    } else if (role.includes("DRIVER")) {
        roleText = "Tài xế";
    } else if (role.includes("PASSENGER")) {
        roleText = "Hành khách";
    } else if (!roleText) {
        roleText = "Người dùng";
    }

    const parts = fullName.trim().split(/\s+/);

    const initials = parts.length > 1
        ? (parts[0][0] + parts[parts.length - 1][0])
            .toUpperCase()
        : fullName.substring(0, 2).toUpperCase();

    const sidebarName = $("sidebarUserName");
    const sidebarRole = $("sidebarUserRole");
    const sidebarAvatar = $("sidebarAvatar");
    const topAvatar = $("topAvatar");
    const topUserRole = $("topUserRole");

    if (sidebarName) {
        sidebarName.textContent = fullName;
    }

    if (sidebarRole) {
        sidebarRole.textContent = roleText;
    }

    if (sidebarAvatar) {
        sidebarAvatar.textContent = initials;
    }

    if (topAvatar) {
        topAvatar.textContent = initials;
    }

    if (topUserRole) {
        topUserRole.textContent = roleText;
    }
}

// =========================================================
// INITIALIZE
// =========================================================

document.addEventListener(
    "DOMContentLoaded",
    async () => {

        loadCurrentUserInfo();

        initLogout();

        const roleLoaded =
            await fetchRoles();

        if (roleLoaded) {

            await fetchUsers();
        }

    }
);
