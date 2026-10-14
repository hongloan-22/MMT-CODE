// Data/DbInitializer.cs

using Microsoft.EntityFrameworkCore;
using Ticket.Models;

namespace Ticket.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            ApplicationDbContext db)
        {
            await SeedRolesAndUsersAsync(db);

            // =====================================================
            // BUS STOP
            // =====================================================

            if (!await db.BusStops.AnyAsync())
            {
                db.BusStops.AddRange(
                    new BusStop
                    {
                        StopCode = "S01",
                        Name = "Bến xe Gia Lâm",
                        Address = "Ngô Gia Tự, Long Biên, Hà Nội",
                        Description = "Điểm đầu tuyến 01"
                    },

                    new BusStop
                    {
                        StopCode = "S02",
                        Name = "Trạm Long Biên",
                        Address = "Yên Phụ, Ba Đình, Hà Nội"
                    },

                    new BusStop
                    {
                        StopCode = "S03",
                        Name = "Bến xe Yên Nghĩa",
                        Address = "Quang Trung, Hà Đông, Hà Nội",
                        Description = "Điểm cuối tuyến 01"
                    },

                    new BusStop
                    {
                        StopCode = "S04",
                        Name = "Bến xe Mỹ Đình",
                        Address = "Phạm Hùng, Nam Từ Liêm, Hà Nội",
                        Description = "Điểm đầu tuyến 02"
                    },

                    new BusStop
                    {
                        StopCode = "S05",
                        Name = "Bến xe Thái Nguyên",
                        Address = "Quang Trung, TP. Thái Nguyên"
                    },

                    new BusStop
                    {
                        StopCode = "S06",
                        Name = "Cổng Trường ĐH ICTU",
                        Address = "Đường Z115, TP. Thái Nguyên",
                        Description = "Điểm cuối tuyến 02"
                    }
                );

                await db.SaveChangesAsync();
            }

            // =====================================================
            // BUS ROUTE
            // =====================================================

            if (!await db.BusRoutes.AnyAsync())
            {
                db.BusRoutes.AddRange(
                    new BusRoute
                    {
                        RouteCode = "R01",
                        RouteName =
                            "Tuyến 01: Bến xe Gia Lâm - Bến xe Yên Nghĩa",
                        StartPoint = "Bến xe Gia Lâm",
                        EndPoint = "Bến xe Yên Nghĩa",
                        Fare = 15000m,
                        TotalDistanceKm = 28.5,
                        EstimatedDuration =
                            TimeSpan.FromMinutes(60),
                        IsActive = true
                    },

                    new BusRoute
                    {
                        RouteCode = "R02",
                        RouteName =
                            "Tuyến 02: Bến xe Mỹ Đình - Cổng Trường ĐH ICTU",
                        StartPoint = "Bến xe Mỹ Đình",
                        EndPoint = "Cổng Trường ĐH ICTU",
                        Fare = 50000m,
                        TotalDistanceKm = 75,
                        EstimatedDuration =
                            TimeSpan.FromMinutes(110),
                        IsActive = true
                    }
                );

                await db.SaveChangesAsync();
            }

            // =====================================================
            // ROUTE STOP
            // =====================================================

            if (!await db.RouteStops.AnyAsync())
            {
                var routes =
                    await db.BusRoutes
                        .ToDictionaryAsync(x => x.RouteCode);

                var stops =
                    await db.BusStops
                        .ToDictionaryAsync(x => x.StopCode);

                db.RouteStops.AddRange(
                    // Tuyến 01
                    new RouteStop
                    {
                        RouteId = routes["R01"].Id,
                        StopId = stops["S01"].Id,
                        StopOrder = 1,
                        DistanceFromStartKm = 0,
                        TravelTimeFromStartMinutes = 0
                    },

                    new RouteStop
                    {
                        RouteId = routes["R01"].Id,
                        StopId = stops["S02"].Id,
                        StopOrder = 2,
                        DistanceFromStartKm = 8.5,
                        TravelTimeFromStartMinutes = 25
                    },

                    new RouteStop
                    {
                        RouteId = routes["R01"].Id,
                        StopId = stops["S03"].Id,
                        StopOrder = 3,
                        DistanceFromStartKm = 28.5,
                        TravelTimeFromStartMinutes = 60
                    },

                    // Tuyến 02
                    new RouteStop
                    {
                        RouteId = routes["R02"].Id,
                        StopId = stops["S04"].Id,
                        StopOrder = 1,
                        DistanceFromStartKm = 0,
                        TravelTimeFromStartMinutes = 0
                    },

                    new RouteStop
                    {
                        RouteId = routes["R02"].Id,
                        StopId = stops["S05"].Id,
                        StopOrder = 2,
                        DistanceFromStartKm = 70,
                        TravelTimeFromStartMinutes = 90
                    },

                    new RouteStop
                    {
                        RouteId = routes["R02"].Id,
                        StopId = stops["S06"].Id,
                        StopOrder = 3,
                        DistanceFromStartKm = 75,
                        TravelTimeFromStartMinutes = 110
                    }
                );

                await db.SaveChangesAsync();
            }

            // =====================================================
            // BUS + BUS SEATS
            // 4 cột dọc: A, B, C, D
            // Mỗi cột 4 ghế
            // Hàng cuối: E01 -> E05
            // Tổng: 21 ghế
            // =====================================================

            var bus = await db.Buses
                .FirstOrDefaultAsync(x => x.BusCode == "BUS01");

            if (bus == null)
            {
                bus = new Bus
                {
                    BusCode = "BUS01",
                    LicensePlate = "29A-12345",
                    SeatCount = 21,
                    IsActive = true
                };

                db.Buses.Add(bus);
                await db.SaveChangesAsync();
            }

            // Tạo 21 ghế nếu xe chưa có ghế
            var existingSeatCount = await db.BusSeats
                .CountAsync(x => x.BusId == bus.Id);

            if (existingSeatCount == 0)
            {
                var seats = new List<BusSeat>();

                // 4 cột dọc x 4 ghế
                for (int row = 1; row <= 4; row++)
                {
                    seats.Add(new BusSeat
                    {
                        BusId = bus.Id,
                        SeatCode = $"A{row:00}",
                        SeatType = "NORMAL",
                        IsActive = true
                    });

                    seats.Add(new BusSeat
                    {
                        BusId = bus.Id,
                        SeatCode = $"B{row:00}",
                        SeatType = "NORMAL",
                        IsActive = true
                    });

                    seats.Add(new BusSeat
                    {
                        BusId = bus.Id,
                        SeatCode = $"C{row:00}",
                        SeatType = "NORMAL",
                        IsActive = true
                    });

                    seats.Add(new BusSeat
                    {
                        BusId = bus.Id,
                        SeatCode = $"D{row:00}",
                        SeatType = "NORMAL",
                        IsActive = true
                    });
                }

                // Ghế dài cuối xe: 5 chỗ
                for (int i = 1; i <= 5; i++)
                {
                    seats.Add(new BusSeat
                    {
                        BusId = bus.Id,
                        SeatCode = $"E{i:00}",
                        SeatType = "LONG",
                        IsActive = true
                    });
                }

                db.BusSeats.AddRange(seats);

                await db.SaveChangesAsync();
            }

            // =====================================================
            // SAMPLE TRIPS
            // =====================================================

            if (!await db.Trips.AnyAsync())
            {
                var route1 =
                    await db.BusRoutes
                        .SingleAsync(x => x.RouteCode == "R01");

                var route2 =
                    await db.BusRoutes
                        .SingleAsync(x => x.RouteCode == "R02");

                var today = DateTime.Today.AddDays(1);

                db.Trips.AddRange(
                    new Trip
                    {
                        RouteId = route1.Id,
                        BusId = bus.Id,
                        TripCode = "TRIP-R01-0800",
                        TripDate = today,
                        DepartureTime =
                            new TimeSpan(8, 0, 0),
                        ArrivalTime =
                            new TimeSpan(9, 0, 0),
                        Status = "ACTIVE",
                        TotalSeats = bus.SeatCount,
                        AvailableSeats = bus.SeatCount,
                        BookedSeats = 0,
                        IsActive = true
                    },

                    new Trip
                    {
                        RouteId = route1.Id,
                        BusId = bus.Id,
                        TripCode = "TRIP-R01-1330",
                        TripDate = today,
                        DepartureTime =
                            new TimeSpan(13, 30, 0),
                        ArrivalTime =
                            new TimeSpan(14, 30, 0),
                        Status = "ACTIVE",
                        TotalSeats = bus.SeatCount,
                        AvailableSeats = bus.SeatCount,
                        BookedSeats = 0,
                        IsActive = true
                    },

                    new Trip
                    {
                        RouteId = route2.Id,
                        BusId = bus.Id,
                        TripCode = "TRIP-R02-0730",
                        TripDate = today,
                        DepartureTime =
                            new TimeSpan(7, 30, 0),
                        ArrivalTime =
                            new TimeSpan(9, 20, 0),
                        Status = "ACTIVE",
                        TotalSeats = bus.SeatCount,
                        AvailableSeats = bus.SeatCount,
                        BookedSeats = 0,
                        IsActive = true
                    }
                );

                await db.SaveChangesAsync();
            }

            // =====================================================
            // GÁN BUS CHO CÁC TRIP CŨ NẾU CHƯA CÓ BUS
            // =====================================================

            var tripsWithoutBus = await db.Trips
                .Where(x => x.BusId == null)
                .ToListAsync();

            if (tripsWithoutBus.Count > 0)
            {
                foreach (var trip in tripsWithoutBus)
                {
                    trip.BusId = bus.Id;
                    trip.TotalSeats = bus.SeatCount;
                    trip.AvailableSeats = bus.SeatCount;
                    trip.BookedSeats = 0;
                }

                await db.SaveChangesAsync();
            }

            // =====================================================
            // TRIP SCHEDULE
            // =====================================================

            if (!await db.TripSchedules.AnyAsync())
            {
                var trips =
                    await db.Trips
                        .ToListAsync();

                var routeStops =
                    await db.RouteStops
                        .OrderBy(x => x.RouteId)
                        .ThenBy(x => x.StopOrder)
                        .ToListAsync();

                foreach (var trip in trips)
                {
                    var stops =
                        routeStops
                            .Where(x => x.RouteId == trip.RouteId)
                            .ToList();

                    foreach (var routeStop in stops)
                    {
                        var arrival =
                            trip.DepartureTime.Add(
                                TimeSpan.FromMinutes(
                                    routeStop.TravelTimeFromStartMinutes));

                        var departure =
                            arrival.Add(
                                TimeSpan.FromMinutes(
                                    routeStop == stops.Last()
                                        ? 0
                                        : 5));

                        db.TripSchedules.Add(
                            new TripSchedule
                            {
                                TripId = trip.Id,
                                StopId = routeStop.StopId,
                                StopOrder =
                                    routeStop.StopOrder,
                                ArrivalTime = arrival,
                                DepartureTime = departure,
                                Status = "SCHEDULED"
                            });
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        private static async Task SeedRolesAndUsersAsync(ApplicationDbContext db)
        {
            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { RoleId = 1, RoleCode = "ADMIN", RoleName = "Quản trị viên", Description = "Quản lý hệ thống", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                    new Role { RoleId = 2, RoleCode = "MANAGER", RoleName = "Quản lý", Description = "Quản lý tuyến, xe và lịch trình", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                    new Role { RoleId = 3, RoleCode = "DRIVER", RoleName = "Tài xế", Description = "Tài khoản tài xế", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) },
                    new Role { RoleId = 4, RoleCode = "USER", RoleName = "Hành khách", Description = "Tài khoản hành khách", CreatedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc) }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Users.AnyAsync())
            {
                const string password = "123456";
                const string saltAdmin = "a1b2c3d4";
                const string saltManager = "b2c3d4e5";
                const string saltDriver = "c3d4e5f6";
                const string saltUser = "e5f6g7h8";
                var seedTime = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

                db.Users.AddRange(
                    CreateSeedUser("usr-adm-001", "admin@gmail.com", "Admin Quản Trị", "0123456789", 1, saltAdmin, password, seedTime),
                    CreateSeedUser("usr-opr-001", "manager@gmail.com", "Quản Lý Vận Hành", "0987654321", 2, saltManager, password, seedTime),
                    CreateSeedUser("usr-drv-001", "taixe@gmail.com", "Lê Văn Tài", "0963456789", 3, saltDriver, password, seedTime),
                    CreateSeedUser("usr-cus-001", "user@gmail.com", "Hoàng Minh Đức", "0915678901", 4, saltUser, password, seedTime)
                );
                await db.SaveChangesAsync();
            }
        }

        private static User CreateSeedUser(
            string userId, string email, string fullName, string phone,
            int roleId, string salt, string password, DateTime seedTime)
        {
            return new User
            {
                UserId = userId,
                Email = email,
                FullName = fullName,
                PhoneEncrypted = phone,
                RoleId = roleId,
                Salt = salt,
                PasswordHash = ComputeHash(password + salt),
                Status = "ACTIVE",
                IsActive = true,
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            };
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

    }
}