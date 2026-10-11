using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ticket.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundTransactions_Tickets_TicketId",
                table: "RefundTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundTransactions_users_ProcessedByUserId",
                table: "RefundTransactions");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_RefundTransactions_Status",
                table: "RefundTransactions");

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    transaction_code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    order_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    provider_transaction_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    provider = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    hold_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    trip_code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    seat_ids = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    method = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    paid_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    gateway_response_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    gateway_response_message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    gateway_signature = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    return_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ipn_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    user_ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    callback_count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "payment_transactions",
                columns: new[] { "Id", "amount", "callback_count", "completed_at", "created_at", "expires_at", "gateway_response_code", "gateway_response_message", "gateway_signature", "hold_id", "ipn_url", "method", "note", "order_code", "paid_at", "provider", "provider_transaction_id", "return_url", "seat_ids", "status", "transaction_code", "trip_code", "user_id", "user_ip_address" },
                values: new object[,]
                {
                    { 1, 150000m, 0, new DateTime(2026, 10, 5, 8, 5, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 8, 15, 0, 0, DateTimeKind.Utc), "00", "Giao dịch thành công", null, "seed-hold-success-vnpay", null, 1, "Mẫu: thanh toán VNPay thành công", "SBGD2026100500001", new DateTime(2026, 10, 5, 8, 5, 0, 0, DateTimeKind.Utc), "VNPay", "VNP14000001", null, "A1,A2", 1, "SBGD-20261005-00001", "TRIP01", "usr-cus-001", null },
                    { 2, 50000m, 0, null, new DateTime(2026, 10, 5, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 23, 59, 0, 0, DateTimeKind.Utc), null, null, null, "seed-hold-pending-momo", null, 2, "Mẫu: giao dịch MoMo đang chờ thanh toán", "SBGD2026100500002", null, "MoMo", null, null, "B1", 0, "SBGD-20261005-00002", "TRIP01", "usr-cus-001", null },
                    { 3, 100000m, 0, new DateTime(2026, 10, 6, 10, 3, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 6, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 6, 10, 15, 0, 0, DateTimeKind.Utc), "-49", "Thanh toán thất bại", null, "seed-hold-failed-zalo", null, 3, "Mẫu: thanh toán ZaloPay thất bại", "SBGD2026100600001", null, "ZaloPay", null, null, "C1,C2", 2, "SBGD-20261006-00001", "TRIP01", "usr-cus-001", null },
                    { 4, 200000m, 0, new DateTime(2026, 10, 7, 11, 20, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 7, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 7, 11, 15, 0, 0, DateTimeKind.Utc), null, null, null, "seed-hold-cancelled-bank", null, 4, "Mẫu: hết thời gian thanh toán", "SBGD2026100700001", null, "BankTransfer", null, null, "D1,D2", 3, "SBGD-20261007-00001", "TRIP01", "usr-cus-001", null },
                    { 5, 50000m, 0, new DateTime(2026, 10, 8, 7, 30, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 8, 7, 30, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 8, 7, 45, 0, 0, DateTimeKind.Utc), null, null, null, "seed-hold-cash-trip01", null, 5, "Mẫu: thanh toán tiền mặt thành công ngay", "SBGD2026100800001", new DateTime(2026, 10, 8, 7, 30, 0, 0, DateTimeKind.Utc), "Cash", null, null, "A3", 1, "SBGD-20261008-00001", "TRIP01", "usr-cus-001", null }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-adm-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 11, 19, 44, 6, 902, DateTimeKind.Utc).AddTicks(7745));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-cus-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 11, 19, 44, 6, 902, DateTimeKind.Utc).AddTicks(8327));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-drv-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 11, 19, 44, 6, 902, DateTimeKind.Utc).AddTicks(8232));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-opr-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 11, 19, 44, 6, 902, DateTimeKind.Utc).AddTicks(8125));

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_hold_id",
                table: "payment_transactions",
                column: "hold_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_order_code",
                table: "payment_transactions",
                column: "order_code");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_transaction_code",
                table: "payment_transactions",
                column: "transaction_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundTransactions_Tickets_TicketId",
                table: "RefundTransactions",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundTransactions_users_ProcessedByUserId",
                table: "RefundTransactions",
                column: "ProcessedByUserId",
                principalTable: "users",
                principalColumn: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundTransactions_Tickets_TicketId",
                table: "RefundTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundTransactions_users_ProcessedByUserId",
                table: "RefundTransactions");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GatewayOrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GatewayResponseCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    GatewayResponseMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    GatewaySignature = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    GatewayTransactionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HoldId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IpnUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReturnUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SeatIds = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionRef = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TripCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UserIpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-adm-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 10, 9, 12, 3, 217, DateTimeKind.Utc).AddTicks(5658));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-cus-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 10, 9, 12, 3, 217, DateTimeKind.Utc).AddTicks(6395));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-drv-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 10, 9, 12, 3, 217, DateTimeKind.Utc).AddTicks(6286));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-opr-001",
                column: "updated_at",
                value: new DateTime(2026, 10, 10, 9, 12, 3, 217, DateTimeKind.Utc).AddTicks(6098));

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactions_Status",
                table: "RefundTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayOrderId",
                table: "Payments",
                column: "GatewayOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_HoldId",
                table: "Payments",
                column: "HoldId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionRef",
                table: "Payments",
                column: "TransactionRef",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundTransactions_Tickets_TicketId",
                table: "RefundTransactions",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundTransactions_users_ProcessedByUserId",
                table: "RefundTransactions",
                column: "ProcessedByUserId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
