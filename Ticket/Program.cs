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