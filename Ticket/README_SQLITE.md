# Ticket - SQLite version based on TicketSQL Server

## Nội dung chuyển đổi
- Giữ các controller, service, DTO, model và giao diện từ bản `TicketSQL Server` để giữ luồng nghiệp vụ của bản đó.
- Đổi provider EF Core từ SQL Server sang SQLite.
- Dùng database riêng `ticket-sqlite.db` để không ghi đè file `ticket.db` hiện có của bạn.
- Dùng `EnsureCreatedAsync()` để tạo cấu trúc database SQLite mới ở lần chạy đầu; dữ liệu mẫu được thêm qua `Data/DbInitializer.cs`.
- Tiền được lưu dưới dạng số nguyên cent (1 đơn vị tiền = 100 cent) và `TimeSpan` được lưu dạng ticks để tránh vấn đề kiểu `decimal`/`time` của SQLite khi so sánh/sắp xếp. Model/API vẫn sử dụng `decimal` và `TimeSpan`.
- Đã bỏ màn hình/API xem nhật ký Admin theo phạm vi đã thống nhất; phần ghi log nội bộ vẫn được giữ.
- Chuyến mẫu được tạo cho ngày mai để dễ kiểm thử tìm kiếm chuyến sau khi khởi động ở bất kỳ thời điểm nào trong ngày.

## Cách mở và chạy
1. Giải nén thư mục `Ticket`.
2. Mở `Ticket.slnx` hoặc `Ticket.csproj` bằng Visual Studio 2022 có hỗ trợ .NET 8.
3. Chờ NuGet restore hoàn tất.
4. Chạy project `Ticket` bằng HTTPS/F5.
5. Lần chạy đầu tạo file `ticket-sqlite.db` trong thư mục làm việc của ứng dụng và thêm dữ liệu mẫu.

## Tài khoản dữ liệu mẫu
Tất cả tài khoản mẫu dùng mật khẩu `123456` trong môi trường demo:
- Admin: `admin@gmail.com`
- Quản lý: `manager@gmail.com`
- Tài xế: `taixe@gmail.com`
- Hành khách: `user@gmail.com`

**Chỉ dùng các tài khoản này cho môi trường demo/local.** Mã băm mật khẩu trong source giữ theo cách triển khai của bản SQL Server nguồn; nên cải thiện bằng ASP.NET Core Identity/PasswordHasher trước khi đưa vào môi trường thật.

## Lưu ý database và migration
- Bản này loại bỏ các migration SQL Server vì không thể dùng chúng để tạo schema SQLite. Database mới được tạo bằng `EnsureCreatedAsync()`.
- Nếu thay đổi model trong giai đoạn phát triển, backup dữ liệu trước. Với database demo có thể dừng ứng dụng, đổi tên/xóa `ticket-sqlite.db`, rồi chạy lại để tạo schema mới. Việc này sẽ làm mất dữ liệu trong file đó.
- Dự án đã có `Data/DbContextFactory.cs` và package EF Core Design để tạo migration SQLite về sau. Trước khi dùng migration, cần tạo migration khởi tạo SQLite và đổi `EnsureCreatedAsync()` sang `MigrateAsync()`; không dùng `EnsureCreated` và migrations lẫn nhau trên cùng database.

## Giới hạn cần kiểm thử
- Không thể build/launch dự án trong môi trường tạo gói hiện tại vì không có .NET CLI. Cần mở trong Visual Studio, restore packages và chạy thử.
- Việc chuyển provider không tự biến mã thanh toán thành tích hợp gateway thật. Hãy coi luồng thanh toán theo đúng mức mà mã nguồn bản `TicketSQL Server` đã triển khai, và kiểm thử callback/trạng thái trước khi demo.
- Cơ chế phân quyền từ bản nguồn cần được kiểm thử kỹ; header `X-User-Id` không nên được coi là phương thức xác thực an toàn cho triển khai thật.
