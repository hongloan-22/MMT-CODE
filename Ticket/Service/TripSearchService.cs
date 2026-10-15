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
    }
}