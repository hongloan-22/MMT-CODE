# Giao diện chọn ghế + giữ chỗ Smartbus

Thư mục: `wwwroot/chon-ghe/`

## Chức năng giao diện
- Hiển thị sơ đồ ghế và các trạng thái: còn trống, đang chọn, đang giữ chỗ, đã đặt, không bán.
- Người dùng chọn tối đa 5 ghế.
- Nhấn **Tiếp tục thanh toán** để chuyển các ghế đã chọn sang trạng thái **GiuCho**.
- Đồng hồ đếm ngược bắt đầu từ **10:00**, theo yêu cầu Product Backlog "Giữ chỗ tạm thời".
- Hết 10 phút: ghế được trả về trạng thái `available` và giao diện báo hết hạn.
- Có nút **Hủy giữ chỗ** để trả ghế về trạng thái còn trống.

## Lưu ý tích hợp BE
Đây là logic demo frontend. Chưa tạo bảng/endpoint giữ chỗ trong C#.
Khi tích hợp backend, thời gian giữ chỗ phải được lưu và kiểm tra ở server/database để tránh hai người cùng giữ một ghế.

ERD của SmartBus dùng các trạng thái vé gồm `GiuCho`, `DaThanhToan`, `DaSoat`, `DaHuy`.
