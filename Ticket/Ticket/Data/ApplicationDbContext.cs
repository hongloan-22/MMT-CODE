using Microsoft.EntityFrameworkCore;
using Smart_Bus_Ticketing_System.Models;
using Ticket.Models;

namespace Ticket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public DbSet<BusStop> BusStops { get; set; } = null!;
        public DbSet<BusRoute> BusRoutes { get; set; } = null!;
        public DbSet<RouteStop> RouteStops { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // 3. Khởi tạo Dữ liệu Mẫu (Seed Data)
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleCode = "ADMIN", RoleName = "Quản trị hệ thống", Description = "Toàn quyền quản lý tài khoản, phân quyền và xem nhật ký an ninh", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 2, RoleCode = "MANAGER", RoleName = "Quản lý", Description = "Quản lý tuyến xe, lịch chạy, điều xe, duyệt ưu đãi và xem báo cáo", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 3, RoleCode = "DRIVER", RoleName = "Tài xế", Description = "Sử dụng app di động để quét mã QR soát vé và cập nhật sự cố trễ chuyến", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 4, RoleCode = "USER", RoleName = "Người dùng", Description = "Tra cứu tuyến, chọn ghế, thanh toán vé, đăng ký vé tháng và xem bản đồ GPS", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) }
            );

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = "usr-adm-001",
                    Email = "admin@gmail.com",
                    PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
                    Salt = "a1b2c3d4",
                    FullName = "Admin",
                    PhoneEncrypted = "enc_aes_0981234567",
                    IdentityCardEncrypted = "enc_aes_ADM01",
                    RoleId = 1, // ADMIN
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = "usr-opr-001",
                    Email = "manager@gmail.com",
                    PasswordHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    Salt = "b2c3d4e5",
                    FullName = "Quản Lý",
                    PhoneEncrypted = "enc_aes_0972345678",
                    IdentityCardEncrypted = "enc_aes_QL01",
                    RoleId = 2, // MANAGER
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = "usr-drv-001",
                    Email = "taixe@gmail.com",
                    PasswordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
                    Salt = "c3d4e5f6",
                    FullName = "Lê Văn Tài",
                    PhoneEncrypted = "enc_aes_0963456789",
                    IdentityCardEncrypted = "enc_aes_TX01",
                    RoleId = 3, // DRIVER
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = "usr-cus-001",
                    Email = "user@gmail.com",
                    PasswordHash = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
                    Salt = "e5f6g7h8",
                    FullName = "Hoàng Minh Đức",
                    PhoneEncrypted = "enc_aes_0915678901",
                    IdentityCardEncrypted = "enc_aes_NV01",
                    RoleId = 4, // USER (Mặc định)
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2026, 9, 5, 10, 15, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<AuditLog>().HasData(
                new AuditLog
                {
                    LogId = 1,
                    UserId = "usr-adm-001",
                    Action = "Khởi tạo hệ thống và gán quyền Quản lý (MANAGER) cho manager@gmail.com",
                    IpAddress = "127.0.0.1",
                    CreatedAt = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc)
                }
            );
        }
}
}