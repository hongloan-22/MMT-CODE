using Microsoft.EntityFrameworkCore;
using SmartBusTicketing.DTOs;
using Ticket.Data;
using Ticket.Models;

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
                    .Include(t => t.Bus)
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

            var seatAvailabilityByTrip =
                await GetSeatAvailabilityByTripAsync(
                    trips,
                    DateTime.UtcNow);

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
                                seatAvailabilityByTrip.TryGetValue(
                                    x.Trip.Id,
                                    out var seatInfo)
                                    ? seatInfo.AvailableSeats
                                    : 0,

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
            // 1. Lấy thông tin chuyến xe.
            var trip = await _context.Trips
                .AsNoTracking()
                .Include(t => t.Bus)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy chuyến xe với ID {tripId}");
            }

            // 2. Sử dụng chung quy tắc tính ghế với API tìm chuyến
            // và API kiểm tra tính toàn vẹn dữ liệu.
            var nowUtc = DateTime.UtcNow;

            var seatAvailabilityByTrip =
                await GetSeatAvailabilityByTripAsync(
                    new List<Trip> { trip },
                    nowUtc);

            if (!seatAvailabilityByTrip.TryGetValue(
                    trip.Id, out var seatInfo))
            {
                throw new InvalidOperationException(
                    $"Không thể tính số ghế cho chuyến xe {tripId}");
            }

            // 3. Lấy kết quả tính ghế thống nhất.
            int totalSeats = seatInfo.TotalSeats;
            int bookedSeats = seatInfo.BookedSeats;
            int heldSeats = seatInfo.HeldSeats;
            int availableSeats = seatInfo.AvailableSeats;

            // 4. Tính tỷ lệ lấp đầy.
            // Chỉ tính ghế đã đặt thành công, không tính ghế giữ tạm.
            double occupancyRate = totalSeats > 0
                ? Math.Round(
                    (double)bookedSeats / totalSeats * 100,
                    2)
                : 0;

            // 5. Trả kết quả thống kê.
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

        private sealed class TripSeatAvailability
        {
            public int TotalSeats { get; init; }
            public int BookedSeats { get; init; }
            public int HeldSeats { get; init; }
            public int AvailableSeats { get; init; }
        }

        private async Task<Dictionary<int, TripSeatAvailability>>
            GetSeatAvailabilityByTripAsync(
                IReadOnlyCollection<Trip> trips,
                DateTime nowUtc)
        {
            var result = new Dictionary<int, TripSeatAvailability>();

            if (trips.Count == 0)
                return result;

            var tripIds = trips.Select(t => t.Id).ToList();

            var busIds = trips
                .Where(t => t.BusId.HasValue)
                .Select(t => t.BusId!.Value)
                .Distinct()
                .ToList();

            // Lấy cả ghế đang hoạt động và không hoạt động để biết
            // xe đã được cấu hình ghế hay chưa.
            var seatRows = await _context.BusSeats
                .AsNoTracking()
                .Where(s => busIds.Contains(s.BusId))
                .Select(s => new
                {
                    s.BusId,
                    s.SeatCode,
                    s.IsActive
                })
                .ToListAsync();

            var seatCodesByBus = seatRows
                .Where(s => !string.IsNullOrWhiteSpace(s.SeatCode))
                .GroupBy(s => s.BusId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        HasConfiguration = true,
                        ActiveSeatCodes = g
                            .Where(s => s.IsActive)
                            .Select(s => s.SeatCode.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase)
                    });

            var bookedCodesByTrip =
                new Dictionary<int, HashSet<string>>();

            var heldCodesByTrip =
                new Dictionary<int, HashSet<string>>();

            void AddSeatCode(
                Dictionary<int, HashSet<string>> target,
                int tripId,
                string? seatCode)
            {
                if (string.IsNullOrWhiteSpace(seatCode))
                    return;

                if (!target.TryGetValue(tripId, out var seatCodes))
                {
                    seatCodes = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);
                    target[tripId] = seatCodes;
                }

                seatCodes.Add(seatCode.Trim());
            }

            // Ghế đã có vé hợp lệ hoặc đã sử dụng.
            var ticketRows = await _context.Tickets
                .AsNoTracking()
                .Where(t =>
                    tripIds.Contains(t.TripId) &&
                    (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => new
                {
                    t.TripId,
                    t.SeatCode
                })
                .ToListAsync();

            foreach (var ticket in ticketRows)
            {
                AddSeatCode(
                    bookedCodesByTrip,
                    ticket.TripId,
                    ticket.SeatCode);
            }

            // Ghế có SeatHold và Booking đã được xác nhận.
            var confirmedRows = await _context.SeatHolds
                .AsNoTracking()
                .Where(h =>
                    tripIds.Contains(h.TripId) &&
                    h.Status == "CONFIRMED" &&
                    h.Booking != null &&
                    h.Booking.Status == "CONFIRMED")
                .Select(h => new
                {
                    h.TripId,
                    h.SeatCode
                })
                .ToListAsync();

            foreach (var hold in confirmedRows)
            {
                AddSeatCode(
                    bookedCodesByTrip,
                    hold.TripId,
                    hold.SeatCode);
            }

            // Ghế giữ tạm còn hạn, gắn với booking vẫn ở trạng thái HELD.
            var activeHoldRows = await _context.SeatHolds
                .AsNoTracking()
                .Where(h =>
                    tripIds.Contains(h.TripId) &&
                    h.Status == "HELD" &&
                    h.ExpiresAt > nowUtc &&
                    h.Booking != null &&
                    h.Booking.Status == "HELD")
                .Select(h => new
                {
                    h.TripId,
                    h.SeatCode
                })
                .ToListAsync();

            foreach (var hold in activeHoldRows)
            {
                AddSeatCode(
                    heldCodesByTrip,
                    hold.TripId,
                    hold.SeatCode);
            }

            foreach (var trip in trips)
            {
                var hasSeatConfiguration = false;
                HashSet<string>? activeSeatCodes = null;

                if (trip.BusId.HasValue &&
                    seatCodesByBus.TryGetValue(
                        trip.BusId.Value,
                        out var configuredSeats))
                {
                    hasSeatConfiguration = true;
                    activeSeatCodes = configuredSeats.ActiveSeatCodes;
                }

                // Thứ tự nguồn dữ liệu thống nhất:
                // 1) Cấu hình ghế BusSeats nếu đã có;
                // 2) Sức chứa của xe Bus;
                // 3) Trip.TotalSeats làm giá trị dự phòng cuối cùng.
                int busSeatCount = trip.Bus?.SeatCount ?? 0;

                int totalSeats = hasSeatConfiguration
                    ? activeSeatCodes!.Count
                    : (busSeatCount > 0
                        ? busSeatCount
                        : trip.TotalSeats);

                var bookedCodes =
                    bookedCodesByTrip.TryGetValue(
                        trip.Id,
                        out var booked)
                        ? booked
                        : new HashSet<string>(
                            StringComparer.OrdinalIgnoreCase);

                var heldCodes =
                    heldCodesByTrip.TryGetValue(
                        trip.Id,
                        out var held)
                        ? held
                        : new HashSet<string>(
                            StringComparer.OrdinalIgnoreCase);

                int bookedSeats = bookedCodes.Count(code =>
                    activeSeatCodes == null ||
                    activeSeatCodes.Contains(code));

                int heldSeats = heldCodes.Count(code =>
                    (activeSeatCodes == null ||
                     activeSeatCodes.Contains(code)) &&
                    !bookedCodes.Contains(code));

                int availableSeats = Math.Max(
                    0,
                    totalSeats - bookedSeats - heldSeats);

                result[trip.Id] = new TripSeatAvailability
                {
                    TotalSeats = totalSeats,
                    BookedSeats = bookedSeats,
                    HeldSeats = heldSeats,
                    AvailableSeats = availableSeats
                };
            }

            return result;
        }

        // =====================================================
        // US-165 + US-166: LẤY DANH SÁCH CHUYẾN
        // (phân trang + lọc + sắp xếp + trạng thái + đếm ngược)
        // GET: /api/TraCuuTuyenXe/danh-sach
        // =====================================================
        public async Task<TripListResponseDto> GetTripListAsync(
            TripListRequestDto request)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            if (pageSize > 100)
                pageSize = 100;

            var now = DateTime.Now;
            var nowUtc = DateTime.UtcNow;

            // 1. Lấy những chuyến đang hoạt động.
            // Nếu tìm theo ngày, lấy thêm chuyến bắt đầu từ ngày trước
            // để hỗ trợ các chuyến chạy qua nửa đêm.
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
                    t.BusId != null);

            if (request.DepartureDate.HasValue)
            {
                var date = request.DepartureDate.Value.Date;
                var previousDate = date.AddDays(-1);

                query = query.Where(t =>
                    t.TripDate.Date == date ||
                    t.TripDate.Date == previousDate);
            }
            else
            {
                // Giữ cả chuyến bắt đầu từ hôm qua nhưng vẫn
                // đang chạy và có điểm đón trong tương lai.
                var earliestDate = now.Date.AddDays(-1);

                query = query.Where(t =>
                    t.TripDate.Date >= earliestDate);
            }

            var trips = await query.ToListAsync();

            var seatAvailabilityByTrip =
                await GetSeatAvailabilityByTripAsync(
                    trips,
                    nowUtc);

            // 3. Ghép thông tin chuyến với điểm đón/trả,
            // giờ thực tế theo trạm và số ghế còn trống.
            var origin = request.OriginKeyword?.Trim();
            var destination = request.DestinationKeyword?.Trim();

            var filtered = trips
                .Select(t =>
                {
                    var schedules = t.Schedules
                        .Where(s => s.BusStop != null)
                        .OrderBy(s => s.StopOrder)
                        .ToList();

                    if (schedules.Count < 2)
                        return null;

                    var start = string.IsNullOrWhiteSpace(origin)
                        ? schedules.FirstOrDefault()
                        : schedules.FirstOrDefault(s =>
                            s.BusStop!.Name.Contains(
                                origin,
                                StringComparison.CurrentCultureIgnoreCase));

                    var end = string.IsNullOrWhiteSpace(destination)
                        ? schedules.LastOrDefault()
                        : schedules.FirstOrDefault(s =>
                            s.BusStop!.Name.Contains(
                                destination,
                                StringComparison.CurrentCultureIgnoreCase));

                    if (start == null || end == null ||
                        start.StopOrder >= end.StopOrder)
                    {
                        return null;
                    }

                    // Giờ tại trạm được quy đổi sang ngày giờ cụ thể.
                    // Trạm có giờ nhỏ hơn giờ xuất phát gốc được hiểu
                    // là trạm thuộc phần chạy sau nửa đêm.
                    DateTime ToScheduleDateTime(TimeSpan time)
                    {
                        var value = t.TripDate.Date + time;

                        if (time < t.DepartureTime)
                            value = value.AddDays(1);

                        return value;
                    }

                    var departureDateTime =
                        ToScheduleDateTime(start.DepartureTime);

                    var arrivalDateTime =
                        ToScheduleDateTime(end.ArrivalTime);

                    while (arrivalDateTime < departureDateTime)
                    {
                        arrivalDateTime = arrivalDateTime.AddDays(1);
                    }

                    // Không hiển thị chuyến đã rời điểm đón.
                    if (departureDateTime <= now)
                        return null;

                    // Nếu người dùng chọn ngày, đối chiếu ngày tại
                    // điểm đón, không chỉ ngày bắt đầu của cả chuyến.
                    if (request.DepartureDate.HasValue &&
                        departureDateTime.Date !=
                        request.DepartureDate.Value.Date)
                    {
                        return null;
                    }

                    if (request.MinDepartureTime.HasValue &&
                        departureDateTime.TimeOfDay <
                        request.MinDepartureTime.Value)
                    {
                        return null;
                    }

                    var fare = t.Route!.Fare;

                    if (request.MinFare.HasValue &&
                        fare < request.MinFare.Value)
                    {
                        return null;
                    }

                    if (request.MaxFare.HasValue &&
                        fare > request.MaxFare.Value)
                    {
                        return null;
                    }


                    var seatAvailability =
                        seatAvailabilityByTrip.TryGetValue(
                            t.Id,
                            out var calculatedSeats)
                            ? calculatedSeats
                            : null;

                    var totalSeats =
                        seatAvailability?.TotalSeats ?? 0;

                    var availableSeats =
                        seatAvailability?.AvailableSeats ?? 0;


                    if (request.OnlyAvailable == true &&
                        availableSeats <= 0)
                    {
                        return null;
                    }

                    var minutesUntilDeparture = Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (departureDateTime - now).TotalMinutes));

                    var status = availableSeats <= 0
                        ? "SOLD_OUT"
                        : minutesUntilDeparture <= 30
                            ? "DEPARTING_SOON"
                            : "ACTIVE";

                    var routeStops = t.Route.RouteStops
                        .OrderBy(rs => rs.StopOrder)
                        .ToList();

                    var routeStart = routeStops.FirstOrDefault(
                        rs => rs.StopId == start.StopId);

                    var routeEnd = routeStops.FirstOrDefault(
                        rs => rs.StopId == end.StopId);

                    var distanceKm = 0.0;

                    if (routeStart != null &&
                        routeEnd != null &&
                        routeEnd.DistanceFromStartKm >=
                        routeStart.DistanceFromStartKm)
                    {
                        distanceKm =
                            routeEnd.DistanceFromStartKm -
                            routeStart.DistanceFromStartKm;
                    }

                    var durationMinutes = (int)Math.Max(
                        0,
                        (arrivalDateTime - departureDateTime)
                            .TotalMinutes);

                    var stopCount = schedules.Count(s =>
                        s.StopOrder > start.StopOrder &&
                        s.StopOrder < end.StopOrder);

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
                        Fare = fare,

                        TotalSeats = totalSeats,
                        AvailableSeats = availableSeats,

                        BusCode = t.Bus?.BusCode ?? string.Empty,
                        LicensePlate = t.Bus?.LicensePlate ?? string.Empty,

                        Status = status,
                        MinutesUntilDeparture =
                            minutesUntilDeparture,
                        StopCount = stopCount,

                        OriginAddress =
                            start.BusStop.Address ?? string.Empty,
                        DestinationAddress =
                            end.BusStop.Address ?? string.Empty
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            // 4. Sắp xếp kết quả.
            var sortBy = request.SortBy?.ToLower() ?? "time";

            var isDesc = string.Equals(
                request.SortOrder,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            filtered = sortBy switch
            {
                "price" => isDesc
                    ? filtered.OrderByDescending(x => x.Fare).ToList()
                    : filtered.OrderBy(x => x.Fare).ToList(),

                "duration" => isDesc
                    ? filtered.OrderByDescending(
                        x => x.DurationMinutes).ToList()
                    : filtered.OrderBy(
                        x => x.DurationMinutes).ToList(),

                _ => isDesc
                    ? filtered.OrderByDescending(
                        x => x.DepartureDateTime).ToList()
                    : filtered.OrderBy(
                        x => x.DepartureDateTime).ToList()
            };

            // 5. Phân trang.
            var totalCount = filtered.Count;

            var totalPages = (int)Math.Ceiling(
                (double)totalCount / pageSize);

            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Trả kết quả.
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
            var now = DateTime.UtcNow;

            var report = new DataIntegrityReportDto
            {
                CheckedAt = now
            };

            var trips = await _context.Trips
                .AsNoTracking()
                .Include(t => t.Bus)
                .Where(t => t.IsActive)
                .ToListAsync();

            report.TotalTripsChecked = trips.Count;

            // Sử dụng chung quy tắc tính ghế với API tìm chuyến.
            // Ticket VALID/USED và SeatHold CONFIRMED được hợp nhất
            // theo mã ghế duy nhất để tránh đếm trùng.
            var seatAvailabilityByTrip =
                await GetSeatAvailabilityByTripAsync(trips, now);

            foreach (var trip in trips)
            {
                var seatInfo = seatAvailabilityByTrip[trip.Id];

                // 1. Kiểm tra tổng số ghế.
                // Dùng trực tiếp cùng một nguồn số ghế với API tìm chuyến
                // và API tỷ lệ lấp đầy để tránh lệch quy tắc dự phòng.
                var expectedTotalSeats = seatInfo.TotalSeats;

                if (trip.TotalSeats != expectedTotalSeats)
                {
                    report.Summary.SeatCountMismatch++;

                    report.Issues.Add(new DataIntegrityIssueDto
                    {
                        IssueType = "SEAT_COUNT_MISMATCH",
                        Severity = "HIGH",
                        TripId = trip.Id,
                        TripCode = trip.TripCode,
                        Description =
                            "Trip.TotalSeats khác số ghế kỳ vọng theo cấu hình xe",
                        ExpectedValue = expectedTotalSeats.ToString(),
                        ActualValue = trip.TotalSeats.ToString()
                    });
                }

                // 2. Kiểm tra số ghế đã đặt.
                // Ghế xuất hiện đồng thời trong Tickets và SeatHolds
                // chỉ được tính một lần theo mã ghế.
                var actualBooked = seatInfo.BookedSeats;

                if (trip.BookedSeats != actualBooked)
                {
                    report.Summary.BookedSeatsMismatch++;

                    report.Issues.Add(new DataIntegrityIssueDto
                    {
                        IssueType = "BOOKED_SEATS_MISMATCH",
                        Severity = "MEDIUM",
                        TripId = trip.Id,
                        TripCode = trip.TripCode,
                        Description =
                            "Trip.BookedSeats khác số ghế đã đặt thực tế " +
                            "(Ticket VALID/USED và SeatHold CONFIRMED, đã loại trùng)",
                        ExpectedValue = actualBooked.ToString(),
                        ActualValue = trip.BookedSeats.ToString()
                    });
                }

                // 3. Kiểm tra số ghế còn trống.
                // Dùng cùng kết quả tính toán với API tìm chuyến.
                var expectedAvailable = seatInfo.AvailableSeats;

                if (trip.AvailableSeats != expectedAvailable)
                {
                    report.Summary.AvailableSeatsMismatch++;

                    report.Issues.Add(new DataIntegrityIssueDto
                    {
                        IssueType = "AVAILABLE_SEATS_MISMATCH",
                        Severity = "MEDIUM",
                        TripId = trip.Id,
                        TripCode = trip.TripCode,
                        Description =
                            "Trip.AvailableSeats khác số ghế còn trống " +
                            "tính từ mã ghế duy nhất đã đặt và đang giữ",
                        ExpectedValue = expectedAvailable.ToString(),
                        ActualValue = trip.AvailableSeats.ToString()
                    });
                }
            }

            // 4. Kiểm tra các ghế giữ đã hết hạn nhưng chưa được giải phóng.
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
                    Description =
                        "Ghế đang giữ (HELD) đã quá hạn nhưng chưa được release",
                    ExpectedValue = "RELEASED hoặc EXPIRED",
                    ActualValue =
                        $"{hold.Status} (hết hạn lúc {hold.ExpiresAt:u})"
                });
            }

            // 5. Tổng hợp báo cáo.
            report.TotalIssuesFound = report.Issues.Count;

            return report;
        }
    }
}