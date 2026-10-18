/* Smartbus centered dialog replacement for browser alert/confirm. */
(function () {
    "use strict";

    if (window.SmartbusDialog) return;

    const STYLE_ID = "smartbus-dialog-styles";
    let activeDialog = null;
    const queue = [];

    function ensureStyles() {
        if (document.getElementById(STYLE_ID)) return;
        const style = document.createElement("style");
        style.id = STYLE_ID;
        style.textContent = `
            .sb-dialog-overlay {
                position: fixed; inset: 0; z-index: 100000;
                display: flex; align-items: center; justify-content: center;
                padding: 20px; background: rgba(22, 44, 61, .42);
                backdrop-filter: blur(2px); -webkit-backdrop-filter: blur(2px);
                animation: sbDialogFadeIn .16s ease-out;
            }
            .sb-dialog-overlay[hidden] { display: none !important; }
            .sb-dialog-card {
                width: min(100%, 460px); max-height: min(84vh, 720px);
                overflow: auto; box-sizing: border-box; background: #fff;
                border: 1px solid rgba(220, 230, 239, .95); border-radius: 18px;
                padding: 28px 30px 26px; box-shadow: 0 24px 70px rgba(12, 34, 52, .24);
                color: #1c2d3d; text-align: center;
                animation: sbDialogRise .18s ease-out;
            }
            .sb-dialog-icon {
                width: 52px; height: 52px; margin: 0 auto 14px; border-radius: 16px;
                display: flex; align-items: center; justify-content: center;
                background: #eaf5ff; color: #1685e8; font-size: 25px; font-weight: 700;
            }
            .sb-dialog-icon.danger { background: #fff0f1; color: #d92f45; }
            .sb-dialog-icon.warning { background: #fff6df; color: #b78105; }
            .sb-dialog-title { margin: 0 0 10px; color: #1e3041; font-size: 21px; line-height: 1.35; font-weight: 750; }
            .sb-dialog-message { margin: 0; color: #6b7f95; font-size: 14px; line-height: 1.7; white-space: pre-line; overflow-wrap: anywhere; }
            .sb-dialog-actions { display: flex; justify-content: center; flex-wrap: wrap; gap: 12px; margin-top: 25px; }
            .sb-dialog-btn { min-width: 116px; min-height: 44px; padding: 10px 18px; border-radius: 10px; border: 1px solid #d7e4ef; background: #fff; color: #496176; font: inherit; font-weight: 700; cursor: pointer; transition: transform .12s ease, background .12s ease, box-shadow .12s ease; }
            .sb-dialog-btn:hover { transform: translateY(-1px); background: #f5f9fc; }
            .sb-dialog-btn:focus-visible { outline: 3px solid rgba(22, 133, 232, .28); outline-offset: 2px; }
            .sb-dialog-btn.primary { border-color: #1685e8; background: #1685e8; color: #fff; box-shadow: 0 5px 14px rgba(22, 133, 232, .18); }
            .sb-dialog-btn.primary:hover { background: #0875d5; }
            .sb-dialog-btn.danger { border-color: #e63246; background: #e63246; color: #fff; box-shadow: 0 5px 14px rgba(230, 50, 70, .16); }
            .sb-dialog-btn.danger:hover { background: #c92135; }
            @keyframes sbDialogFadeIn { from { opacity: 0; } to { opacity: 1; } }
            @keyframes sbDialogRise { from { opacity: 0; transform: translateY(8px) scale(.985); } to { opacity: 1; transform: translateY(0) scale(1); } }
            @media (max-width: 520px) { .sb-dialog-card { padding: 24px 20px 22px; border-radius: 16px; } .sb-dialog-title { font-size: 19px; } .sb-dialog-actions { gap: 8px; } .sb-dialog-btn { flex: 1 1 110px; } }
            @media (prefers-reduced-motion: reduce) { .sb-dialog-overlay, .sb-dialog-card { animation: none; } .sb-dialog-btn { transition: none; } }
        `;
        document.head.appendChild(style);
    }

    function showNext() {
        if (activeDialog || queue.length === 0) return;
        const task = queue.shift();
        ensureStyles();

        const overlay = document.createElement("div");
        overlay.className = "sb-dialog-overlay";
        overlay.setAttribute("role", "presentation");

        const card = document.createElement("section");
        card.className = "sb-dialog-card";
        card.setAttribute("role", task.type === "confirm" ? "alertdialog" : "dialog");
        card.setAttribute("aria-modal", "true");
        card.setAttribute("aria-labelledby", "sb-dialog-title");
        card.setAttribute("aria-describedby", "sb-dialog-message");

        const icon = document.createElement("div");
        icon.className = `sb-dialog-icon${task.options.danger ? " danger" : task.options.variant === "warning" ? " warning" : ""}`;
        icon.setAttribute("aria-hidden", "true");
        icon.textContent = task.options.icon || (task.type === "confirm" ? "?" : (task.options.variant === "error" ? "!" : "✓"));

        const title = document.createElement("h2");
        title.className = "sb-dialog-title";
        title.id = "sb-dialog-title";
        title.textContent = task.options.title || (task.type === "confirm" ? "Xác nhận" : "Thông báo");

        const message = document.createElement("p");
        message.className = "sb-dialog-message";
        message.id = "sb-dialog-message";
        message.textContent = String(task.message ?? "");

        const actions = document.createElement("div");
        actions.className = "sb-dialog-actions";

        function finish(result) {
            if (!activeDialog || activeDialog.overlay !== overlay) return;
            document.removeEventListener("keydown", onKeyDown, true);
            overlay.remove();
            activeDialog = null;
            if (task.type === "confirm") task.resolve(Boolean(result));
            else task.resolve();
            showNext();
        }
        function onKeyDown(event) {
            if (event.key === "Escape") {
                event.preventDefault();
                finish(false);
            } else if (event.key === "Tab") {
                const controls = Array.from(card.querySelectorAll("button:not([disabled])"));
                if (!controls.length) return;
                const first = controls[0], last = controls[controls.length - 1];
                if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
                else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
            }
        }

        if (task.type === "confirm") {
            const cancel = document.createElement("button");
            cancel.type = "button";
            cancel.className = "sb-dialog-btn";
            cancel.textContent = task.options.cancelText || "Hủy";
            cancel.addEventListener("click", () => finish(false));
            actions.appendChild(cancel);
        }

        const accept = document.createElement("button");
        accept.type = "button";
        accept.className = `sb-dialog-btn ${task.options.danger ? "danger" : "primary"}`;
        accept.textContent = task.type === "confirm" ? (task.options.confirmText || "Xác nhận") : (task.options.closeText || "Đóng");
        accept.addEventListener("click", () => finish(true));
        actions.appendChild(accept);

        card.append(icon, title, message, actions);
        overlay.appendChild(card);
        overlay.addEventListener("click", event => { if (event.target === overlay && task.type === "confirm") finish(false); });
        document.body.appendChild(overlay);
        activeDialog = { overlay, finish };
        document.addEventListener("keydown", onKeyDown, true);
        accept.focus();
    }

    function enqueue(type, message, options) {
        return new Promise(resolve => {
            queue.push({ type, message, options: options || {}, resolve });
            showNext();
        });
    }

    window.SmartbusDialog = {
        alert(message, options) { return enqueue("alert", message, options); },
        confirm(message, options) { return enqueue("confirm", message, options); }
    };

    // Catch any remaining window.alert calls so the browser's native alert is never shown.
    window.alert = function (message) {
        void window.SmartbusDialog.alert(String(message ?? ""));
    };
})();
