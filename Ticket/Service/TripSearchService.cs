using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.DTOs;
using Ticket.Data;

namespace SmartBusTicketing.Services
{
    public class TripServices : ITripService
    {
        private readonly ApplicationDbContext _context;

        public TripServices(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TripResponseDto>>
            SearchTripsByOriginAsync(
                TripSearchRequestDto request)
        {
            var currentDateTime =
                DateTime.Now;

            var currentDate =
                currentDateTime.Date;

            var currentTime =
                currentDateTime.TimeOfDay;
            var trips =
                await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Route)
                        .ThenInclude(r =>
                            r!.RouteStops)
                            .ThenInclude(rs =>
                                rs.BusStop)
                    .Include(t =>
                        t.Schedules)
                        .ThenInclude(s =>
                            s.BusStop)
                    .Where(t =>
                        t.IsActive &&
                        t.Status == "ACTIVE" &&
                        t.Route != null &&
                        t.Route.IsActive)
                    .ToListAsync();


            var origin =
                request.OriginKeyword?
                    .Trim()
                    .ToLower();

            var destination =
                request.DestinationKeyword?
                    .Trim()
                    .ToLower();


            var result =
    trips

        // =============================================
        // KHÔNG HIỂN THỊ CHUYẾN ĐÃ QUÁ GIỜ KHỞI HÀNH
        // =============================================
        .Where(t =>
            t.TripDate.Date > currentDate ||

            (
                t.TripDate.Date == currentDate &&
                t.DepartureTime > currentTime
            ))

        // =============================================
        // LỌC NGÀY KHÁCH CHỌN
        // =============================================
        .Where(t =>
            !request.DepartureDate.HasValue ||
            t.TripDate.Date ==
            request.DepartureDate.Value.Date)
                    .Select(t =>
                    {
                        var schedules =
                            t.Schedules
                                .Where(s =>
                                    s.BusStop != null)
                                .OrderBy(s =>
                                    s.StopOrder)
                                .ToList();


                        var start =
                            schedules
                                .FirstOrDefault(s =>
                                    s.BusStop!
                                        .Name
                                        .ToLower()
                                        .Contains(
                                            origin ?? ""));


                        var end =
                            schedules
                                .FirstOrDefault(s =>
                                    s.BusStop!
                                        .Name
                                        .ToLower()
                                        .Contains(
                                            destination ?? ""));


                        var routeStops =
                            t.Route!
                                .RouteStops
                                .OrderBy(
                                    rs =>
                                        rs.StopOrder)
                                .ToList();


                        var routeStart =
                            start == null
                                ? null
                                : routeStops
                                    .FirstOrDefault(
                                        rs =>
                                            rs.StopId ==
                                            start.StopId);


                        var routeEnd =
                            end == null
                                ? null
                                : routeStops
                                    .FirstOrDefault(
                                        rs =>
                                            rs.StopId ==
                                            end.StopId);


                        double distanceKm = 0;


                        if (
                            routeStart != null &&
                            routeEnd != null &&
                            routeEnd
                                .DistanceFromStartKm >=
                            routeStart
                                .DistanceFromStartKm)
                        {
                            distanceKm =
                                routeEnd
                                    .DistanceFromStartKm -
                                routeStart
                                    .DistanceFromStartKm;
                        }


                        int durationMinutes = 0;


                        if (
                            start != null &&
                            end != null &&
                            end.ArrivalTime >=
                            start.DepartureTime)
                        {
                            durationMinutes =
                                (int)(
                                end.ArrivalTime -
                                start.DepartureTime
                                ).TotalMinutes;
                        }


                        return new
                        {
                            Trip = t,
                            Start = start,
                            End = end,
                            DistanceKm =
                                distanceKm,
                            DurationMinutes =
                                durationMinutes
                        };
                    })
                    .Where(x =>
                        x.Start != null &&
                        x.End != null &&
                        x.Start.StopOrder <
                        x.End.StopOrder)
                    .OrderBy(x =>
                        x.Trip.TripDate)
                    .ThenBy(x =>
                        x.Trip.DepartureTime)
                    .Select(x =>
                        new TripResponseDto
                        {
                            TripId =
                                x.Trip.Id,

                            RouteCode =
                                x.Trip.Route!
                                    .RouteCode,

                            RouteName =
                                x.Trip.Route
                                    .RouteName,

                            OriginStopName =
                                x.Start!
                                    .BusStop!
                                    .Name,

                            DestinationStopName =
                                x.End!
                                    .BusStop!
                                    .Name,

                            DepartureDateTime =
                                x.Trip
                                    .TripDate
                                    .Date +
                                x.Trip
                                    .DepartureTime,

                            ArrivalDateTime =
                                x.Trip
                                    .TripDate
                                    .Date +
                                x.End!
                                    .ArrivalTime,

                            Fare =
                                x.Trip.Route
                                    .Fare,

                            DistanceKm =
                                x.DistanceKm,

                            DurationMinutes =
                                x.DurationMinutes,

                            AvailableSeats =
                                x.Trip
                                    .AvailableSeats,

                            IsActive =
                                x.Trip.IsActive
                        })
                    .ToList();

            return result;
        }

        // =====================================================
        // THỐNG KÊ TỶ LỆ LẤP ĐẦY CHUYẾN XE (US-160)
        // =====================================================
        public async Task<TripOccupancyDto>
            GetTripOccupancyAsync(int tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.Bus)
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.Id == tripId);

            if (trip == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy chuyến xe với ID {tripId}");
            }

            int totalSeats =
                trip.TotalSeats > 0
                    ? trip.TotalSeats
                    : (trip.Bus?.SeatCount ?? 0);

            int bookedSeats = await _context.Tickets
                .Where(t =>
                    t.TripId == tripId &&
                    (t.Status == "VALID" ||
                     t.Status == "USED"))
                .CountAsync();

            var now = DateTime.UtcNow;

            int heldSeats = await _context.SeatHolds
                .Where(sh =>
                    sh.TripId == tripId &&
                    (sh.Status == "CONFIRMED" ||
                    (sh.Status == "HELD" &&
                     sh.ExpiresAt > now)))
                .CountAsync();

            int availableSeats =
                totalSeats - bookedSeats - heldSeats;

            if (availableSeats < 0)
                availableSeats = 0;

            double occupancyRate =
                totalSeats > 0
                    ? ((double)bookedSeats / totalSeats) * 100
                    : 0;

            occupancyRate =
                Math.Round(occupancyRate, 2);

            return new TripOccupancyDto
            {
                TripId = trip.Id,
                TripCode = trip.TripCode,
                TotalSeats = totalSeats,
                BookedSeats = bookedSeats,
                HeldSeats = heldSeats,
                AvailableSeats = availableSeats,
                OccupancyRate = occupancyRate
            };
        }

        // =====================================================
        // US-165 + US-166: LẤY DANH SÁCH CHUYẾN
        // (phân trang + lọc + sắp xếp + trạng thái + đếm ngược)
        // GET: /api/TraCuuTuyenXe/danh-sach
        // =====================================================
        public async Task<TripListResponseDto> GetTripListAsync(
            TripListRequestDto request)
        {
            // 1. Chuẩn hóa tham số phân trang
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            if (pageSize > 100) pageSize = 100;

            // 2. Query cơ bản
            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Route)
                    .ThenInclude(r => r!.RouteStops)
                        .ThenInclude(rs => rs.BusStop)
                .Include(t => t.Schedules)
                    .ThenInclude(s => s.BusStop)
                .Include(t => t.Bus)
                .Where(t =>
                    t.IsActive &&
                    t.Status == "ACTIVE" &&
                    t.Route != null &&
                    t.Route.IsActive &&
                    t.BusId != null)
                .AsQueryable();

            // ✨ US-166: Chỉ hiển thị chuyến còn chỗ
            if (request.OnlyAvailable == true)
            {
                query = query.Where(t => t.AvailableSeats > 0);
            }

            // Lọc theo ngày
            if (request.DepartureDate.HasValue)
            {
                var date = request.DepartureDate.Value.Date;
                query = query.Where(t => t.TripDate.Date == date);
            }

            // Lọc theo giá
            if (request.MinFare.HasValue)
            {
                query = query.Where(t =>
                    t.Route!.Fare >= request.MinFare.Value);
            }

            if (request.MaxFare.HasValue)
            {
                query = query.Where(t =>
                    t.Route!.Fare <= request.MaxFare.Value);
            }

            // 3. Load về memory
            var trips = await query.ToListAsync();

            // 4. Filter keyword + giờ
            var origin = request.OriginKeyword?.Trim().ToLower();
            var destination = request.DestinationKeyword?.Trim().ToLower();
            var minTime = request.MinDepartureTime;
            var now = DateTime.Now;

            var filtered = trips
                .Where(t => !minTime.HasValue || t.DepartureTime >= minTime.Value)
                .Select(t =>
                {
                    var schedules = t.Schedules
                        .Where(s => s.BusStop != null)
                        .OrderBy(s => s.StopOrder)
                        .ToList();

                    var start = string.IsNullOrEmpty(origin)
                        ? schedules.FirstOrDefault()
                        : schedules.FirstOrDefault(s =>
                            s.BusStop!.Name.ToLower().Contains(origin));

                    var end = string.IsNullOrEmpty(destination)
                        ? schedules.LastOrDefault()
                        : schedules.FirstOrDefault(s =>
                            s.BusStop!.Name.ToLower().Contains(destination));

                    if (start == null || end == null) return null;
                    if (start.StopOrder >= end.StopOrder) return null;

                    var routeStops = t.Route!.RouteStops
                        .OrderBy(rs => rs.StopOrder)
                        .ToList();

                    var routeStart = routeStops.FirstOrDefault(rs => rs.StopId == start.StopId);
                    var routeEnd = routeStops.FirstOrDefault(rs => rs.StopId == end.StopId);

                    double distanceKm = 0;
                    if (routeStart != null && routeEnd != null &&
                        routeEnd.DistanceFromStartKm >= routeStart.DistanceFromStartKm)
                    {
                        distanceKm = routeEnd.DistanceFromStartKm - routeStart.DistanceFromStartKm;
                    }

                    int durationMinutes = 0;
                    if (end.ArrivalTime >= start.DepartureTime)
                    {
                        durationMinutes = (int)(end.ArrivalTime - start.DepartureTime).TotalMinutes;
                    }

                    // Xử lý chuyến đêm
                    var departureDateTime = t.TripDate.Date + t.DepartureTime;
                    var arrivalDateTime = t.TripDate.Date + end.ArrivalTime;
                    if (end.ArrivalTime < t.DepartureTime)
                    {
                        arrivalDateTime = arrivalDateTime.AddDays(1);
                    }

                    var totalSeats = t.TotalSeats > 0
                        ? t.TotalSeats
                        : (t.Bus?.SeatCount ?? 0);

                    // ✨ US-166: Số phút còn lại đến giờ khởi hành
                    var minutesUntil = (int)Math.Round(
                        (departureDateTime - now).TotalMinutes);

                    // ✨ US-166: Trạng thái chuyến
                    string status;
                    if (t.AvailableSeats <= 0)
                    {
                        status = "SOLD_OUT";
                    }
                    else if (minutesUntil >= 0 && minutesUntil <= 30)
                    {
                        status = "DEPARTING_SOON";
                    }
                    else
                    {
                        status = "ACTIVE";
                    }

                    // ✨ US-166: Số trạm dừng giữa (không tính điểm đi/đến)
                    int stopCount = 0;
                    for (int i = 0; i < schedules.Count; i++)
                    {
                        var s = schedules[i];
                        if (s.StopOrder > start.StopOrder &&
                            s.StopOrder < end.StopOrder)
                        {
                            stopCount++;
                        }
                    }

                    return new TripListItemDto
                    {
                        TripId = t.Id,
                        TripCode = t.TripCode,
                        RouteCode = t.Route.RouteCode,
                        RouteName = t.Route.RouteName,
                        OriginStopName = start.BusStop!.Name,
                        DestinationStopName = end.BusStop!.Name,
                        DepartureDateTime = departureDateTime,
                        ArrivalDateTime = arrivalDateTime,
                        DurationMinutes = durationMinutes,
                        DistanceKm = distanceKm,
                        Fare = t.Route.Fare,
                        TotalSeats = totalSeats,
                        AvailableSeats = t.AvailableSeats,
                        BusCode = t.Bus?.BusCode ?? string.Empty,
                        LicensePlate = t.Bus?.LicensePlate ?? string.Empty,

                        // ✨ US-166
                        Status = status,
                        MinutesUntilDeparture = minutesUntil,
                        StopCount = stopCount,
                        OriginAddress = start.BusStop!.Address ?? string.Empty,
                        DestinationAddress = end.BusStop!.Address ?? string.Empty
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            // 5. Sắp xếp
            var sortBy = request.SortBy?.ToLower() ?? "time";
            var isDesc = string.Equals(request.SortOrder, "desc",
                StringComparison.OrdinalIgnoreCase);

            filtered = sortBy switch
            {
                "price" => isDesc
                    ? filtered.OrderByDescending(x => x.Fare).ToList()
                    : filtered.OrderBy(x => x.Fare).ToList(),
                "duration" => isDesc
                    ? filtered.OrderByDescending(x => x.DurationMinutes).ToList()
                    : filtered.OrderBy(x => x.DurationMinutes).ToList(),
                _ => isDesc
                    ? filtered.OrderByDescending(x => x.DepartureDateTime).ToList()
                    : filtered.OrderBy(x => x.DepartureDateTime).ToList()
            };

            // 6. Phân trang
            var totalCount = filtered.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 7. Trả kết quả
            return new TripListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = items
            };
        }

        // =====================================================
        // US-159: KIỂM TRA TÍNH HỢP LỆ DỮ LIỆU
        // GET: /api/Statistics/data-integrity-check
        // =====================================================
        public async Task<DataIntegrityReportDto> CheckDataIntegrityAsync()
        {
            var report = new DataIntegrityReportDto
            {
                CheckedAt = DateTime.UtcNow
            };

            var now = DateTime.UtcNow;

            var trips = await _context.Trips
                .AsNoTracking()
                .Include(t => t.Bus)
                .Where(t => t.IsActive)
                .ToListAsync();

            report.TotalTripsChecked = trips.Count;

            var bookedCountByTrip = await _context.Tickets
                .AsNoTracking()
                .Where(t => t.Status == "VALID" || t.Status == "USED")
                .GroupBy(t => t.TripId)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripId, x => x.Count);

            var heldCountByTrip = await _context.SeatHolds
                .AsNoTracking()
                .Where(sh =>
                    sh.Status == "CONFIRMED" ||
                    (sh.Status == "HELD" && sh.ExpiresAt > now))
                .GroupBy(sh => sh.TripId)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripId, x => x.Count);

            foreach (var trip in trips)
            {
                if (trip.Bus != null && trip.Bus.SeatCount > 0)
                {
                    if (trip.TotalSeats != trip.Bus.SeatCount)
                    {
                        report.Summary.SeatCountMismatch++;
                        report.Issues.Add(new DataIntegrityIssueDto
                        {
                            IssueType = "SEAT_COUNT_MISMATCH",
                            Severity = "HIGH",
                            TripId = trip.Id,
                            TripCode = trip.TripCode,
                            Description = "Trip.TotalSeats khác với Bus.SeatCount",
                            ExpectedValue = trip.Bus.SeatCount.ToString(),
                            ActualValue = trip.TotalSeats.ToString()
                        });
                    }
                }

                var actualBooked = bookedCountByTrip.TryGetValue(trip.Id, out var bc) ? bc : 0;
                if (trip.BookedSeats != actualBooked)
                {
                    report.Summary.BookedSeatsMismatch++;
                    report.Issues.Add(new DataIntegrityIssueDto
                    {
                        IssueType = "BOOKED_SEATS_MISMATCH",
                        Severity = "MEDIUM",
                        TripId = trip.Id,
                        TripCode = trip.TripCode,
                        Description = "Trip.BookedSeats khác với số vé VALID/USED thực tế",
                        ExpectedValue = actualBooked.ToString(),
                        ActualValue = trip.BookedSeats.ToString()
                    });
                }

                var actualHeld = heldCountByTrip.TryGetValue(trip.Id, out var hc) ? hc : 0;
                var expectedAvailable = trip.TotalSeats - actualBooked - actualHeld;
                if (expectedAvailable < 0) expectedAvailable = 0;

                if (trip.AvailableSeats != expectedAvailable)
                {
                    report.Summary.AvailableSeatsMismatch++;
                    report.Issues.Add(new DataIntegrityIssueDto
                    {
                        IssueType = "AVAILABLE_SEATS_MISMATCH",
                        Severity = "MEDIUM",
                        TripId = trip.Id,
                        TripCode = trip.TripCode,
                        Description = "Trip.AvailableSeats khác với công thức TotalSeats - Booked - Held",
                        ExpectedValue = expectedAvailable.ToString(),
                        ActualValue = trip.AvailableSeats.ToString()
                    });
                }
            }

            var expiredHolds = await _context.SeatHolds
                .AsNoTracking()
                .Include(sh => sh.Trip)
                .Where(sh =>
                    sh.Status == "HELD" &&
                    sh.ExpiresAt <= now)
                .ToListAsync();

            foreach (var hold in expiredHolds)
            {
                report.Summary.ExpiredSeatHolds++;
                report.Issues.Add(new DataIntegrityIssueDto
                {
                    IssueType = "EXPIRED_SEAT_HOLDS",
                    Severity = "LOW",
                    TripId = hold.TripId,
                    TripCode = hold.Trip?.TripCode,
                    SeatHoldId = hold.Id,
                    SeatCode = hold.SeatCode,
                    Description = "Ghế đang giữ (HELD) đã quá hạn nhưng chưa được release",
                    ExpectedValue = "RELEASED hoặc EXPIRED",
                    ActualValue = $"{hold.Status} (hết hạn lúc {hold.ExpiresAt:u})"
                });
            }

            report.TotalIssuesFound = report.Issues.Count;

            return report;
        }
    }
}