using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Ticket.Data;
using Ticket.Models;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cho phép CORS để Frontend fetch() không bị chặn
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. Controllers + Views & chống lặp tuần tự hóa JSON
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<
    ITripService, TripServices>();

builder.Services.AddScoped<
    BackendAuthorizationService>();

builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<Ticket.Service.RefundService>();
builder.Services.AddScoped<Ticket.Service.ExchangeService>();

// US-148: BÁO CÁO DOANH THU
builder.Services.AddScoped<
    IReportService, ReportService>();

builder.Services.AddHostedService<
    SeatHoldExpirationService>();

builder.Services.AddSingleton<
    RecurringScheduleGeneratorService>();

builder.Services.AddSingleton<
    IRecurringScheduleGeneratorService>(
        sp => sp.GetRequiredService<
            RecurringScheduleGeneratorService>());

builder.Services.AddHostedService(
    sp => sp.GetRequiredService<
        RecurringScheduleGeneratorService>());

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// =====================================================
// CREATE SQLITE SCHEMA + SEED DATABASE
// =====================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await db.Database.EnsureCreatedAsync();

        // Bảng theo dõi GPS: tạo bổ sung an toàn cho cả SQLite đã tồn tại.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "BusGpsPositions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_BusGpsPositions" PRIMARY KEY AUTOINCREMENT,
                "TripId" INTEGER NOT NULL,
                "Latitude" REAL NOT NULL,
                "Longitude" REAL NOT NULL,
                "SpeedKmh" REAL NULL,
                "RecordedAtUtc" TEXT NOT NULL,
                FOREIGN KEY ("TripId") REFERENCES "Trips" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_BusGpsPositions_TripId_RecordedAtUtc" ON "BusGpsPositions" ("TripId", "RecordedAtUtc");
            """);

        // Tạo bảng nếu database hiện tại chưa có bảng này.
        await db.Database.ExecuteSqlRawAsync("""
    CREATE TABLE IF NOT EXISTS "TripStaffAssignments" (
        "Id" INTEGER NOT NULL
            CONSTRAINT "PK_TripStaffAssignments"
            PRIMARY KEY AUTOINCREMENT,

        "TripId" INTEGER NOT NULL,
        "UserId" TEXT NOT NULL,
        "AssignmentType" TEXT NOT NULL,
        "AssignedAt" TEXT NOT NULL,
        "IsActive" INTEGER NOT NULL,

        CONSTRAINT "FK_TripStaffAssignments_Trips_TripId"
            FOREIGN KEY ("TripId")
            REFERENCES "Trips" ("Id")
            ON DELETE RESTRICT,

        CONSTRAINT "FK_TripStaffAssignments_users_UserId"
            FOREIGN KEY ("UserId")
            REFERENCES "users" ("user_id")
            ON DELETE RESTRICT
    );
    """);

        // Ngăn trùng vai trò trong cùng một chuyến.
        await db.Database.ExecuteSqlRawAsync("""
    CREATE UNIQUE INDEX IF NOT EXISTS
        "IX_TripStaffAssignments_TripId_AssignmentType"
    ON "TripStaffAssignments" ("TripId", "AssignmentType")
    WHERE "IsActive" = 1;
    """);

        await DbInitializer.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB ERROR] {ex.Message}");
    }
}

// 5. Cấu hình Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartBus Ticket API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Cho phép phục vụ file tĩnh trong wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();