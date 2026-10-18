(() => {
    "use strict";

    const $ = id => document.getElementById(id);

    const state = {
        items: [],
        loading: false,
        processingIds: new Set()
    };

    function getSession() {
        try {
            const raw = localStorage.getItem("userSession");
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    }

    function getUserId() {
        const user = getSession();
        return String(
            user?.userId || user?.UserId || user?.userID || user?.UserID || ""
        ).trim();
    }

    function headers() {
        return {
            "X-User-Id": getUserId(),
            "Accept": "application/json"
        };
    }

    function getValue(object, ...keys) {
        for (const key of keys) {
            if (object && object[key] !== undefined && object[key] !== null) {
                return object[key];
            }
        }
        return null;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function money(value) {
        return new Intl.NumberFormat("vi-VN", {
            maximumFractionDigits: 0
        }).format(Number(value || 0)) + " đ";
    }

    function dateTime(value) {
        if (!value) return "—";
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return escapeHtml(value);
        return date.toLocaleString("vi-VN", {
            day: "2-digit", month: "2-digit", year: "numeric",
            hour: "2-digit", minute: "2-digit"
        });
    }

    function normalizeItem(item) {
        return {
            refundId: getValue(item, "refundId", "RefundId", "id", "Id"),
            ticketId: getValue(item, "ticketId", "TicketId"),
            ticketCode: getValue(item, "ticketCode", "TicketCode") || "—",
            bookingCode: getValue(item, "bookingCode", "BookingCode") || "—",
            amount: Number(getValue(item, "amount", "Amount") || 0),
            provider: getValue(item, "provider", "Provider") || "Không rõ",
            transactionCode: getValue(item, "transactionCode", "TransactionCode") || "—",
            reason: getValue(item, "reason", "Reason") || "—",
            status: getValue(item, "status", "Status") || "PENDING",
            createdAt: getValue(item, "createdAt", "CreatedAt")
        };
    }

    let toastTimer;
    function showToast(message) {
        const el = $("toast");
        if (!el) return;
        el.textContent = message;
        el.classList.add("show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove("show"), 3200);
    }

    function setTableMessage(message, isError = false) {
        $("refundTableBody").innerHTML = `
            <tr><td colspan="10" class="table-message ${isError ? "error" : ""}">
                ${escapeHtml(message)}
            </td></tr>`;
        $("resultCount").textContent = isError ? "Không tải được dữ liệu" : message;
    }

    function updateSummary(items) {
        $("pendingCount").textContent = String(items.length);
        $("pendingAmount").textContent = money(
            items.reduce((sum, item) => sum + item.amount, 0)
        );
        $("lastUpdated").textContent = new Date().toLocaleTimeString("vi-VN", {
            hour: "2-digit", minute: "2-digit"
        });
    }

    function renderTable() {
        const keyword = $("refundSearch").value.trim().toLocaleLowerCase("vi-VN");
        const items = state.items.filter(item => {
            const text = [
                item.refundId, item.ticketId, item.ticketCode, item.bookingCode,
                item.provider, item.transactionCode, item.reason, item.amount
            ].join(" ").toLocaleLowerCase("vi-VN");
            return text.includes(keyword);
        });

        $("resultCount").textContent = `Hiển thị ${items.length}/${state.items.length} yêu cầu`;

        if (items.length === 0) {
            $("refundTableBody").innerHTML = state.items.length === 0
                ? `<tr><td colspan="10" class="table-message"><span class="empty-title">Không có yêu cầu hoàn tiền đang chờ</span><span class="empty-detail">Khi có yêu cầu phù hợp, chúng sẽ xuất hiện tại đây.</span></td></tr>`
                : `<tr><td colspan="10" class="table-message">Không tìm thấy yêu cầu khớp từ khóa.</td></tr>`;
            return;
        }

        $("refundTableBody").innerHTML = items.map(item => {
            const id = Number(item.refundId);
            const disabled = state.processingIds.has(id);
            return `
                <tr>
                    <td class="mono">#${escapeHtml(item.refundId ?? "—")}</td>
                    <td class="mono">${escapeHtml(item.ticketCode)}</td>
                    <td class="mono">${escapeHtml(item.bookingCode)}</td>
                    <td>${escapeHtml(item.provider)}</td>
                    <td class="mono">${escapeHtml(item.transactionCode)}</td>
                    <td class="reason-cell">${escapeHtml(item.reason)}</td>
                    <td class="money-cell">${money(item.amount)}</td>
                    <td class="mono">${dateTime(item.createdAt)}</td>
                    <td><span class="status-pill">${escapeHtml(item.status)}</span></td>
                    <td><button class="refund-process-btn" type="button" data-process-id="${escapeHtml(item.refundId)}" ${disabled ? "disabled" : ""}>${disabled ? "Đang xử lý..." : "Xử lý hoàn tiền"}</button></td>
                </tr>`;
        }).join("");
    }

    async function readResponse(response) {
        const data = await response.json().catch(() => null);
        if (response.status === 401) {
            window.location.href = "../auth/dangnhap.html";
            throw new Error("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }
        if (response.status === 403) {
            throw new Error(getValue(data, "message", "Message") || "Chỉ Admin hoặc Manager được sử dụng chức năng này.");
        }
        if (!response.ok) {
            throw new Error(getValue(data, "message", "Message") || `Yêu cầu thất bại (HTTP ${response.status}).`);
        }
        return data;
    }

    async function loadPendingRefunds() {
        if (state.loading) return;
        if (!getUserId()) {
            window.location.href = "../auth/dangnhap.html";
            return;
        }

        state.loading = true;
        const refreshBtn = $("refreshBtn");
        refreshBtn.disabled = true;
        refreshBtn.textContent = "Đang tải...";
        setTableMessage("Đang tải danh sách yêu cầu hoàn tiền...");

        try {
            const response = await fetch("/api/Refund/pending", {
                method: "GET",
                headers: headers(),
                cache: "no-store"
            });
            const payload = await readResponse(response);
            const rawItems = Array.isArray(payload)
                ? payload
                : (getValue(payload, "data", "Data", "items", "Items") || []);

            if (!Array.isArray(rawItems)) {
                throw new Error("API đã phản hồi nhưng dữ liệu danh sách không đúng định dạng.");
            }

            state.items = rawItems.map(normalizeItem);
            updateSummary(state.items);
            renderTable();
        } catch (error) {
            state.items = [];
            $("pendingCount").textContent = "—";
            $("pendingAmount").textContent = "—";
            $("lastUpdated").textContent = "Lỗi";
            setTableMessage(error.message || "Không thể tải danh sách hoàn tiền.", true);
            showToast("❌ " + (error.message || "Không thể tải danh sách hoàn tiền."));
        } finally {
            state.loading = false;
            refreshBtn.disabled = false;
            refreshBtn.textContent = "↻ Làm mới";
        }
    }

    async function processRefund(refundId) {
        const id = Number(refundId);
        if (!Number.isInteger(id) || id <= 0) {
            showToast("❌ Mã yêu cầu hoàn tiền không hợp lệ.");
            return;
        }

        const item = state.items.find(row => Number(row.refundId) === id);
        if (!item) return;

        const approved = await SmartbusDialog.confirm(
            `Mã yêu cầu: #${id}\n` +
            `Mã vé: ${item.ticketCode}\n` +
            `Số tiền: ${money(item.amount)}\n\n` +
            "Lưu ý: chức năng hiện tại chỉ cập nhật trạng thái demo, chưa chuyển tiền thật.",
            { title: "Xác nhận xử lý hoàn tiền", confirmText: "Xử lý hoàn tiền", variant: "warning" }
        );
        if (!approved) return;

        state.processingIds.add(id);
        renderTable();

        try {
            const response = await fetch(`/api/Refund/${encodeURIComponent(id)}/process`, {
                method: "POST",
                headers: headers(),
                cache: "no-store"
            });
            const result = await readResponse(response);
            showToast("✅ " + (getValue(result, "message", "Message") || "Đã xử lý yêu cầu hoàn tiền."));
            await loadPendingRefunds();
        } catch (error) {
            showToast("❌ " + (error.message || "Không xử lý được yêu cầu hoàn tiền."));
        } finally {
            state.processingIds.delete(id);
            renderTable();
        }
    }

    document.addEventListener("DOMContentLoaded", () => {
        $("refreshBtn").addEventListener("click", loadPendingRefunds);
        $("refundSearch").addEventListener("input", renderTable);
        $("refundTableBody").addEventListener("click", event => {
            const button = event.target.closest("[data-process-id]");
            if (button) processRefund(button.dataset.processId);
        });
        loadPendingRefunds();
    });
})();
