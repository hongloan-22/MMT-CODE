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
        // GET: /api/Statistics/trip/{tripId}/occupancy
        // =====================================================
        public async Task<TripOccupancyDto>
            GetTripOccupancyAsync(int tripId)
        {
            // 1. Lấy thông tin chuyến xe (kèm Bus để lấy SeatCount dự phòng)
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

            // 2. Xác định tổng số ghế
            int totalSeats =
                trip.TotalSeats > 0
                    ? trip.TotalSeats
                    : (trip.Bus?.SeatCount ?? 0);

            // 3. Đếm số ghế đã đặt (Vé VALID hoặc USED)
            int bookedSeats = await _context.Tickets
                .Where(t =>
                    t.TripId == tripId &&
                    (t.Status == "VALID" ||
                     t.Status == "USED"))
                .CountAsync();

            // 4. Đếm số ghế đang giữ
            // (HELD chưa hết hạn hoặc CONFIRMED)
            var now = DateTime.UtcNow;

            int heldSeats = await _context.SeatHolds
                .Where(sh =>
                    sh.TripId == tripId &&
                    (sh.Status == "CONFIRMED" ||
                    (sh.Status == "HELD" &&
                     sh.ExpiresAt > now)))
                .CountAsync();

            // 5. Tính số ghế còn trống
            int availableSeats =
                totalSeats - bookedSeats - heldSeats;

            if (availableSeats < 0)
                availableSeats = 0; // Tránh âm

            // 6. Tính tỷ lệ lấp đầy (%)
            double occupancyRate =
                totalSeats > 0
                    ? ((double)bookedSeats / totalSeats) * 100
                    : 0;

            occupancyRate =
                Math.Round(occupancyRate, 2);

            // 7. Trả về DTO
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
    }
}