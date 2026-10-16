/* =========================================================
   CHI TIẾT CHUYẾN XE
   Smartbus - Admin / Manager
   ========================================================= */


const tripData = {

    id: "SB-HNTN-161026-01",

    routeCode: "HN-TN",

    origin: "Bến xe Mỹ Đình",

    destination: "Bến xe Thái Nguyên",

    date: "16/10/2026",

    departure: "07:30",

    arrival: "09:00",

    totalSeats: 40,

    bookedSeats: 28,

    pendingSeats: 3,

    cancelledTickets: 2,

    revenue: 4200000

};


const tickets = [

    {
        code: "SB001285",
        passenger: "Nguyễn Văn Minh",
        seat: "A01",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001286",
        passenger: "Trần Thị Lan",
        seat: "A02",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001287",
        passenger: "Lê Hoàng Nam",
        seat: "A03",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001288",
        passenger: "Phạm Thu Hà",
        seat: "B01",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001289",
        passenger: "Đỗ Minh Đức",
        seat: "B02",
        price: 150000,
        status: "pending"
    },

    {
        code: "SB001290",
        passenger: "Nguyễn Thị Mai",
        seat: "B03",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001291",
        passenger: "Hoàng Văn Tuấn",
        seat: "C01",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001292",
        passenger: "Vũ Thu Trang",
        seat: "C02",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001293",
        passenger: "Bùi Quang Huy",
        seat: "C03",
        price: 150000,
        status: "pending"
    },

    {
        code: "SB001294",
        passenger: "Nguyễn Đức Anh",
        seat: "D01",
        price: 150000,
        status: "cancelled"
    },

    {
        code: "SB001295",
        passenger: "Trần Quốc Việt",
        seat: "D02",
        price: 150000,
        status: "confirmed"
    },

    {
        code: "SB001296",
        passenger: "Đặng Thị Hương",
        seat: "D03",
        price: 150000,
        status: "confirmed"
    }

];


const statusText = {

    confirmed: "Đã xác nhận",

    pending: "Chờ thanh toán",

    cancelled: "Đã hủy"

};


function formatMoney(value) {

    return new Intl.NumberFormat(
        "vi-VN"
    ).format(value) + "đ";

}


function showToast(message) {

    const toast =
        document.getElementById("toast");

    if (!toast) {
        return;
    }

    toast.textContent = message;

    toast.classList.add("show");

    clearTimeout(
        window.__toastTimer
    );

    window.__toastTimer =
        setTimeout(() => {

            toast.classList.remove(
                "show"
            );

        }, 2200);

}


function calculateOccupancy() {

    if (!tripData.totalSeats) {
        return 0;
    }

    return Math.round(
        tripData.bookedSeats /
        tripData.totalSeats *
        100
    );

}


function renderStatistics() {

    const available =
        tripData.totalSeats -
        tripData.bookedSeats -
        tripData.pendingSeats;

    const occupancy =
        calculateOccupancy();


    const booked =
        document.getElementById(
            "bookedTickets"
        );

    const availableElement =
        document.getElementById(
            "availableSeats"
        );

    const occupancyElement =
        document.getElementById(
            "occupancyRate"
        );

    const revenueElement =
        document.getElementById(
            "tripRevenue"
        );

    const seatPercent =
        document.getElementById(
            "seatPercent"
        );


    if (booked) {
        booked.textContent =
            tripData.bookedSeats;
    }


    if (availableElement) {
        availableElement.textContent =
            Math.max(available, 0);
    }


    if (occupancyElement) {
        occupancyElement.textContent =
            occupancy + "%";
    }


    if (revenueElement) {
        revenueElement.textContent =
            (tripData.revenue / 1000000)
                .toFixed(1) + " triệu";
    }


    if (seatPercent) {
        seatPercent.textContent =
            occupancy + "%";
    }


    const progress =
        document.querySelector(
            ".progress-track span"
        );


    if (progress) {
        progress.style.width =
            occupancy + "%";
    }


    const progressNumber =
        document.querySelector(
            ".progress-head strong"
        );


    if (progressNumber) {

        progressNumber.textContent =
            tripData.bookedSeats +
            " / " +
            tripData.totalSeats;

    }

}


function renderTickets(data) {

    const tbody =
        document.getElementById(
            "ticketTableBody"
        );

    const empty =
        document.getElementById(
            "tableEmpty"
        );


    if (!tbody) {
        return;
    }


    tbody.innerHTML = "";


    if (!data.length) {

        if (empty) {
            empty.style.display =
                "flex";
        }

        return;

    }


    if (empty) {
        empty.style.display =
            "none";
    }


    data.forEach(ticket => {

        const tr =
            document.createElement(
                "tr"
            );


        tr.innerHTML = `

            <td>
                <span class="ticket-code">
                    ${ticket.code}
                </span>
            </td>

            <td>
                <span class="passenger-name">
                    ${ticket.passenger}
                </span>
            </td>

            <td>
                <span class="seat-number">
                    ${ticket.seat}
                </span>
            </td>

            <td>
                ${formatMoney(ticket.price)}
            </td>

            <td>
                <span class="ticket-status ${ticket.status}">
                    ${statusText[ticket.status]}
                </span>
            </td>

            <td>

                <button
                    class="mini-action"
                    type="button"
                    data-ticket="${ticket.code}">

                    Xem

                </button>

            </td>

        `;


        tbody.appendChild(tr);

    });


    tbody
        .querySelectorAll(
            ".mini-action"
        )
        .forEach(button => {

            button.addEventListener(
                "click",
                function () {

                    const code =
                        this.dataset.ticket;

                    showToast(
                        "Đang xem chi tiết vé " +
                        code
                    );

                }
            );

        });

}


function filterTickets() {

    const searchInput =
        document.getElementById(
            "ticketSearch"
        );

    const statusSelect =
        document.getElementById(
            "ticketStatus"
        );


    const keyword =
        searchInput
            ? searchInput.value
                .trim()
                .toLowerCase()
            : "";


    const status =
        statusSelect
            ? statusSelect.value
            : "all";


    const filtered =
        tickets.filter(ticket => {

            const matchKeyword =

                !keyword ||

                ticket.code
                    .toLowerCase()
                    .includes(keyword) ||

                ticket.passenger
                    .toLowerCase()
                    .includes(keyword) ||

                ticket.seat
                    .toLowerCase()
                    .includes(keyword);


            const matchStatus =

                status === "all" ||

                ticket.status === status;


            return (
                matchKeyword &&
                matchStatus
            );

        });


    renderTickets(filtered);

}


function exportTickets() {

    let csv =
        "Mã vé,Hành khách,Ghế,Giá vé,Trạng thái\n";


    tickets.forEach(ticket => {

        csv += [

            ticket.code,

            ticket.passenger,

            ticket.seat,

            ticket.price,

            statusText[ticket.status]

        ]
            .map(value =>
                `"${String(value)
                    .replaceAll('"', '""')}"`
            )
            .join(",");

        csv += "\n";

    });


    const blob =
        new Blob(
            ["\uFEFF" + csv],
            {
                type:
                    "text/csv;charset=utf-8;"
            }
        );


    const url =
        URL.createObjectURL(blob);


    const a =
        document.createElement("a");

    a.href = url;

    a.download =
        "danh-sach-ve-" +
        tripData.id +
        ".csv";


    document.body.appendChild(a);

    a.click();

    a.remove();

    URL.revokeObjectURL(url);


    showToast(
        "Đã xuất danh sách vé."
    );

}


function initEvents() {

    const search =
        document.getElementById(
            "ticketSearch"
        );


    const status =
        document.getElementById(
            "ticketStatus"
        );


    if (search) {

        search.addEventListener(
            "input",
            filterTickets
        );

    }


    if (status) {

        status.addEventListener(
            "change",
            filterTickets
        );

    }


    const exportBtn =
        document.getElementById(
            "exportBtn"
        );


    if (exportBtn) {

        exportBtn.addEventListener(
            "click",
            exportTickets
        );

    }


    const backBtn =
        document.getElementById(
            "backBtn"
        );


    if (backBtn) {

        backBtn.addEventListener(
            "click",
            function () {

                window.location.href =
                    "../lap-lich-trinh/index.html";

            }
        );

    }


    const vehicleBtn =
        document.getElementById(
            "vehicleBtn"
        );


    if (vehicleBtn) {

        vehicleBtn.addEventListener(
            "click",
            function () {

                window.location.href =
                    "../doi-xe/index.html";

            }
        );

    }


    const logout =
        document.querySelector(
            ".logout-btn"
        );


    if (logout) {

        logout.addEventListener(
            "click",
            function () {

                localStorage.removeItem(
                    "userSession"
                );

                window.location.href =
                    "../auth/dangnhap.html";

            }
        );

    }

}


document.addEventListener(
    "DOMContentLoaded",
    function () {

        renderStatistics();

        renderTickets(tickets);

        initEvents();

    }
);