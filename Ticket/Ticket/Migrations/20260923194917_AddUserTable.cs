using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ticket.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    salt = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    phone_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    identity_card_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    log_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "created_at", "description", "role_code", "role_name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Toàn quyền quản lý tài khoản, phân quyền và xem nhật ký an ninh", "ADMIN", "Quản trị hệ thống" },
                    { 2, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Quản lý tuyến xe, lịch chạy, điều xe, duyệt ưu đãi và xem báo cáo", "MANAGER", "Quản lý" },
                    { 3, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Sử dụng app di động để quét mã QR soát vé và cập nhật sự cố trễ chuyến", "DRIVER", "Tài xế" },
                    { 4, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Tra cứu tuyến, chọn ghế, thanh toán vé, đăng ký vé tháng và xem bản đồ GPS", "USER", "Người dùng" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "user_id", "created_at", "email", "full_name", "identity_card_encrypted", "password_hash", "phone_encrypted", "role_id", "salt", "status", "updated_at" },
                values: new object[,]
                {
                    { "usr-adm-001", new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "admin@gmail.com", "Admin", "enc_aes_ADM01", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", "enc_aes_0981234567", 1, "a1b2c3d4", "ACTIVE", new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7946) },
                    { "usr-cus-001", new DateTime(2026, 9, 5, 10, 15, 0, 0, DateTimeKind.Utc), "user@gmail.com", "Hoàng Minh Đức", "enc_aes_NV01", "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", "enc_aes_0915678901", 4, "e5f6g7h8", "ACTIVE", new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7995) },
                    { "usr-drv-001", new DateTime(2026, 9, 2, 9, 0, 0, 0, DateTimeKind.Utc), "taixe@gmail.com", "Lê Văn Tài", "enc_aes_TX01", "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8", "enc_aes_0963456789", 3, "c3d4e5f6", "ACTIVE", new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7988) },
                    { "usr-opr-001", new DateTime(2026, 9, 1, 8, 30, 0, 0, DateTimeKind.Utc), "manager@gmail.com", "Quản Lý", "enc_aes_QL01", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "enc_aes_0972345678", 2, "b2c3d4e5", "ACTIVE", new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7980) }
                });

            migrationBuilder.InsertData(
                table: "audit_logs",
                columns: new[] { "log_id", "action", "created_at", "ip_address", "user_id" },
                values: new object[] { 1, "Khởi tạo hệ thống và gán quyền Quản lý (MANAGER) cho manager@gmail.com", new DateTime(2026, 9, 1, 8, 30, 0, 0, DateTimeKind.Utc), "127.0.0.1", "usr-adm-001" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_code",
                table: "roles",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
