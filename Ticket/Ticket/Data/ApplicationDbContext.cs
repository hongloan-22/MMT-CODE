using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Ticket.Models;

namespace Ticket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // AUTH & NGƯỜI DÙNG
        // =========================
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        // =========================
        // TUYẾN XE & TRẠM XE
        // =========================
        public DbSet<BusRoute> BusRoutes { get; set; } = null!;
        public DbSet<BusStop> BusStops { get; set; } = null!;
        public DbSet<RouteStop> RouteStops { get; set; } = null!;
        public DbSet<Station> Stations { get; set; } = null!;
        public DbSet<Ticket.Models.Route> Routes { get; set; } = null!;

        // =========================
        // CHUYẾN XE & LỊCH TRÌNH
        // =========================
        public DbSet<Trip> Trips { get; set; } = null!;
        public DbSet<TripSchedule> TripSchedules { get; set; } = null!;
        public DbSet<BusSchedule> BusSchedules { get; set; } = null!;

        // =========================
        // XE, GHẾ & GIỮ CHỖ (SPRINT 2 - US-52, US-58)
        // =========================
        public DbSet<Bus> Buses { get; set; } = null!;
        public DbSet<Seat> Seats { get; set; } = null!;
        public DbSet<SeatHold> SeatHolds { get; set; } = null!;

        // =========================
        // THANH TOAN (SPRINT 3 - US06)
        // =========================
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Phân quyền & Tài khoản
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleCode)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Tuyến xe -> Điểm dừng
            modelBuilder.Entity<RouteStop>()
                .HasOne(x => x.Route)
                .WithMany(x => x.RouteStops)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasOne(x => x.BusStop)
                .WithMany(x => x.RouteStops)
                .HasForeignKey(x => x.StopId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Chuyến xe (Trip)
            modelBuilder.Entity<Trip>()
                .HasIndex(x => x.TripCode)
                .IsUnique();

            modelBuilder.Entity<Trip>()
                .HasOne(x => x.Route)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Liên kết Chuyến xe với Xe (Trip -> Bus) [US-52]
            modelBuilder.Entity<Trip>()
                .HasOne(x => x.Bus)
                .WithMany(b => b.Trips)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.SetNull);

            // 4. Lịch trình chuyến xe (TripSchedule)
            modelBuilder.Entity<TripSchedule>()
                .HasOne(x => x.Trip)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TripSchedule>()
                .HasOne(x => x.BusStop)
                .WithMany()
                .HasForeignKey(x => x.StopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripSchedule>()
                .HasIndex(x => new { x.TripId, x.StopOrder })
                .IsUnique();

            modelBuilder.Entity<TripSchedule>()
                .HasIndex(x => new { x.TripId, x.StopId })
                .IsUnique();

            // 5. Cấu hình Xe & Ghế (US-52)
            modelBuilder.Entity<Seat>()
                .HasOne(s => s.Bus)
                .WithMany(b => b.Seats)
                .HasForeignKey(s => s.BusId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Seat>()
                .HasIndex(s => new { s.BusId, s.SeatNumber })
                .IsUnique();

            // 6. Cấu hình Phiên giữ chỗ (US-58)
            modelBuilder.Entity<SeatHold>()
                .HasIndex(sh => new { sh.TripId, sh.ExpiresAt });

            modelBuilder.Entity<SeatHold>()
                .HasIndex(sh => new { sh.TripId, sh.ExpiresAt });

            // 7. Cấu hình giao dịch thanh toán (Sprint 3 - US06)
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.TransactionCode)
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.OrderCode);

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.HoldId);

            // Nạp dữ liệu mẫu
            SeedData(modelBuilder);
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleCode = "ADMIN", RoleName = "Quản trị hệ thống", Description = "Toàn quyền quản lý tài khoản, phân quyền và xem nhật ký an ninh", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 2, RoleCode = "MANAGER", RoleName = "Quản lý", Description = "Quản lý tuyến xe, lịch chạy, điều xe, duyệt ưu đãi và xem báo cáo", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 3, RoleCode = "DRIVER", RoleName = "Tài xế", Description = "Sử dụng app di động để quét mã QR soát vé và cập nhật sự cố trễ chuyến", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 4, RoleCode = "USER", RoleName = "Người dùng", Description = "Tra cứu tuyến, chọn ghế, thanh toán vé, đăng ký vé tháng và xem bản đồ GPS", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) }
            );

            // Mật khẩu mặc định cho tất cả tài khoản là: 123456
            const string defaultPassword = "123456";

            const string saltAdmin = "a1b2c3d4";
            const string saltManager = "b2c3d4e5";
            const string saltDriver = "c3d4e5f6";
            const string saltUser = "e5f6g7h8";

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = "usr-adm-001",
                    FullName = "Admin Quản Trị",
                    Email = "admin@gmail.com",
                    Salt = saltAdmin,
                    PasswordHash = ComputeHash(defaultPassword + saltAdmin),
                    RoleId = 1,
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = "usr-opr-001",
                    FullName = "Quản Lý Vận Hành",
                    Email = "manager@gmail.com",
                    Salt = saltManager,
                    PasswordHash = ComputeHash(defaultPassword + saltManager),
                    PhoneEncrypted = "enc_aes_0972345678",
                    IdentityCardEncrypted = "enc_aes_QL01",
                    RoleId = 2,
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = "usr-drv-001",
                    FullName = "Lê Văn Tài",
                    Email = "taixe@gmail.com",
                    Salt = saltDriver,
                    PasswordHash = ComputeHash(defaultPassword + saltDriver),
                    PhoneEncrypted = "enc_aes_0963456789",
                    IdentityCardEncrypted = "enc_aes_TX01",
                    RoleId = 3,
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = "usr-cus-001",
                    FullName = "Hoàng Minh Đức",
                    Email = "user@gmail.com",
                    Salt = saltUser,
                    PasswordHash = ComputeHash(defaultPassword + saltUser),
                    PhoneEncrypted = "enc_aes_0915678901",
                    IdentityCardEncrypted = "enc_aes_NV01",
                    RoleId = 4,
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 5, 10, 15, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<AuditLog>().HasData(
                new AuditLog
                {
                    LogId = 1,
                    UserId = "usr-adm-001",
                    Action = "Khởi tạo hệ thống",
                    IpAddress = "127.0.0.1",
                    CreatedAt = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc)
                }
            );

            // ============================================================
            // DU LIEU MAU GIAO DICH THANH TOAN (Sprint 3 - US06)
            // Bao gom day du cac trang thai: PENDING/SUCCESS/FAILED/CANCELLED + Cash
            // ============================================================
            modelBuilder.Entity<PaymentTransaction>().HasData(
                // 1. VNPay - thanh cong
                new PaymentTransaction
                {
                    Id = 1,
                    TransactionCode = "SBGD-20261005-00001",
                    OrderCode = "SBGD2026100500001",
                    Provider = "VNPay",
                    HoldId = "seed-hold-success-vnpay",
                    TripCode = "TRIP01",
                    SeatIds = "A1,A2",
                    UserId = "usr-cus-001",
                    Method = PaymentMethod.VNPay,
                    Amount = 150000m,
                    Status = PaymentStatus.Success,
                    CreatedAt = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc),
                    ExpiresAt = new DateTime(2026, 10, 5, 8, 15, 0, DateTimeKind.Utc),
                    PaidAt = new DateTime(2026, 10, 5, 8, 5, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 10, 5, 8, 5, 0, DateTimeKind.Utc),
                    ProviderTransactionId = "VNP14000001",
                    GatewayResponseCode = "00",
                    GatewayResponseMessage = "Giao dịch thành công",
                    Note = "Mẫu: thanh toán VNPay thành công"
                },
                // 2. MoMo - dang cho
                new PaymentTransaction
                {
                    Id = 2,
                    TransactionCode = "SBGD-20261005-00002",
                    OrderCode = "SBGD2026100500002",
                    Provider = "MoMo",
                    HoldId = "seed-hold-pending-momo",
                    TripCode = "TRIP01",
                    SeatIds = "B1",
                    UserId = "usr-cus-001",
                    Method = PaymentMethod.MoMo,
                    Amount = 50000m,
                    Status = PaymentStatus.Pending,
                    CreatedAt = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc),
                    ExpiresAt = new DateTime(2026, 12, 31, 23, 59, 0, DateTimeKind.Utc),
                    Note = "Mẫu: giao dịch MoMo đang chờ thanh toán"
                },
                // 3. ZaloPay - that bai
                new PaymentTransaction
                {
                    Id = 3,
                    TransactionCode = "SBGD-20261006-00001",
                    OrderCode = "SBGD2026100600001",
                    Provider = "ZaloPay",
                    HoldId = "seed-hold-failed-zalo",
                    TripCode = "TRIP01",
                    SeatIds = "C1,C2",
                    UserId = "usr-cus-001",
                    Method = PaymentMethod.ZaloPay,
                    Amount = 100000m,
                    Status = PaymentStatus.Failed,
                    CreatedAt = new DateTime(2026, 10, 6, 10, 0, 0, DateTimeKind.Utc),
                    ExpiresAt = new DateTime(2026, 10, 6, 10, 15, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 10, 6, 10, 3, 0, DateTimeKind.Utc),
                    GatewayResponseCode = "-49",
                    GatewayResponseMessage = "Thanh toán thất bại",
                    Note = "Mẫu: thanh toán ZaloPay thất bại"
                },
                // 4. BankTransfer - da huy / het han
                new PaymentTransaction
                {
                    Id = 4,
                    TransactionCode = "SBGD-20261007-00001",
                    OrderCode = "SBGD2026100700001",
                    Provider = "BankTransfer",
                    HoldId = "seed-hold-cancelled-bank",
                    TripCode = "TRIP01",
                    SeatIds = "D1,D2",
                    UserId = "usr-cus-001",
                    Method = PaymentMethod.BankTransfer,
                    Amount = 200000m,
                    Status = PaymentStatus.Cancelled,
                    CreatedAt = new DateTime(2026, 10, 7, 11, 0, 0, DateTimeKind.Utc),
                    ExpiresAt = new DateTime(2026, 10, 7, 11, 15, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 10, 7, 11, 20, 0, DateTimeKind.Utc),
                    Note = "Mẫu: hết thời gian thanh toán"
                },
                // 5. Cash - thanh cong ngay
                new PaymentTransaction
                {
                    Id = 5,
                    TransactionCode = "SBGD-20261008-00001",
                    OrderCode = "SBGD2026100800001",
                    Provider = "Cash",
                    HoldId = "seed-hold-cash-trip01",
                    TripCode = "TRIP01",
                    SeatIds = "A3",
                    UserId = "usr-cus-001",
                    Method = PaymentMethod.Cash,
                    Amount = 50000m,
                    Status = PaymentStatus.Success,
                    CreatedAt = new DateTime(2026, 10, 8, 7, 30, 0, DateTimeKind.Utc),
                    ExpiresAt = new DateTime(2026, 10, 8, 7, 45, 0, DateTimeKind.Utc),
                    PaidAt = new DateTime(2026, 10, 8, 7, 30, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 10, 8, 7, 30, 0, DateTimeKind.Utc),
                    Note = "Mẫu: thanh toán tiền mặt thành công ngay"
                }
            );
        }
    }
}