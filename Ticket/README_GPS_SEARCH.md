 SmartBus – Tìm chuyến và theo dõi GPS

 Tính năng đã bổ sung

- Tìm chuyến theo điểm đi, điểm đến và ngày đi qua API `/api/TraCuuTuyenXe/theo-diem`.
- Hiển thị giá vé, thời lượng, quãng đường và số ghế trống; chọn chuyến để vẽ lộ trình theo thứ tự các trạm có tọa độ GPS.
- Bản đồ Leaflet/OpenStreetMap hiển thị các trạm và vị trí xe được cập nhật 5 giây/lần.
- Popup vị trí xe hiển thị tốc độ, thời điểm cập nhật và trạm gần nhất (tính theo khoảng cách đường chim bay).
- API gửi GPS được bảo vệ bằng header `X-Tracking-Key`.

 Cấu hình khóa GPS

Không commit khóa thật vào `appsettings.json`. Đặt khóa bí mật bằng biến môi trường ASP.NET Core.

**PowerShell (chỉ phiên terminal hiện tại):**

```powershell
$env:Tracking__ApiKey = "THAY_BANG_CHUOI_BI_MAT_DAI_NGAU_NHIEN"
dotnet run
```

Hoặc tạo User Secrets trong thư mục dự án:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Tracking:ApiKey" "THAY_BANG_CHUOI_BI_MAT_DAI_NGAU_NHIEN"
dotnet run
```

Nhập đúng giá trị khóa trên trang `/tai-xe-phu-xe/gps-gui-vi.html`. Nếu khóa chưa được cấu hình, API gửi GPS trả về HTTP 503; nếu sai khóa, trả về HTTP 401.

 Chạy thử

1. Cài .NET 8 SDK và mở `Ticket.csproj`.
2. Cấu hình khóa như hướng dẫn trên, sau đó chạy `dotnet run`.
3. Mở `/tra-cuu/index.html` qua URL do ứng dụng cung cấp (ưu tiên HTTPS hoặc localhost).
4. Nhập điểm đi, điểm đến, ngày đi rồi chọn **Tìm chuyến**.
5. Chọn **Xem lộ trình trên bản đồ**. Để vẽ được đường, chuyến cần có ít nhất hai trạm trong `TripSchedules` có `Latitude` và `Longitude` hợp lệ.
6. Mở `/tai-xe-phu-xe/gps-gui-vi.html` trên thiết bị có GPS, nhập `TripId` hợp lệ và khóa, bấm **Bắt đầu chia sẻ GPS**. Quay lại bản đồ; vị trí sẽ được làm mới định kỳ.

## Dữ liệu cần có

- Trạm (`BusStops`): `Name`, `Latitude`, `Longitude`, `IsActive`.
- Chuyến (`Trips`): đang hoạt động, có `TripSchedules` sắp thứ tự đúng (`StopOrder`).
- Các trạm của chuyến cần tọa độ GPS. Thiếu tọa độ thì chuyến vẫn có thể tìm thấy nhưng không thể vẽ đầy đủ lộ trình.

## API

- `POST /api/tracking/position` – nhận GPS, bắt buộc header `X-Tracking-Key`.
- `GET /api/tracking/trips/{tripId}/position` – vị trí mới nhất của chuyến.
- `GET /api/tracking/trips/{tripId}/route` – danh sách trạm có tọa độ theo thứ tự chạy.
- `GET /api/tracking/active` – vị trí được cập nhật trong 10 phút gần nhất.

## Giới hạn và bảo mật

Khóa dùng chung là lớp bảo vệ tối thiểu cho bản demo, không thay thế xác thực tài khoản tài xế, phân quyền theo chuyến, HTTPS, giới hạn tần suất, kiểm tra vị trí bất thường và nhật ký truy cập. Trước khi đưa lên Internet, nên thay khóa dùng chung bằng xác thực tài xế và quyền gửi GPS theo chuyến. GPS trình duyệt chỉ hoạt động trên HTTPS hoặc `localhost`; bản đồ nền cần Internet.
