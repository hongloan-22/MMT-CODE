using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Cấu hình DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// =========================================================================
// SEED DATA DỰ ÁN SMART BUS TICKETING (US-15, US-23)
// Đặt TOÀN BỘ Seed Data VÀO TRONG scope VÀ TRƯỚC app.Run()
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // 1. Chèn danh sách Trạm dừng mẫu nếu chưa có
        if (!context.BusStops.Any())
        {
            var stops = new List<BusStop>
            {
                new BusStop { Name = "Bến xe Gia Lâm", Address = "Ngô Gia Tự, Long Biên" },
                new BusStop { Name = "Trạm Long Biên", Address = "Yên Phụ, Ba Đình" },
                new BusStop { Name = "Bến xe Yên Nghĩa", Address = "Quang Trung, Hà Đông" },
                new BusStop { Name = "Cổng Trường ĐH ICTU", Address = "Đường Z115, Thái Nguyên" }
            };
            context.BusStops.AddRange(stops);
            context.SaveChanges();
        }

        // 2. Chèn danh sách Tuyến xe mẫu nếu chưa có (US-15)
        if (!context.BusRoutes.Any())
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
            context.BusRoutes.AddRange(routes);
            context.SaveChanges();
            Console.WriteLine("--> [US-15] Seed dữ liệu Tuyến & Trạm thành công!");
        }

        // 3. Seed Lịch trình chạy xe (BusSchedules - US-23)
        if (!context.BusSchedules.Any())
        {
            var route1 = context.BusRoutes.FirstOrDefault();
            if (route1 != null)
            {
                var schedules = new List<BusSchedule>
                {
                    new BusSchedule
                    {
                        BusRouteId = route1.Id,
                        DepartureTime = DateTime.Now.AddHours(1),
                        ArrivalTime = DateTime.Now.AddHours(2.5),
                        AvailableSeats = 40,
                        IsActive = true
                    },
                    new BusSchedule
                    {
                        BusRouteId = route1.Id,
                        DepartureTime = DateTime.Now.AddHours(3),
                        ArrivalTime = DateTime.Now.AddHours(4.5),
                        AvailableSeats = 35,
                        IsActive = true
                    }
                };
                context.BusSchedules.AddRange(schedules);
                context.SaveChanges();
                Console.WriteLine("--> [US-23] Seed Lịch trình thành công!");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> [Lỗi Seed Data]: {ex.Message}");
    }
}

// Cấu hình MapRoute
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Lệnh chạy ứng dụng PHẢI Ở DÒNG CUỐI CÙNG
app.Run();