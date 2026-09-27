# Smartbus - Giao diện Quản lý tuyến & trạm

Giao diện HTML/CSS/JS thuần, thiết kế đồng bộ với màu của trang Smartbus bạn đã gửi.

## Nguồn thiết kế
- Product Backlog: User Story #12 — Quản lý tuyến & trạm: quản lý có thể thêm/sửa/xóa thông tin tuyến đường, danh sách trạm dừng và giá vé.
- ERD:
  - Tuyen: ma_tuyen, ten_tuyen, diem_dau, diem_cuoi, gia_ve_co_ban, trang_thai.
  - Tram: ma_tram, ten_tram, toa_do_lat, toa_do_lng, dia_chi.
  - Tuyen_Tram: ma_tuyen, ma_tram, thu_tu_tram, khoang_cach_km.

## Chạy
Mở `index.html` bằng VS Code + Live Server.

Đây là frontend demo; dữ liệu đang nằm trong `script.js`, chưa kết nối Backend/API.
