(function () {

    "use strict";


    // =========================================================
    // LẤY SESSION
    // =========================================================

    function getSession() {

        try {

            const raw =
                localStorage.getItem(
                    "userSession"
                );

            if (!raw)
                return null;

            return JSON.parse(raw);

        }
        catch (error) {

            console.error(
                "Không thể đọc userSession:",
                error
            );

            return null;
        }
    }


    // =========================================================
    // LẤY ROLE
    // =========================================================

    function getRole(user) {

        if (!user)
            return "";

        return String(
            user.role ||
            user.Role ||
            user.roleCode ||
            user.RoleCode ||
            user.roleName ||
            user.RoleName ||
            ""
        )
            .trim()
            .toUpperCase();
    }


    // =========================================================
    // CẬP NHẬT SIDEBAR
    // =========================================================

    function updateSidebar() {

        const user =
            getSession();

        if (!user)
            return;


        const role =
            getRole(user);


        // =====================================================
        // CHỈ ADMIN ĐƯỢC THẤY PHÂN QUYỀN
        // =====================================================

        const permissionLinks =
            document.querySelectorAll(
                "#linkPhanQuyen"
            );

        permissionLinks.forEach(
            link => {

                if (role === "ADMIN") {

                    link.style.display =
                        "";

                }
                else {

                    link.style.display =
                        "none";
                }

            }
        );


        // =====================================================
        // THÔNG TIN TÀI KHOẢN
        // =====================================================

        const name =
            user.fullName ||
            user.FullName ||
            "Người dùng";


        const roleText =
            role === "ADMIN"
                ? "Quản trị hệ thống"
                : role === "MANAGER"
                    ? "Quản lý vận hành"
                    : role === "DRIVER"
                        ? "Tài xế"
                        : "Hành khách";


        document
            .querySelectorAll(
                "#sidebarUserName"
            )
            .forEach(
                element => {

                    element.textContent =
                        name;

                }
            );


        document
            .querySelectorAll(
                "#sidebarUserRole"
            )
            .forEach(
                element => {

                    element.textContent =
                        roleText;

                }
            );


        // =====================================================
        // AVATAR
        // =====================================================

        const avatar =
            document.getElementById(
                "sidebarAvatar"
            );

        if (avatar) {

            const parts =
                name
                    .trim()
                    .split(/\s+/);

            let initials = "";

            if (parts.length === 1) {

                initials =
                    parts[0]
                        .substring(0, 2)
                        .toUpperCase();

            }
            else {

                initials =
                    (
                        parts[0][0] +
                        parts[parts.length - 1][0]
                    )
                        .toUpperCase();

            }

            avatar.textContent =
                initials;
        }


        // =====================================================
        // LIÊN KẾT HỒ SƠ CÁ NHÂN
        // Dùng chung trên các trang quản trị có sidebar.
        // =====================================================

        const sidebarBottom =
            document.querySelector(".sidebar-bottom");

        const logoutBtn =
            document.getElementById(
                "logoutBtn"
            );

        if (
            sidebarBottom &&
            !document.getElementById("linkHoSoCaNhan")
        ) {
            const profileLink = document.createElement("a");
            profileLink.id = "linkHoSoCaNhan";
            profileLink.href = "../ho-so/index.html";
            profileLink.textContent = "👤 Hồ sơ cá nhân";
            profileLink.style.display = "block";
            profileLink.style.width = "100%";
            profileLink.style.boxSizing = "border-box";
            profileLink.style.textAlign = "center";
            profileLink.style.textDecoration = "none";
            profileLink.style.border = "1px solid #dceaf4";
            profileLink.style.background = "#f4faff";
            profileLink.style.borderRadius = "9px";
            profileLink.style.padding = "9px";
            profileLink.style.margin = "0 0 9px";
            profileLink.style.color = "#167cc7";
            profileLink.style.fontSize = "13px";
            profileLink.style.fontWeight = "700";

            sidebarBottom.insertBefore(profileLink, logoutBtn || null);
        }

        if (
            logoutBtn &&
            !logoutBtn.dataset.bound
        ) {

            logoutBtn.dataset.bound =
                "true";

            logoutBtn.addEventListener(
                "click",
                async function () {

                    const confirmed = await SmartbusDialog.confirm(
                        "Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?",
                        { title: "Xác nhận đăng xuất", confirmText: "Đăng xuất", danger: true }
                    );

                    if (!confirmed)
                        return;


                    localStorage.removeItem(
                        "userSession"
                    );

                    window.location.href =
                        "../auth/dangnhap.html";

                }
            );
        }
    }


    // =========================================================
    // CHẠY SAU KHI DOM ĐƯỢC TẢI
    // =========================================================

    document.addEventListener(
        "DOMContentLoaded",
        updateSidebar
    );

})();
