const params =
    new URLSearchParams(
        window.location.search
    );


const bookingId =
    Number(
        params.get("bookingId")
    );


const paymentId =
    Number(
        params.get("paymentId")
    );


const money =
    value =>
        new Intl.NumberFormat(
            "vi-VN"
        ).format(
            Number(value || 0)
        ) + "đ";


function getSession() {

    try {

        return JSON.parse(
            localStorage.getItem(
                "userSession"
            ) || "{}"
        );

    } catch {

        return {};
    }
}


function getUserId() {

    const session =
        getSession();

    return (
        session.userId ||
        session.UserId ||
        ""
    );
}


function headers() {

    return {
        "X-User-Id":
            getUserId()
    };
}


// =====================================================
// LOAD BOOKING + PAYMENT
// =====================================================

async function loadResult() {

    if (
        !bookingId ||
        !paymentId
    ) {

        showError(
            "Không xác định được giao dịch."
        );

        return;
    }


    try {

        const [
            bookingResponse,
            paymentResponse
        ] =
            await Promise.all([
                fetch(
                    `/api/Booking/${bookingId}`,
                    {
                        headers:
                            headers()
                    }
                ),

                fetch(
                    `/api/Payment/${paymentId}`,
                    {
                        headers:
                            headers()
                    }
                )
            ]);


        const booking =
            await bookingResponse.json()
                .catch(
                    () => ({})
                );


        const payment =
            await paymentResponse.json()
                .catch(
                    () => ({})
                );


        if (!bookingResponse.ok) {

            throw new Error(
                booking.message ||
                "Không thể tải đơn đặt vé."
            );
        }


        if (!paymentResponse.ok) {

            throw new Error(
                payment.message ||
                "Không thể tải giao dịch."
            );
        }


        // =================================================
        // HIỂN THỊ
        // =================================================

        document.getElementById(
            "seatNumber"
        ).textContent =
            (booking.seatCodes || [])
                .join(", ") ||
            "—";


        document.getElementById(
            "paymentAmount"
        ).textContent =
            money(
                payment.amount
            );


        document.getElementById(
            "paymentMethod"
        ).textContent =
            payment.provider ||
            "—";


        document.getElementById(
            "transactionCode"
        ).textContent =
            payment.transactionCode ||
            "—";


        // =================================================
        // LINK XEM VÉ
        // =================================================

        document.getElementById(
            "ticketBtn"
        ).href =
            `../ve/index.html?bookingId=${encodeURIComponent(
                bookingId
            )
            }`;


        // =================================================
        // TRẠNG THÁI
        // =================================================

        if (
            payment.status !==
            "SUCCESS"
        ) {

            showFailed(
                payment.status
            );

            return;
        }

    } catch (error) {

        console.error(error);

        showError(
            error.message ||
            "Không thể tải kết quả thanh toán."
        );
    }
}


// =====================================================
// FAILED
// =====================================================

function showFailed(status) {

    const card =
        document.getElementById(
            "resultCard"
        );


    card.classList.add(
        "failed"
    );


    document.getElementById(
        "resultIcon"
    ).textContent =
        "!";


    document.getElementById(
        "resultEyebrow"
    ).textContent =
        "THANH TOÁN CHƯA THÀNH CÔNG";


    document.getElementById(
        "resultTitle"
    ).textContent =
        "Không thể hoàn tất thanh toán";


    document.getElementById(
        "resultMessage"
    ).textContent =
        `Trạng thái giao dịch: ${status || "UNKNOWN"
        }.`;


    const ticketBtn =
        document.getElementById(
            "ticketBtn"
        );


    ticketBtn.textContent =
        "Quay lại thanh toán →";


    ticketBtn.href =
        `../thanh-toan/index.html?bookingId=${encodeURIComponent(
            bookingId
        )
        }`;


    document
        .querySelector(
            ".next-step strong"
        )
        .textContent =
        "Đơn đặt vé chưa được xác nhận";


    document
        .querySelector(
            ".next-step p"
        )
        .textContent =
        "Bạn có thể quay lại bước thanh toán nếu thời gian giữ chỗ vẫn còn.";
}


// =====================================================
// ERROR
// =====================================================

function showError(message) {

    document.getElementById(
        "resultIcon"
    ).textContent =
        "!";


    document.getElementById(
        "resultEyebrow"
    ).textContent =
        "CÓ LỖI";


    document.getElementById(
        "resultTitle"
    ).textContent =
        "Không thể tải dữ liệu";


    document.getElementById(
        "resultMessage"
    ).textContent =
        message;
}


// =====================================================
// START
// =====================================================

loadResult();
