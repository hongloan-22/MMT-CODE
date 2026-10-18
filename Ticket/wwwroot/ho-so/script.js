(function () {
    "use strict";

    const PROFILE_API = "/api/Profile/me";
    const form = document.getElementById("profileForm");
    const fullNameInput = document.getElementById("fullName");
    const emailInput = document.getElementById("email");
    const phoneInput = document.getElementById("phone");
    const message = document.getElementById("profileMessage");
    const saveButton = document.getElementById("saveProfileBtn");
    const resetButton = document.getElementById("resetProfileBtn");
    const logoutButton = document.getElementById("profileLogout");

    let session = null;
    let loadedProfile = null;

    function readSession() {
        try {
            return JSON.parse(localStorage.getItem("userSession") || "null");
        } catch {
            return null;
        }
    }

    function getUserId(user) {
        return user?.userId || user?.UserId || user?.id || user?.Id || "";
    }

    function authHeaders(includeJson) {
        const headers = { "X-User-Id": String(getUserId(session)) };
        if (includeJson) headers["Content-Type"] = "application/json";
        return headers;
    }

    async function readResponse(response) {
        return await response.json().catch(() => ({}));
    }

    function setMessage(text, type) {
        message.textContent = text || "";
        message.className = "profile-message" + (type ? " " + type : "");
    }

    function getInitials(name) {
        const parts = String(name || "Smartbus").trim().split(/\s+/).filter(Boolean);
        if (parts.length > 1) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
        return (parts[0] || "SB").substring(0, 2).toUpperCase();
    }

    function formatDate(value) {
        if (!value) return "Chưa có thông tin";
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return "Chưa có thông tin";
        return date.toLocaleDateString("vi-VN", {
            day: "2-digit", month: "2-digit", year: "numeric"
        });
    }

    function displayProfile(profile) {
        loadedProfile = {
            userId: profile.userId ?? profile.UserId ?? getUserId(session),
            fullName: profile.fullName ?? profile.FullName ?? "",
            email: profile.email ?? profile.Email ?? "",
            phone: profile.phone ?? profile.Phone ?? "",
            role: profile.role ?? profile.Role ?? session.role ?? session.Role ?? "USER",
            roleName: profile.roleName ?? profile.RoleName ?? "Hành khách",
            status: profile.status ?? profile.Status ?? "ACTIVE",
            createdAt: profile.createdAt ?? profile.CreatedAt ?? null
        };

        fullNameInput.value = loadedProfile.fullName;
        emailInput.value = loadedProfile.email;
        phoneInput.value = loadedProfile.phone;

        document.getElementById("summaryName").textContent = loadedProfile.fullName || "Người dùng";
        document.getElementById("summaryEmail").textContent = loadedProfile.email;
        document.getElementById("profileAvatar").textContent = getInitials(loadedProfile.fullName);
        document.getElementById("summaryRole").textContent = loadedProfile.roleName || loadedProfile.role;
        document.getElementById("summaryStatus").textContent =
            String(loadedProfile.status).toUpperCase() === "ACTIVE" ? "Đang hoạt động" : loadedProfile.status;
        document.getElementById("summaryCreatedAt").textContent = formatDate(loadedProfile.createdAt);
    }

    async function loadProfile() {
        setMessage("Đang tải thông tin tài khoản...", "");
        saveButton.disabled = true;

        try {
            const response = await fetch(PROFILE_API, {
                method: "GET",
                headers: authHeaders(false)
            });
            const result = await readResponse(response);

            if (response.status === 401) {
                localStorage.removeItem("userSession");
                window.location.replace("../auth/dangnhap.html");
                return;
            }

            if (!response.ok) {
                throw new Error(result.message || "Không thể tải hồ sơ cá nhân.");
            }

            displayProfile(result.data || result);
            setMessage("Thông tin tài khoản đã được tải.", "success");
        } catch (error) {
            setMessage(error.message || "Không thể kết nối đến máy chủ.", "error");
        } finally {
            saveButton.disabled = false;
        }
    }

    function updateSession(profile) {
        const updatedSession = {
            ...session,
            userId: profile.userId,
            fullName: profile.fullName,
            email: profile.email,
            role: profile.role,
            roleName: profile.roleName
        };
        localStorage.setItem("userSession", JSON.stringify(updatedSession));
        session = updatedSession;
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        setMessage("", "");

        const fullName = fullNameInput.value.trim();
        const email = emailInput.value.trim();
        const phone = phoneInput.value.trim();

        if (fullName.length < 2 || fullName.length > 100) {
            setMessage("Họ tên phải có từ 2 đến 100 ký tự.", "error");
            fullNameInput.focus();
            return;
        }

        if (!emailInput.validity.valid || email.length > 100) {
            setMessage("Vui lòng nhập email hợp lệ, tối đa 100 ký tự.", "error");
            emailInput.focus();
            return;
        }

        if (!/^\d{10}$/.test(phone)) {
            setMessage("Số điện thoại phải gồm đúng 10 chữ số.", "error");
            phoneInput.focus();
            return;
        }

        saveButton.disabled = true;
        saveButton.textContent = "Đang lưu...";

        try {
            const response = await fetch(PROFILE_API, {
                method: "PUT",
                headers: authHeaders(true),
                body: JSON.stringify({ fullName, email, phone })
            });
            const result = await readResponse(response);

            if (response.status === 401) {
                localStorage.removeItem("userSession");
                window.location.replace("../auth/dangnhap.html");
                return;
            }

            if (!response.ok) {
                throw new Error(result.message || "Không thể cập nhật hồ sơ.");
            }

            const profile = result.data || result;
            displayProfile(profile);
            updateSession(loadedProfile);
            setMessage(result.message || "Cập nhật hồ sơ thành công.", "success");
        } catch (error) {
            setMessage(error.message || "Không thể kết nối đến máy chủ.", "error");
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = "Lưu thay đổi";
        }
    });

    resetButton.addEventListener("click", function () {
        if (loadedProfile) displayProfile(loadedProfile);
        setMessage("Đã khôi phục thông tin gần nhất được tải/lưu.", "");
    });

    if (logoutButton) {
        logoutButton.addEventListener("click", async function () {
            const confirmed = await SmartbusDialog.confirm(
                "Bạn có chắc chắn muốn đăng xuất?",
                { title: "Xác nhận đăng xuất", confirmText: "Đăng xuất", danger: true }
            );
            if (!confirmed) return;
            localStorage.removeItem("userSession");
            window.location.href = "../auth/dangnhap.html";
        });
    }

    session = readSession();
    if (!session || !getUserId(session)) {
        localStorage.removeItem("userSession");
        window.location.replace("../auth/dangnhap.html");
        return;
    }

    const role = String(session.role || session.Role || session.roleCode || session.RoleCode || "USER").toUpperCase();
    if (role !== "USER") {
        const tripsLink = document.getElementById("myTripsLink");
        if (tripsLink) tripsLink.style.display = "none";
    }

    loadProfile();
})();
