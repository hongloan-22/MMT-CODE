using Microsoft.EntityFrameworkCore;
using Ticket.Data;

namespace SmartBusTicketing.Services
{
    public class SeatHoldExpirationService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SeatHoldExpirationService> _logger;

        public SeatHoldExpirationService(
            IServiceScopeFactory scopeFactory,
            ILogger<SeatHoldExpirationService> logger)
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
                    using var scope =
                        _scopeFactory.CreateScope();

                    var db =
                        scope.ServiceProvider
                            .GetRequiredService<ApplicationDbContext>();

                    var now =
                        DateTime.UtcNow;

                    var expiredHolds =
                        await db.SeatHolds
                            .Where(x =>
                                x.Status == "HELD" &&
                                x.ExpiresAt <= now)
                            .ToListAsync(
                                stoppingToken);

                    if (expiredHolds.Count > 0)
                    {
                        foreach (var hold in expiredHolds)
                        {
                            hold.Status =
                                "EXPIRED";
                        }

                        var bookingIds =
                            expiredHolds
                                .Select(x =>
                                    x.BookingId)
                                .Distinct()
                                .ToList();

                        var bookings =
                            await db.Bookings
                                .Where(x =>
                                    bookingIds.Contains(
                                        x.Id) &&
                                    x.Status == "HELD")
                                .ToListAsync(
                                    stoppingToken);

                        foreach (var booking in bookings)
                        {
                            booking.Status =
                                "EXPIRED";
                        }

                        await db.SaveChangesAsync(
                            stoppingToken);

                        _logger.LogInformation(
                            "Đã giải phóng {Count} ghế hết hạn.",
                            expiredHolds.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Lỗi khi xử lý SeatHold hết hạn.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(30),
                        stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}