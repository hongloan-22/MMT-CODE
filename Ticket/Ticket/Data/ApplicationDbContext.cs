using Microsoft.EntityFrameworkCore;
using Ticket.Models;

namespace Ticket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

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

            // Khởi tạo Dữ liệu Mẫu (Seed Data)
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

            // Seed Users (PasswordHash = SHA256(password + salt))
            modelBuilder.Entity<User>().HasData(
              new User
              {
                  UserId = "usr-adm-001",
                  FullName = "Admin",
                  Email = "admin@gmail.com",
                  Salt = "a1b2c3d4",
                  // Cập nhật chuỗi hash chuẩn này:
                  PasswordHash = "9058ca8b5620bb5eb2c88085b19830013ea2ab152245a58d9202f5c56582151f",
                  RoleId = 1,
                  Status = "ACTIVE",
                  CreatedAt = DateTime.UtcNow
              },
                new User
                {
                    UserId = "usr-opr-001",
                    Email = "manager@gmail.com",
                    PasswordHash = "e2a0f8b1c4112e4f0dc2fecba72da9bf747167a5bf7bf2eeac54508ecfef591d", // manager123 + b2c3d4e5
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
                    PasswordHash = "b347b5ae177db5523dc34cb740d216fce7c093a39e802a466a3d6cb46f90119e", // taixe123 + c3d4e5f6
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
                    PasswordHash = "ae473c4ee4003d7398e7a0e5b3ee581b7e42d76535542dfba0a109a138096f4b", // user123 + e5f6g7h8
                    Salt = "e5f6g7h8",
                    FullName = "Hoàng Minh Đức",
                    PhoneEncrypted = "enc_aes_0915678901",
                    IdentityCardEncrypted = "enc_aes_NV01",
                    RoleId = 4, // USER
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