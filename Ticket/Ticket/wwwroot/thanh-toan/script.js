// SmartBus - giao diện chọn phương thức và khởi tạo thanh toán
const params = new URLSearchParams(window.location.search);
const tripId = params.get('tripId') || params.get('tripCode') || '';
const holdId = params.get('holdId') || '';
const seats = (params.get('seats') || '').split(',').map(x => x.trim()).filter(Boolean);
const total = Number(params.get('total') || 0);
const expiresAtStr = params.get('expiresAt') || '';
const money = n => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(Number(n) || 0) + ' ₫';
const notes = {
    VNPay: 'Bạn sẽ được chuyển đến cổng VNPay để hoàn tất giao dịch trong môi trường thanh toán được cấu hình.',
    MoMo: 'Bạn sẽ được chuyển đến cổng MoMo. Kiểm tra số tiền và thông tin đơn hàng trước khi xác nhận.',
    ZaloPay: 'Bạn sẽ được chuyển đến cổng ZaloPay để tiếp tục thanh toán.',
    Bank: 'Hệ thống sẽ hiển thị thông tin chuyển khoản. Chỉ chuyển đúng số tiền và nội dung được cung cấp.'
};

function showMessage(message, type = 'error') {
    const el = document.getElementById('paymentAlert');
    if (!el) return alert(message);
    el.className = `payment-alert ${type}`;
    el.textContent = message;
    el.hidden = false;
    el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}
function setButtonLoading(loading, method) {
    const btn = document.getElementById('payBtn');
    if (!btn) return;
    btn.disabled = loading;
    btn.innerHTML = loading
        ? `<span class="spinner" aria-hidden="true"></span> Đang khởi tạo thanh toán…`
        : `Thanh toán <strong id="payAmount">${money(total)}</strong><span aria-hidden="true">→</span>`;
    if (loading) btn.setAttribute('aria-label', `Đang khởi tạo thanh toán ${method}`);
    else btn.removeAttribute('aria-label');
}
function updateMethod() {
    const selected = document.querySelector('input[name="paymentMethod"]:checked');
    const method = selected?.value || 'VNPay';
    document.querySelectorAll('.method').forEach(label => {
        const input = label.querySelector('input');
        const active = input?.checked === true;
        label.classList.toggle('active', active);
        label.setAttribute('aria-checked', String(active));
    });
    const note = document.querySelector('#methodNote p');
    if (note) note.textContent = notes[method] || '';
    const btn = document.getElementById('payBtn');
    if (btn && !btn.disabled) btn.innerHTML = `Thanh toán <strong id="payAmount">${money(total)}</strong><span aria-hidden="true">→</span>`;
}
function showBankTransfer(uri, transactionRef) {
    const panel = document.getElementById('bankTransferPanel');
    if (!panel) return;
    let data = {};
    try { const parsed = new URL(uri); data = Object.fromEntries(parsed.searchParams.entries()); } catch { /* giữ panel ở trạng thái hướng dẫn */ }
    document.getElementById('bankName').textContent = data.bank || 'NCB';
    document.getElementById('bankAccount').textContent = data.account || 'Thông tin chưa có';
    document.getElementById('bankAmount').textContent = money(data.amount || total);
    document.getElementById('bankContent').textContent = data.note || transactionRef || 'SmartBus';
    panel.hidden = false;
    panel.scrollIntoView({ behavior: 'smooth', block: 'center' });
    document.getElementById('copyBankInfo').onclick = async () => {
        const text = `Ngân hàng: ${data.bank || 'NCB'}\nSố tài khoản: ${data.account || ''}\nSố tiền: ${money(data.amount || total)}\nNội dung: ${data.note || transactionRef || ''}`;
        try { await navigator.clipboard.writeText(text); showMessage('Đã sao chép thông tin chuyển khoản.', 'success'); }
        catch { showMessage('Trình duyệt không cho phép sao chép tự động. Bạn hãy sao chép thông tin hiển thị.', 'info'); }
    };
}

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('seatText').textContent = seats.length ? seats.join(', ') : 'Chưa có thông tin';
    document.getElementById('priceText').textContent = money(total);
    document.getElementById('totalText').textContent = money(total);
    document.getElementById('payAmount').textContent = money(total);
    const tripText = document.getElementById('tripCodeText');
    if (tripText) tripText.textContent = tripId || 'Chưa có thông tin';

    document.querySelectorAll('input[name="paymentMethod"]').forEach(input => input.addEventListener('change', updateMethod));
    document.querySelectorAll('.method').forEach(label => label.addEventListener('keydown', e => {
        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); const input = label.querySelector('input'); if (input) { input.checked = true; input.dispatchEvent(new Event('change', { bubbles: true })); } }
    }));
    updateMethod();

    document.getElementById('payBtn').addEventListener('click', async () => {
        const selectedMethod = document.querySelector('input[name="paymentMethod"]:checked')?.value || 'VNPay';
        document.getElementById('paymentAlert').hidden = true;
        document.getElementById('bankTransferPanel').hidden = true;
        if (!holdId) return showMessage('Không tìm thấy mã giữ chỗ (holdId). Vui lòng quay lại chọn ghế để tạo lượt giữ chỗ mới.');
        if (!seats.length) return showMessage('Chưa có ghế trong đơn đặt vé. Vui lòng quay lại chọn ghế.');
        if (!Number.isFinite(total) || total <= 0) return showMessage('Số tiền thanh toán không hợp lệ. Vui lòng chọn ghế lại.');

        setButtonLoading(true, selectedMethod);
        try {
            const response = await fetch('/api/payment/create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    holdId, tripCode: tripId, seatIds: seats,
                    method: selectedMethod, amount: total,
                    userId: sessionStorage.getItem('userId') || null
                })
            });
            const result = await response.json().catch(() => ({}));
            const success = response.ok && (result.success ?? result.Success);
            const paymentUrl = result.paymentUrl ?? result.PaymentUrl;
            const transactionRef = result.transactionRef ?? result.TransactionRef ?? '';
            if (!success) throw new Error(result.message || result.Message || 'Không thể khởi tạo giao dịch. Vui lòng thử lại.');
            if (transactionRef) sessionStorage.setItem('lastTransRef', transactionRef);
            sessionStorage.setItem('lastPaymentMethod', selectedMethod);
            sessionStorage.setItem('lastPaymentHoldId', holdId);

            if (selectedMethod === 'Bank' && paymentUrl) {
                showBankTransfer(paymentUrl, transactionRef);
                showMessage('Đã tạo yêu cầu thanh toán. Sau khi chuyển khoản, hệ thống cần nhận được xác nhận hợp lệ từ máy chủ trước khi vé được xác nhận.', 'info');
                return;
            }
            if (paymentUrl && /^https:\/\//i.test(paymentUrl)) {
                window.location.assign(paymentUrl);
                return;
            }
            // Không tự giả lập thanh toán thành công nếu backend không trả URL cổng thanh toán.
            throw new Error(result.message || result.Message || 'Giao dịch đã được tạo nhưng chưa có URL thanh toán hợp lệ. Vui lòng kiểm tra cấu hình cổng thanh toán.');
        } catch (error) {
            console.error('[SmartBus Payment]', error);
            showMessage(error.message || 'Không thể kết nối máy chủ thanh toán. Vui lòng thử lại.');
            setButtonLoading(false, selectedMethod);
        }
    });
    initCountdownTimer();
});

function initCountdownTimer() {
    const el = document.getElementById('countdown');
    if (!el) return;
    const expiry = expiresAtStr ? new Date(expiresAtStr).getTime() : Date.now() + 10 * 60 * 1000;
    if (!Number.isFinite(expiry)) return;
    const tick = () => {
        const remaining = expiry - Date.now();
        if (remaining <= 0) {
            el.textContent = '00:00'; el.classList.add('expired');
            document.getElementById('payBtn').disabled = true;
            showMessage('Thời gian giữ chỗ đã hết. Vui lòng quay lại chọn ghế để tiếp tục.', 'error');
            clearInterval(timer);
            return;
        }
        const minutes = Math.floor(remaining / 60000), seconds = Math.floor((remaining % 60000) / 1000);
        el.textContent = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
        if (remaining < 60000) el.classList.add('urgent');
    };
    let timer = setInterval(tick, 1000);
    tick();
}
