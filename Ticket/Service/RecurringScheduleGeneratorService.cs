using Microsoft.EntityFrameworkCore;
using Ticket.Data;
using Ticket.Models;

namespace SmartBusTicketing.Services
{
    public interface IRecurringScheduleGeneratorService
    {
        Task GenerateUpcomingTripsAsync(
            int horizonDays = 30,
            CancellationToken cancellationToken = default);
    }

    public class RecurringScheduleGeneratorService
        : BackgroundService,
          IRecurringScheduleGeneratorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringScheduleGeneratorService> _logger;

        public RecurringScheduleGeneratorService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecurringScheduleGeneratorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateUpcomingTripsAsync(
                        30,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Lỗi khi tự sinh chuyến từ lịch cố định.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromHours(6),
                        stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public async Task GenerateUpcomingTripsAsync(
            int horizonDays = 30,
            CancellationToken cancellationToken = default)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var db =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var today =
                DateTime.Today;

            var horizonEnd =
                today.AddDays(horizonDays);

            var schedules =
                await db.RecurringSchedules
                    .Include(x => x.Route)
                        .ThenInclude(x => x!.RouteStops)
                    .Include(x => x.Bus)
                        .ThenInclude(x => x!.Seats)
                    .Where(x =>
                        x.IsActive &&
                        x.Route != null &&
                        x.Route.IsActive &&
                        x.Bus != null &&
                        x.Bus.IsActive)
                    .ToListAsync(
                        cancellationToken);

            foreach (var schedule in schedules)
            {
                var startDate =
                    schedule.StartDate.Date > today
                        ? schedule.StartDate.Date
                        : today;

                var endDate =
                    schedule.EndDate.HasValue
                        ? schedule.EndDate.Value.Date < horizonEnd
                            ? schedule.EndDate.Value.Date
                            : horizonEnd
                        : horizonEnd;

                if (startDate > endDate)
                    continue;

                for (
                    var date = startDate;
                    date <= endDate;
                    date = date.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!ShouldRunOnDate(
                            schedule,
                            date))
                    {
                        continue;
                    }

                    var existingTrip =
                        await db.Trips
                            .FirstOrDefaultAsync(
                                x =>
                                    x.RecurringScheduleId ==
                                        schedule.Id &&
                                    x.TripDate >= date &&
                                    x.TripDate < date.AddDays(1),
                                cancellationToken);

                    var shouldRun =
                        ShouldRunOnDate(
                            schedule,
                            date);

                    if (existingTrip != null)
                    {
                        if (shouldRun)
                        {
                            if (!existingTrip.IsActive)
                            {
                                existingTrip.IsActive = true;
                                existingTrip.Status = "ACTIVE";
                            }
                        }
                        else if (existingTrip.IsActive)
                        {
                            existingTrip.IsActive = false;
                            existingTrip.Status = "CANCELLED";
                        }

                        continue;
                    }

                    if (!shouldRun)
                        continue;

                    var conflict =
                        await db.Trips.AnyAsync(
                            x =>
                                x.RouteId ==
                                    schedule.RouteId &&
                                x.BusId ==
                                    schedule.BusId &&
                                x.TripDate >= date &&
                                x.TripDate <
                                    date.AddDays(1) &&
                                x.DepartureTime ==
                                    schedule.DepartureTime &&
                                x.IsActive,
                            cancellationToken);

                    if (conflict)
                        continue;

                    await GenerateTripAsync(
                        db,
                        schedule,
                        date,
                        cancellationToken);
                }
            }
        }

        private static bool ShouldRunOnDate(
            RecurringSchedule schedule,
            DateTime date)
        {
            var type =
                schedule.ScheduleType
                    .Trim()
                    .ToUpperInvariant();

            if (type == "DAILY")
                return true;

            if (type != "WEEKLY")
                return false;

            if (string.IsNullOrWhiteSpace(
                    schedule.DaysOfWeek))
            {
                return false;
            }

            var values =
                schedule.DaysOfWeek
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                        int.TryParse(
                            x.Trim(),
                            out var value)
                            ? value
                            : -1)
                    .ToHashSet();

            return values.Contains(
                (int)date.DayOfWeek);
        }

        private static async Task GenerateTripAsync(
            ApplicationDbContext db,
            RecurringSchedule schedule,
            DateTime date,
            CancellationToken cancellationToken)
        {
            var routeStops =
                schedule.Route!
                    .RouteStops
                    .OrderBy(x => x.StopOrder)
                    .ToList();

            if (routeStops.Count == 0)
                return;

            var activeSeatCount =
                schedule.Bus!.Seats.Count(x => x.IsActive);

            if (activeSeatCount <= 0)
                return;

            var maxTravelMinutes =
                routeStops.Max(
                    x =>
                        x.TravelTimeFromStartMinutes);

            var arrivalTime =
                schedule.DepartureTime.Add(
                    TimeSpan.FromMinutes(
                        Math.Max(
                            1,
                            maxTravelMinutes)));

            var tripCode =
                $"SCH{schedule.Id:D4}-" +
                $"{date:yyyyMMdd}-" +
                $"{schedule.DepartureTime:hhmm}";

            var trip =
                new Trip
                {
                    RouteId =
                        schedule.RouteId,

                    BusId =
                        schedule.BusId,

                    RecurringScheduleId =
                        schedule.Id,

                    TripCode =
                        tripCode,

                    TripDate =
                        date,

                    DepartureTime =
                        schedule.DepartureTime,

                    ArrivalTime =
                        arrivalTime,

                    Status =
                        "ACTIVE",

                    TotalSeats =
                        activeSeatCount,

                    AvailableSeats =
                        activeSeatCount,

                    BookedSeats =
                        0,

                    IsActive =
                        true
                };

            db.Trips.Add(trip);

            await db.SaveChangesAsync(
                cancellationToken);

            var tripSchedules =
                new List<TripSchedule>();

            for (
                int index = 0;
                index < routeStops.Count;
                index++)
            {
                var routeStop =
                    routeStops[index];

                var arrival =
                    schedule.DepartureTime.Add(
                        TimeSpan.FromMinutes(
                            routeStop
                                .TravelTimeFromStartMinutes));

                var isLast =
                    index ==
                    routeStops.Count - 1;

                var departure =
                    isLast
                        ? arrival
                        : arrival.Add(
                            TimeSpan.FromMinutes(5));

                tripSchedules.Add(
                    new TripSchedule
                    {
                        TripId =
                            trip.Id,

                        StopId =
                            routeStop.StopId,

                        StopOrder =
                            routeStop.StopOrder,

                        ArrivalTime =
                            arrival,

                        DepartureTime =
                            departure,

                        Status =
                            "SCHEDULED"
                    });
            }

            db.TripSchedules.AddRange(
                tripSchedules);

            await db.SaveChangesAsync(
                cancellationToken);
        }
    }
}