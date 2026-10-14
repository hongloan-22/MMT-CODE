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
        // ĐĂNG XUẤT
        // =====================================================

        const logoutBtn =
            document.getElementById(
                "logoutBtn"
            );

        if (
            logoutBtn &&
            !logoutBtn.dataset.bound
        ) {

            logoutBtn.dataset.bound =
                "true";

            logoutBtn.addEventListener(
                "click",
                function () {

                    const confirmed =
                        window.confirm(
                            "Xác nhận đăng xuất?"
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
