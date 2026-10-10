using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.DTOs;
using SmartBusTicketing.Services;
using System.Text.Json.Serialization;
using Ticket.Data;
using Ticket.Models;
using Ticket.Service;
using Ticket.Services;

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

// 3. Đăng ký Service tìm kiếm chuyến xe US-31
builder.Services.AddScoped<ITripService, TripServices>();

// 4. Cấu hình Database SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================================================================
// ĐĂNG KÝ CÁC SERVICE CHO GIỮ GHẾ, THANH TOÁN VÀ VÉ (US-97 -> US-107)
// =========================================================================
builder.Services.AddSingleton<ISeatBookingService, SeatBookingService>();
builder.Services.AddSingleton<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<ITicketService, TicketService>();

var app = builder.Build();

// =========================================================================
// KHỞI TẠO CSDL VÀ SEED DATA TUYẾN - TRẠM
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Tự động tạo sạch các bảng và nạp sẵn Users/Roles từ ApplicationDbContext.SeedData
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Warning] Tao CSDL: {ex.Message}");
    }

    try
    {
        // 1. Seed Trạm xe buýt chuẩn (BusStops - US-24)
        if (!db.BusStops.Any())
        {
            var stops = new List<BusStop>
            {
                new BusStop { Name = "Bến xe Gia Lâm", Address = "Ngô Gia Tự, Long Biên, Hà Nội" },
                new BusStop { Name = "Trạm Long Biên", Address = "Yên Phụ, Ba Đình, Hà Nội" },
                new BusStop { Name = "Bến xe Yên Nghĩa", Address = "Quang Trung, Hà Đông, Hà Nội" },
                new BusStop { Name = "Bến xe Mỹ Đình", Address = "Phạm Hùng, Nam Từ Liêm, Hà Nội" },
                new BusStop { Name = "Bến xe Thái Nguyên", Address = "Quang Trung, TP. Thái Nguyên" },
                new BusStop { Name = "Cổng Trường ĐH ICTU", Address = "Đường Z115, TP. Thái Nguyên" }
            };
            db.BusStops.AddRange(stops);
            db.SaveChanges();
            Console.WriteLine("--> [Seed Data] Da nap BusStops");
        }

        // 2. Seed Tuyến xe buýt chuẩn (BusRoutes - US-15, US-30)
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
                    RouteName = "Tuyến 02: Bến xe Mỹ Đình - Thái Nguyên",
                    TotalDistanceKm = 75.0,
                    EstimatedDuration = TimeSpan.FromHours(2.0),
                    IsActive = true
                }
            };
            db.BusRoutes.AddRange(routes);
            db.SaveChanges();
            Console.WriteLine("--> [Seed Data] Da nap BusRoutes");
        }

        // 3. Seed liên kết Tuyến xe - Trạm dừng (RouteStops - có thứ tự StopOrder phục vụ US-33)
        if (!db.RouteStops.Any())
        {
            var routeList = db.BusRoutes.ToList();
            var allStops = db.BusStops.ToDictionary(s => s.Name, s => s.Id);

            if (routeList.Count >= 2)
            {
                var route1 = routeList[0]; // Tuyến 01
                var route2 = routeList[1]; // Tuyến 02

                // Tuyến 01: Gia Lâm (Order 1) -> Long Biên (Order 2) -> Yên Nghĩa (Order 3)
                if (allStops.ContainsKey("Bến xe Gia Lâm") && allStops.ContainsKey("Bến xe Yên Nghĩa"))
                {
                    db.RouteStops.AddRange(
                        new RouteStop { RouteId = route1.Id, StopId = allStops["Bến xe Gia Lâm"], StopOrder = 1, DistanceFromStartKm = 0, TravelTimeFromStartMinutes = 0 },
                        new RouteStop { RouteId = route1.Id, StopId = allStops["Trạm Long Biên"], StopOrder = 2, DistanceFromStartKm = 8.5, TravelTimeFromStartMinutes = 25 },
                        new RouteStop { RouteId = route1.Id, StopId = allStops["Bến xe Yên Nghĩa"], StopOrder = 3, DistanceFromStartKm = 28.5, TravelTimeFromStartMinutes = 60 }
                    );
                }

                // Tuyến 02: Mỹ Đình (Order 1) -> Bến xe Thái Nguyên (Order 2) -> Cổng Trường ĐH ICTU (Order 3)
                if (allStops.ContainsKey("Bến xe Mỹ Đình") && allStops.ContainsKey("Cổng Trường ĐH ICTU"))
                {
                    db.RouteStops.AddRange(
                        new RouteStop { RouteId = route2.Id, StopId = allStops["Bến xe Mỹ Đình"], StopOrder = 1, DistanceFromStartKm = 0, TravelTimeFromStartMinutes = 0 },
                        new RouteStop { RouteId = route2.Id, StopId = allStops["Bến xe Thái Nguyên"], StopOrder = 2, DistanceFromStartKm = 70.0, TravelTimeFromStartMinutes = 90 },
                        new RouteStop { RouteId = route2.Id, StopId = allStops["Cổng Trường ĐH ICTU"], StopOrder = 3, DistanceFromStartKm = 75.0, TravelTimeFromStartMinutes = 110 }
                    );
                }

                db.SaveChanges();
                Console.WriteLine("--> [Seed Data] Da nap day du RouteStops theo StopOrder");
            }
        }

        // 4. Seed Trạm xe (Stations) và Chuyến xe (Trips - US-31)
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
            Console.WriteLine("--> [Seed Data] Da nap Trips mau thanh cong!");
        }

        // 4.1 Seed Xe và Ghế mẫu (US-52)
        if (!db.Buses.Any())
        {
            var bus = new Bus
            {
                LicensePlate = "29B-888.88",
                BusType = "Ghế ngồi 24 chỗ",
                TotalSeats = 24,
                IsActive = true
            };
            db.Buses.Add(bus);
            db.SaveChanges();

            // Sinh 24 ghế chuẩn sơ đồ 4 hàng x 6 cột
            var sampleSeats = new List<Seat>();
            char[] rowLetters = { 'A', 'B', 'C', 'D' };
            foreach (var r in rowLetters)
            {
                for (int c = 1; c <= 6; c++)
                {
                    sampleSeats.Add(new Seat
                    {
                        BusId = bus.Id,
                        SeatNumber = $"{r}{c}",
                        Row = r - 'A' + 1,
                        Column = c,
                        Price = 50000,
                        Status = SeatStatus.Available
                    });
                }
            }
            db.Seats.AddRange(sampleSeats);

            // Gán xe này vào chuyến TRIP01
            var trip = db.Trips.FirstOrDefault(t => t.TripCode == "TRIP01");
            if (trip != null)
            {
                trip.BusId = bus.Id;
            }

            db.SaveChanges();
            Console.WriteLine("--> [Seed Data] Da nap Bus va 24 Seats cho TRIP01!");
        }

        // 5. Seed Lịch trình chi tiết từng trạm cho Chuyến xe (TripSchedules - US-25)
        if (!db.TripSchedules.Any())
        {
            var sampleTrip = db.Trips.FirstOrDefault();
            var allStops = db.BusStops.ToDictionary(s => s.Name, s => s.Id);

            if (sampleTrip != null && allStops.ContainsKey("Bến xe Gia Lâm") && allStops.ContainsKey("Trạm Long Biên") && allStops.ContainsKey("Bến xe Yên Nghĩa"))
            {
                db.TripSchedules.AddRange(
                    new TripSchedule
                    {
                        TripId = sampleTrip.Id,
                        StopId = allStops["Bến xe Gia Lâm"],
                        StopOrder = 1,
                        ArrivalTime = new TimeSpan(8, 0, 0),
                        DepartureTime = new TimeSpan(8, 5, 0),
                        Status = "SCHEDULED"
                    },
                    new TripSchedule
                    {
                        TripId = sampleTrip.Id,
                        StopId = allStops["Trạm Long Biên"],
                        StopOrder = 2,
                        ArrivalTime = new TimeSpan(8, 30, 0),
                        DepartureTime = new TimeSpan(8, 35, 0),
                        Status = "SCHEDULED"
                    },
                    new TripSchedule
                    {
                        TripId = sampleTrip.Id,
                        StopId = allStops["Bến xe Yên Nghĩa"],
                        StopOrder = 3,
                        ArrivalTime = new TimeSpan(9, 30, 0),
                        DepartureTime = new TimeSpan(9, 35, 0),
                        Status = "SCHEDULED"
                    }
                );

                db.SaveChanges();
                Console.WriteLine("--> [Seed Data] Da nap TripSchedules cho man hinh Lap lich trinh");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> [Lỗi Seed Data]: {ex.Message}");
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
    pattern: "{controller=Home}/{action=./wwwroot/Index}/{id?}");

app.Run();