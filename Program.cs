using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Ticket.Data;
using Ticket.Models;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Views & chống lặp tuần tự hóa JSON
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký Service tìm kiếm chuyến xe US-31
builder.Services.AddScoped<ITripService, TripServices>();

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

// Khởi tạo Database và Seed Data
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
            Console.WriteLine($"[Warning] Migrate CSDL: {ex.Message}");
        }
    }

    try
    {
        // 1. Seed Trạm xe buýt chuẩn (BusStops - US24)
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

        // 2. Seed Tuyến xe buýt chuẩn (BusRoutes - US15, US30)
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

        // 3. Seed liên kết Tuyến xe - Trạm dừng (RouteStops - phục vụ US-33 Tìm kiếm theo thời gian)
        if (!db.RouteStops.Any())
        {
            var route1 = db.BusRoutes.FirstOrDefault();
            var stops = db.BusStops.ToList();

            if (route1 != null && stops.Count >= 3)
            {
                db.RouteStops.AddRange(
                    new RouteStop
                    {
                        RouteId = route1.Id,
                        StopId = stops[0].Id,
                        StopOrder = 1,
                        DistanceFromStartKm = 0,
                        TravelTimeFromStartMinutes = 0
                    },
                    new RouteStop
                    {
                        RouteId = route1.Id,
                        StopId = stops[1].Id,
                        StopOrder = 2,
                        DistanceFromStartKm = 8.5,
                        TravelTimeFromStartMinutes = 25
                    },
                    new RouteStop
                    {
                        RouteId = route1.Id,
                        StopId = stops[2].Id,
                        StopOrder = 3,
                        DistanceFromStartKm = 28.5,
                        TravelTimeFromStartMinutes = 60
                    }
                );
                db.SaveChanges();
                Console.WriteLine("--> [Seed Data] Da nap RouteStops mau cho US-33!");
            }
        }

        // 4. Seed Trạm xe (Stations) và Chuyến xe (Trips - US31)
        if (!db.Stations.Any())
        {
            var st1 = new Station { StationId = 1, StationName = "Bến xe Gia Lâm" };
            var st2 = new Station { StationId = 2, StationName = "Bến xe Yên Nghĩa" };
            var st3 = new Station { StationId = 3, StationName = "Bến xe Mỹ Đình" };

            db.Stations.AddRange(st1, st2, st3);
            db.SaveChanges();

            var route = db.BusRoutes.FirstOrDefault();

            db.Trips.Add(new Trip
            {
                TripCode = "TRIP01",
                RouteId = route != null ? route.Id : 1,
                OriginStation = st1,
                OriginStationId = 1,
                DestinationStation = st2,
                DestinationStationId = 2,
                TripDate = DateTime.Today,
                DepartureTime = new TimeSpan(8, 0, 0),
                TotalSeats = 40,
                AvailableSeats = 35,
                BookedSeats = 5,
                Price = 50000,
                IsActive = true,
                Status = "Active"
            });
            db.SaveChanges();
            Console.WriteLine("--> [Seed Data] Da nap BusRoutes, Stations va Trip mau thanh cong!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> [Lỗi Seed Data]: {ex.Message}");
    }
}

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket API v1");
    c.RoutePrefix = "swagger";
});

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