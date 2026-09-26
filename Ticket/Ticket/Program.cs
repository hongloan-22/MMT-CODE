using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Ticket.Data;
using Ticket.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình Database InMemory phục vụ kiểm thử
bool useInMemory = builder.Configuration.GetValue<bool>("UseInMemory", true);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("TicketDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

var app = builder.Build();

// Khởi tạo Database và nạp Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    if (useInMemory)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Bỏ qua lỗi Migrate CSDL Local: {ex.Message}");
        }
    }

    try
    {
        // 1. Chèn danh sách Trạm dừng mẫu nếu chưa có
        if (!db.BusStops.Any())
        {
            var stops = new List<BusStop>
            {
                new BusStop { Name = "Bến xe Gia Lâm", Address = "Ngô Gia Tự, Long Biên" },
                new BusStop { Name = "Trạm Long Biên", Address = "Yên Phụ, Ba Đình" },
                new BusStop { Name = "Bến xe Yên Nghĩa", Address = "Quang Trung, Hà Đông" },
                new BusStop { Name = "Cổng Trường ĐH ICTU", Address = "Đường Z115, Thái Nguyên" }
            };
            db.BusStops.AddRange(stops);
            db.SaveChanges();
        }

        // 2. Chèn danh sách Tuyến xe mẫu nếu chưa có (US-15)
        if (!db.BusRoutes.Any())
        {
            var routes = new List<BusRoute>
            {
                new BusRoute
                {
                    RouteName = "Tuyến 01: Bến xe Gia Lâm - Bến xe Yên Nghĩa",
                    TotalDistanceKm = 28.5,
                    EstimatedDuration = TimeSpan.FromHours(1.5),
                    IsActive = true
                },
                new BusRoute
                {
                    RouteName = "Tuyến 02: Bến xe Mỹ Đình - Đại học ICTU",
                    TotalDistanceKm = 15.0,
                    EstimatedDuration = TimeSpan.FromMinutes(45),
                    IsActive = true
                }
            };
            db.BusRoutes.AddRange(routes);
            db.SaveChanges();
        }
        // 3. Seed chuyến xe mẫu để test Lịch trình (US-24)
        if (!db.Trips.Any())
        {
            var route = db.BusRoutes.FirstOrDefault();
            if (route != null)
            {
                db.Trips.Add(new Trip
                {
                    TripCode = "TRIP01",
                    RouteId = route.Id,
                    Status = "Active"
                });
                db.SaveChanges();
                Console.WriteLine("--> Seed Trip mẫu thành công với TripId = 1");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> [Lỗi Seed Data]: {ex.Message}");
    }
}

// Kích hoạt Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket API v1");
    c.RoutePrefix = "swagger";
});

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();