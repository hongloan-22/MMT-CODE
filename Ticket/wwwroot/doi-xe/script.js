// @ts-nocheck

let buses = [];
let staff = [];
let selectedBusId = null;
const $ = id =>
    document.getElementById(id);


// =========================================================
// SESSION
// =========================================================

function getCurrentUser() {

    try {

        const raw =
            localStorage.getItem(
                "userSession"
            );

        return raw
            ? JSON.parse(raw)
            : null;

    }
    catch {

        return null;
    }
}


function getCurrentUserId() {

    return getCurrentUser()
        ?.userId || "";
}


function authHeaders(json = false) {

    const headers = {
        "X-User-Id":
            getCurrentUserId()
    };

    if (json) {

        headers["Content-Type"] =
            "application/json";
    }

    return headers;
}


// =========================================================
// UTILITY
// =========================================================

function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


function toast(message) {

    const el =
        $("toast");

    if (!el)
        return;

    el.textContent =
        message;

    el.classList.add(
        "show"
    );

    clearTimeout(
        window.toastTimer
    );

    window.toastTimer =
        setTimeout(
            () =>
                el.classList.remove(
                    "show"
                ),
            2500
        );
}


function openModal(id) {

    $(id)
        ?.classList.add(
            "open"
        );
}


function closeModal(id) {

    $(id)
        ?.classList.remove(
            "open"
        );
}


// =========================================================
// AUTH
// =========================================================

function checkAuth() {

    const user =
        getCurrentUser();

    if (!user) {

        window.location.replace(
            "../auth/dangnhap.html"
        );

        return false;
    }

    const role =
        String(
            user.role ||
            user.roleCode ||
            user.Role ||
            user.RoleCode ||
            ""
        )
            .trim()
            .toUpperCase();

    if (
        role !== "ADMIN" &&
        role !== "MANAGER"
    ) {
        window.location.replace(
            "../index.html"
        );

        return false;
    }

    const fullName =
        user.fullName ||
        "Người dùng";

    const initials =
        fullName
            .split(" ")
            .filter(Boolean)
            .map(
                x => x[0]
            )
            .join("")
            .slice(0, 2)
            .toUpperCase();

    const roleName =
        role === "ADMIN"
            ? "Quản trị hệ thống"
            : "Quản lý vận hành";

    if ($("sidebarUserName"))
        $("sidebarUserName")
            .textContent =
            fullName;

    if ($("sidebarUserRole"))
        $("sidebarUserRole")
            .textContent =
            roleName;

    if ($("sidebarAvatar"))
        $("sidebarAvatar")
            .textContent =
            initials;

    if ($("topAvatar"))
        $("topAvatar")
            .textContent =
            initials;

    if ($("topUserRole"))
        $("topUserRole")
            .textContent =
            roleName;

    if (role !== "ADMIN") {

        $("linkPhanQuyen")
            ?.style.setProperty(
                "display",
                "none"
            );
    }

    return true;
}


// =========================================================
// LOAD BUSES
// =========================================================

async function loadBuses() {

    const response =
        await fetch(
            "/api/Bus",
            {
                headers:
                    authHeaders()
            }
        );

    if (!response.ok) {

        throw new Error(
            "Không thể tải danh sách xe."
        );
    }

    buses =
        await response.json();

    renderBuses();

    updateStats();
}


// =========================================================
// LOAD STAFF
// =========================================================

async function loadStaff() {

    const response =
        await fetch(
            "/api/Bus/staff",
            {
                headers:
                    authHeaders()
            }
        );

    if (!response.ok) {

        throw new Error(
            "Không thể tải danh sách nhân sự."
        );
    }

    staff =
        await response.json();

    renderStaff();

    renderStaffOptions();

    updateStats();
}


// =========================================================
// RENDER BUS
// =========================================================

function renderBuses() {
    const tbody = $("busTableBody");
    if (!tbody) return;

    if (!buses.length) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="empty-cell">
                    Chưa có xe trong hệ thống.
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = buses.map(bus => {
        const driver = bus.driver
            ? escapeHtml(bus.driver.fullName)
            : `<span class="muted-text">Chưa phân công</span>`;

        const attendants = bus.attendants?.length
            ? bus.attendants.map(x => `
                <div class="staff-chip">
                    ${escapeHtml(x.fullName)}
                    <button type="button"
                        onclick="event.stopPropagation(); removeStaff(${bus.id}, ${x.assignmentId})">
                        ×
                    </button>
                </div>`).join("")
            : `<span class="muted-text">Chưa có</span>`;

        return `
            <tr data-bus-id="${bus.id}"
                class="${Number(selectedBusId) === Number(bus.id) ? "selected" : ""}"
                onclick="selectBusForAssignment(${bus.id})">

                <td><span class="bus-code">${escapeHtml(bus.busCode)}</span></td>
                <td><strong>${escapeHtml(bus.licensePlate)}</strong></td>
                <td>${bus.seatCount}</td>
                <td>${driver}</td>
                <td><div class="staff-list">${attendants}</div></td>
                <td>
                    ${bus.isActive
                        ? `<span class="status active">Hoạt động</span>`
                        : `<span class="status inactive">Ngừng hoạt động</span>`}
                </td>
                <td>
                    <div class="row-actions">
                        ${bus.isActive ? `
                            <button class="mini-btn"
                                title="Phân công"
                                onclick="event.stopPropagation(); selectBusForAssignment(${bus.id})">
                                👤
                            </button>
                            <button class="mini-btn danger"
                                title="Ngừng xe"
                                onclick="event.stopPropagation(); deactivateBus(${bus.id})">
                                ×
                            </button>` : ""}
                    </div>
                </td>
            </tr>`;
    }).join("");
}


function selectBusForAssignment(busId) {
    const bus = buses.find(x => Number(x.id) === Number(busId));
    if (!bus) return;

    selectedBusId = bus.id;

    $("assignmentPanel").scrollIntoView({
        behavior: "smooth",
        block: "nearest"
    });

    $("assignmentTitle").textContent =
        `Phân công nhân sự - ${bus.busCode}`;

    $("assignmentDescription").textContent =
        "Chọn tài xế và phụ xe cho xe bus đang được chọn.";

    $("selectedBusId").value = bus.id;
    $("selectedBusCode").textContent = bus.busCode;
    $("selectedBusPlate").textContent = bus.licensePlate;
    $("selectedBusSeats").textContent = bus.seatCount;

    $("assignmentEmpty").hidden = true;
    $("inlineStaffForm").hidden = false;

    renderInlineStaffOptions(bus);

    renderBuses();
}

function renderInlineStaffOptions(bus) {
    const driverSelect = $("inlineDriver");
    const attendantSelect = $("inlineAttendant");

    if (!driverSelect || !attendantSelect) return;

    const currentDriverId = bus.driver?.userId || "";
    const currentAttendantIds = new Set(
        (bus.attendants || []).map(x => String(x.userId))
    );

    function buildOptions(role, currentIds) {
        const currentSet = new Set(
            Array.isArray(currentIds)
                ? currentIds.map(String)
                : [String(currentIds || "")]
        );

        return `
            <option value="">-- Không phân công --</option>
            ${staff.map(user => {
            const userId = String(user.userId);
            const assignment = user.assignment;

            const assignedToOtherBus =
                assignment &&
                Number(assignment.busId) !== Number(bus.id);

            const isCurrent = currentSet.has(userId);

            return `
                    <option value="${escapeHtml(userId)}"
                        ${assignedToOtherBus && !isCurrent ? "disabled" : ""}
                        ${isCurrent ? "selected" : ""}>
                        ${escapeHtml(user.fullName)}
                        ${assignedToOtherBus ? " (đang được phân công)" : ""}
                    </option>`;
        }).join("")}
        `;
    }

    driverSelect.innerHTML = buildOptions("DRIVER", currentDriverId);
    attendantSelect.innerHTML = buildOptions("ATTENDANT", currentAttendantIds);
}

$("cancelInlineAssignment")?.addEventListener("click", () => {
    selectedBusId = null;

    $("inlineStaffForm").hidden = true;
    $("assignmentEmpty").hidden = false;

    $("assignmentTitle").textContent = "Phân công nhân sự";
    $("assignmentDescription").textContent =
        "Chọn một xe trong danh sách phía trên để phân công tài xế và phụ xe.";

    renderBuses();
});

$("inlineStaffForm")?.addEventListener("submit", async event => {
    event.preventDefault();

    const busId = Number($("selectedBusId").value);
    const driverId = $("inlineDriver").value;
    const attendantId = $("inlineAttendant").value;

    if (!busId) {
        toast("❌ Vui lòng chọn xe trước.");
        return;
    }

    const bus = buses.find(x => Number(x.id) === busId);
    if (!bus) {
        toast("❌ Không tìm thấy thông tin xe.");
        return;
    }

    const currentDriverId = bus.driver?.userId
        ? String(bus.driver.userId)
        : "";

    const currentAttendantIds = new Set(
        (bus.attendants || []).map(x => String(x.userId))
    );

    const requests = [];

    if (driverId && String(driverId) !== currentDriverId) {
        requests.push({
            userId: driverId,
            assignmentType: "DRIVER"
        });
    }

    if (attendantId && !currentAttendantIds.has(String(attendantId))) {
        requests.push({
            userId: attendantId,
            assignmentType: "ATTENDANT"
        });
    }

    if (!requests.length) {
        toast("Bạn chưa thay đổi phân công nào.");
        return;
    }

    const saveButton = $("saveInlineAssignment");
    saveButton.disabled = true;

    try {
        for (const item of requests) {
            const response = await fetch(`/api/Bus/${busId}/staff`, {
                method: "POST",
                headers: authHeaders(true),
                body: JSON.stringify({
                    UserId: item.userId,
                    AssignmentType: item.assignmentType
                })
            });

            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                throw new Error(
                    data.message || "Không thể lưu phân công."
                );
            }
        }

        toast("✅ Đã lưu phân công nhân sự.");

        await Promise.all([
            loadBuses(),
            loadStaff()
        ]);

        const refreshedBus = buses.find(x => Number(x.id) === busId);

        if (refreshedBus) {
            selectBusForAssignment(busId);
        }
    } catch (error) {
        toast("❌ " + (error.message || "Có lỗi khi lưu phân công."));
    } finally {
        saveButton.disabled = false;
    }
});

// =========================================================
// RENDER STAFF
// =========================================================

function renderStaff() {

    const tbody =
        $("staffTableBody");

    if (!tbody)
        return;

    if (!staff.length) {

        tbody.innerHTML = `
            <tr>
                <td colspan="5"
                    class="empty-cell">
                    Chưa có tài khoản được phân quyền DRIVER.
                </td>
            </tr>
        `;

        return;
    }

    tbody.innerHTML =
        staff.map(user => {

            const assignment =
                user.assignment;

            return `
                <tr>

                    <td>
                        <strong>
                            ${escapeHtml(
                user.fullName
            )}
                        </strong>
                    </td>

                    <td>
                        ${escapeHtml(
                user.email
            )}
                    </td>

                    <td>
                        ${escapeHtml(
                user.phone
            )}
                    </td>

                    <td>
                        <span class="role-badge">
                            DRIVER
                        </span>
                    </td>

                    <td>

                        ${assignment
                    ? `
                                    <span class="assignment-badge">
                                        ${assignment.assignmentType ===
                        "DRIVER"
                        ? "Tài xế"
                        : "Phụ xe"
                    }
                                        ·
                                        ${escapeHtml(
                        assignment.busCode
                    )}
                                    </span>
                                  `
                    : `
                                    <span class="unassigned">
                                        Chưa phân công
                                    </span>
                                  `
                }

                    </td>

                </tr>
            `;
        })
            .join("");
}


// =========================================================
// STAFF SELECT
// =========================================================

function renderStaffOptions() {

    const select =
        $("assignmentUser");

    if (!select)
        return;

    const currentBusId =
        Number(
            $("assignmentBusId")
                ?.value || 0
        );

    select.innerHTML = `
        <option value="">
            -- Chọn nhân sự --
        </option>
    `;

    staff.forEach(user => {

        const assignment =
            user.assignment;

        const option =
            document.createElement(
                "option"
            );

        option.value =
            user.userId;

        let text =
            `${user.fullName} - ${user.email}`;

        if (assignment) {

            text +=
                ` — đang phân công ${assignment.busCode}`;
        }

        option.textContent =
            text;

        if (
            assignment &&
            assignment.busId !==
            currentBusId
        ) {

            option.disabled =
                true;
        }

        select.appendChild(
            option
        );
    });
}


// =========================================================
// BUS OPTIONS
// =========================================================

function renderBusOptions() {

    const select =
        $("assignmentBus");

    if (!select)
        return;

    select.innerHTML =
        buses
            .filter(
                x => x.isActive
            )
            .map(
                x =>
                    `
                    <option value="${x.id}">
                        ${escapeHtml(
                        x.busCode
                    )}
                        -
                        ${escapeHtml(
                        x.licensePlate
                    )}
                    </option>
                    `
            )
            .join("");
}


// =========================================================
// STATS
// =========================================================

function updateStats() {

    const active =
        buses.filter(
            x => x.isActive
        );

    const assigned =
        active.filter(
            x => x.driver
        );

    if ($("totalBuses"))
        $("totalBuses")
            .textContent =
            buses.length;

    if ($("activeBuses"))
        $("activeBuses")
            .textContent =
            active.length;

    if ($("totalStaff"))
        $("totalStaff")
            .textContent =
            staff.length;

    if ($("assignedBuses"))
        $("assignedBuses")
            .textContent =
            assigned.length;
}


// =========================================================
// ADD BUS
// =========================================================

$("addBusBtn")
    ?.addEventListener(
        "click",
        () => {

            $("busForm")
                ?.reset();

            $("seatCount").value =
                "21";

            openModal(
                "busModal"
            );
        }
    );


$("busForm")
    ?.addEventListener(
        "submit",
        async event => {

            event.preventDefault();

            const payload = {

                BusCode:
                    $("busCode")
                        .value
                        .trim(),

                LicensePlate:
                    $("licensePlate")
                        .value
                        .trim(),

                SeatCount:
                    Number(
                        $("seatCount")
                            .value
                    )
            };

            if (
                !payload.BusCode ||
                !payload.LicensePlate ||
                !payload.SeatCount
            ) {

                toast(
                    "❌ Vui lòng nhập đầy đủ thông tin."
                );

                return;
            }

            const btn =
                $("saveBusBtn");

            btn.disabled =
                true;

            btn.textContent =
                "Đang lưu...";

            try {

                const response =
                    await fetch(
                        "/api/Bus",
                        {
                            method:
                                "POST",

                            headers:
                                authHeaders(
                                    true
                                ),

                            body:
                                JSON.stringify(
                                    payload
                                )
                        }
                    );

                const data =
                    await response
                        .json()
                        .catch(
                            () => ({})
                        );

                if (!response.ok) {

                    throw new Error(
                        data.message ||
                        "Không thể tạo xe."
                    );
                }

                toast(
                    "✅ Tạo xe thành công."
                );

                closeModal(
                    "busModal"
                );

                await loadBuses();

            }
            catch (error) {

                toast(
                    "❌ " +
                    error.message
                );

            }
            finally {

                btn.disabled =
                    false;

                btn.textContent =
                    "Lưu xe";
            }
        }
    );


// =========================================================
// OPEN ASSIGNMENT MODAL
// =========================================================

function openAssignmentModal(
    busId
) {

    renderBusOptions();

    const bus =
        buses.find(
            x =>
                x.id ===
                Number(busId)
        );

    if (!bus)
        return;

    $("assignmentBusId")
        .value =
        bus.id;

    $("assignmentBus")
        .value =
        bus.id;

    renderStaffOptions();

    $("assignmentType")
        .value =
        "DRIVER";

    openModal(
        "staffModal"
    );
}


// =========================================================
// SAVE ASSIGNMENT
// =========================================================

$("staffForm")
    ?.addEventListener(
        "submit",
        async event => {

            event.preventDefault();

            const busId =
                Number(
                    $("assignmentBusId")
                        .value
                );

            const userId =
                $("assignmentUser")
                    .value;

            const assignmentType =
                $("assignmentType")
                    .value;

            if (!busId ||
                !userId) {

                toast(
                    "❌ Vui lòng chọn xe và nhân sự."
                );

                return;
            }

            try {

                const response =
                    await fetch(
                        `/api/Bus/${busId}/staff`,
                        {
                            method:
                                "POST",

                            headers:
                                authHeaders(
                                    true
                                ),

                            body:
                                JSON.stringify({
                                    UserId:
                                        userId,

                                    AssignmentType:
                                        assignmentType
                                })
                        }
                    );

                const data =
                    await response
                        .json()
                        .catch(
                            () => ({})
                        );

                if (!response.ok) {

                    throw new Error(
                        data.message ||
                        "Không thể phân công."
                    );
                }

                toast(
                    "✅ " +
                    data.message
                );

                closeModal(
                    "staffModal"
                );

                await Promise.all([
                    loadBuses(),
                    loadStaff()
                ]);

            }
            catch (error) {

                toast(
                    "❌ " +
                    error.message
                );
            }
        }
    );


// =========================================================
// REMOVE STAFF
// =========================================================

async function removeStaff(
    busId,
    assignmentId
) {

    const confirmed =
        confirm(
            "Bạn có chắc muốn bỏ phân công này?"
        );

    if (!confirmed)
        return;

    try {

        const response =
            await fetch(
                `/api/Bus/${busId}/staff/${assignmentId}`,
                {
                    method:
                        "DELETE",

                    headers:
                        authHeaders()
                }
            );

        const data =
            await response
                .json()
                .catch(
                    () => ({})
                );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể bỏ phân công."
            );
        }

        toast(
            "✅ Đã bỏ phân công."
        );

        await Promise.all([
            loadBuses(),
            loadStaff()
        ]);

    }
    catch (error) {

        toast(
            "❌ " +
            error.message
        );
    }
}


// =========================================================
// DEACTIVATE BUS
// =========================================================

async function deactivateBus(
    busId
) {

    const bus =
        buses.find(
            x => x.id === busId
        );

    if (!bus)
        return;

    const confirmed =
        confirm(
            `Ngừng hoạt động xe ${bus.busCode}?`
        );

    if (!confirmed)
        return;

    try {

        const response =
            await fetch(
                `/api/Bus/${busId}`,
                {
                    method:
                        "DELETE",

                    headers:
                        authHeaders()
                }
            );

        const data =
            await response
                .json()
                .catch(
                    () => ({})
                );

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Không thể ngừng xe."
            );
        }

        toast(
            "✅ Đã ngừng xe."
        );

        await Promise.all([
            loadBuses(),
            loadStaff()
        ]);

    }
    catch (error) {

        toast(
            "❌ " +
            error.message
        );
    }
}


// =========================================================
// MODAL CLOSE
// =========================================================

document
    .querySelectorAll(
        "[data-close]"
    )
    .forEach(
        button => {

            button.addEventListener(
                "click",
                () => {

                    closeModal(
                        button.dataset.close
                    );
                }
            );
        }
    );


// =========================================================
// LOGOUT
// =========================================================

$("logoutBtn")
    ?.addEventListener(
        "click",
        event => {

            event.preventDefault();

            openModal(
                "logoutModal"
            );
        }
    );


$("cancelLogoutBtn")
    ?.addEventListener(
        "click",
        () =>
            closeModal(
                "logoutModal"
            )
    );


$("confirmLogoutBtn")
    ?.addEventListener(
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


// =========================================================
// LOAD
// =========================================================

document.addEventListener(
    "DOMContentLoaded",
    async () => {

        if (!checkAuth())
            return;

        try {

            await Promise.all([
                loadBuses(),
                loadStaff()
            ]);

        }
        catch (error) {

            console.error(error);

            toast(
                "❌ Không thể tải dữ liệu Đội xe."
            );
        }
    }
);
