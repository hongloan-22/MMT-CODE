using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ticket.Models;
using TicketEntity = Ticket.Models.Ticket;

namespace Ticket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // ACCOUNT
        // =========================
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        // =========================
        // ROUTE - STOP
        // =========================
        public DbSet<BusRoute> BusRoutes { get; set; } = null!;
        public DbSet<BusStop> BusStops { get; set; } = null!;
        public DbSet<RouteStop> RouteStops { get; set; } = null!;

        // =========================
        // TRIP / SCHEDULE
        // =========================
        public DbSet<Trip> Trips { get; set; } = null!;
        public DbSet<TripSchedule> TripSchedules { get; set; } = null!;
        public DbSet<Bus> Buses { get; set; } = null!;
        public DbSet<BusSeat> BusSeats { get; set; } = null!;
        public DbSet<BusStaffAssignment> BusStaffAssignments { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<SeatHold> SeatHolds { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<RecurringSchedule> RecurringSchedules { get; set; } = null!;
        public DbSet<TicketEntity> Tickets { get; set; } = null!;
        public DbSet<TicketQR> TicketQRs { get; set; } = null!;
        public DbSet<RefundTransaction> RefundTransactions { get; set; } = null!;
        public DbSet<TicketChangeHistory> TicketChangeHistories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // ROLE
            // =====================================================

            modelBuilder.Entity<Role>()
                .HasIndex(x => x.RoleCode)
                .IsUnique();

            // =====================================================
            // USER
            // =====================================================

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // AUDIT LOG
            // =====================================================

            modelBuilder.Entity<AuditLog>()
                .HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // BUS ROUTE
            // =====================================================

            modelBuilder.Entity<BusRoute>()
                .HasIndex(x => x.RouteCode)
                .IsUnique();

            // =====================================================
            // BUS STOP
            // =====================================================

            modelBuilder.Entity<BusStop>()
                .HasIndex(x => x.StopCode)
                .IsUnique();

            // =====================================================
            // ROUTE STOP
            // =====================================================

            modelBuilder.Entity<RouteStop>()
                .HasOne(x => x.Route)
                .WithMany(x => x.RouteStops)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasOne(x => x.BusStop)
                .WithMany(x => x.RouteStops)
                .HasForeignKey(x => x.StopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Không được trùng cùng trạm trong một tuyến
            modelBuilder.Entity<RouteStop>()
                .HasIndex(x => new
                {
                    x.RouteId,
                    x.StopId
                })
                .IsUnique();

            // Không được trùng thứ tự trong một tuyến
            modelBuilder.Entity<RouteStop>()
                .HasIndex(x => new
                {
                    x.RouteId,
                    x.StopOrder
                })
                .IsUnique();

            // =====================================================
            // BUS
            // =====================================================

            modelBuilder.Entity<Bus>()
                .HasIndex(x => x.BusCode)
                .IsUnique();

            modelBuilder.Entity<Bus>()
                .HasIndex(x => x.LicensePlate)
                .IsUnique();

            // =====================================================
            // BUS SEAT
            // =====================================================

            modelBuilder.Entity<BusSeat>()
                .HasOne(x => x.Bus)
                .WithMany(x => x.Seats)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusSeat>()
                .HasIndex(x => new
                {
                    x.BusId,
                    x.SeatCode
                })
                .IsUnique();

            // =====================================================
            // BUS STAFF ASSIGNMENT
            // =====================================================

            modelBuilder.Entity<BusStaffAssignment>()
                .HasOne(x => x.Bus)
                .WithMany(x => x.StaffAssignments)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusStaffAssignment>()
                .HasOne(x => x.User)
                .WithMany(x => x.BusStaffAssignments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một nhân sự chỉ được phân công vào một xe đang hoạt động
            modelBuilder.Entity<BusStaffAssignment>()
                .HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            // Một xe chỉ có một tài xế đang hoạt động
            modelBuilder.Entity<BusStaffAssignment>()
                .HasIndex(x => new
                {
                    x.BusId,
                    x.AssignmentType
                })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [AssignmentType] = 'DRIVER'");

            // =====================================================
            // BOOKING
            // =====================================================

            modelBuilder.Entity<Booking>()
                .HasIndex(x => x.BookingCode)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Trip)
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // SEAT HOLD
            // =====================================================

            modelBuilder.Entity<SeatHold>()
                .HasOne(x => x.Booking)
                .WithMany(x => x.SeatHolds)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SeatHold>()
                .HasOne(x => x.Trip)
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index để hỗ trợ kiểm tra ghế đang giữ
            modelBuilder.Entity<SeatHold>()
                .HasIndex(x => new
                {
                    x.TripId,
                    x.SeatCode
                });

            // Index theo trạng thái + thời hạn
            modelBuilder.Entity<SeatHold>()
                .HasIndex(x => new
                {
                    x.Status,
                    x.ExpiresAt
                });

            // =====================================================
            // PAYMENT TRANSACTION
            // =====================================================

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(x => x.TransactionCode)
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // TRIP -> BUS
            // =====================================================

            modelBuilder.Entity<Trip>()
                .HasOne(x => x.Bus)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // RECURRING SCHEDULE
            // =====================================================

            modelBuilder.Entity<RecurringSchedule>()
                .HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecurringSchedule>()
                .HasOne(x => x.Bus)
                .WithMany()
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecurringSchedule>()
                .HasIndex(x => new
                {
                    x.RouteId,
                    x.BusId,
                    x.ScheduleType,
                    x.DepartureTime,
                    x.StartDate
                });

            // =====================================================
            // TRIP -> RECURRING SCHEDULE
            // =====================================================

            modelBuilder.Entity<Trip>()
                .HasOne(x => x.RecurringSchedule)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.RecurringScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // TRIP / SCHEDULE
            // =====================================================

            modelBuilder.Entity<Trip>()
                .HasIndex(x => x.TripCode)
                .IsUnique();

            modelBuilder.Entity<Trip>()
                .HasOne(x => x.Route)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripSchedule>()
                .HasOne(x => x.Trip)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TripSchedule>()
                .HasOne(x => x.BusStop)
                .WithMany()
                .HasForeignKey(x => x.StopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Không trùng thứ tự trạm trong cùng chuyến
            modelBuilder.Entity<TripSchedule>()
                .HasIndex(x => new
                {
                    x.TripId,
                    x.StopOrder
                })
                .IsUnique();

            // Không trùng trạm trong cùng chuyến
            modelBuilder.Entity<TripSchedule>()
                .HasIndex(x => new
                {
                    x.TripId,
                    x.StopId
                })
                .IsUnique();


            // =====================================================
            // TICKET
            // =====================================================

            modelBuilder.Entity<TicketEntity>()
                .HasIndex(x => x.TicketCode)
                .IsUnique();

            modelBuilder.Entity<TicketEntity>()
                .HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketEntity>()
                .HasOne(x => x.Trip)
                .WithMany()
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketEntity>()
                .HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketEntity>()
                .HasOne(x => x.DepartureStop)
                .WithMany()
                .HasForeignKey(x => x.DepartureStopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketEntity>()
                .HasOne(x => x.ArrivalStop)
                .WithMany()
                .HasForeignKey(x => x.ArrivalStopId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // TICKET QR
            // =====================================================

            modelBuilder.Entity<TicketQR>()
                .HasIndex(x => x.QRToken)
                .IsUnique();

            modelBuilder.Entity<TicketQR>()
                .HasOne(x => x.Ticket)
                .WithOne(x => x.QR)
                .HasForeignKey<TicketQR>(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // REFUND TRANSACTION
            // =====================================================

            modelBuilder.Entity<RefundTransaction>()
                .HasOne(x => x.Ticket)
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefundTransaction>()
                .HasOne(x => x.PaymentTransaction)
                .WithMany()
                .HasForeignKey(x => x.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // TICKET CHANGE HISTORY
            // =====================================================



            modelBuilder.Entity<TicketChangeHistory>()
                .HasOne(x => x.Ticket)
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // SQLite không có kiểu DECIMAL/TIME như SQL Server.
            // Lưu tiền bằng integer cent để giữ chính xác 2 chữ số thập phân;
            // lưu TimeSpan bằng ticks để so sánh/sắp xếp giờ ổn định.
            var decimalToCents = new ValueConverter<decimal, long>(
                value => decimal.ToInt64(decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero)),
                value => value / 100m);

            var nullableDecimalToCents = new ValueConverter<decimal?, long?>(
                value => value.HasValue
                    ? decimal.ToInt64(decimal.Round(value.Value * 100m, 0, MidpointRounding.AwayFromZero))
                    : null,
                value => value.HasValue ? value.Value / 100m : null);

            var timeSpanToTicks = new ValueConverter<TimeSpan, long>(
                value => value.Ticks,
                value => TimeSpan.FromTicks(value));

            var nullableTimeSpanToTicks = new ValueConverter<TimeSpan?, long?>(
                value => value.HasValue ? value.Value.Ticks : null,
                value => value.HasValue ? TimeSpan.FromTicks(value.Value) : null);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(entity => entity.GetProperties()))
            {
                if (property.ClrType == typeof(decimal))
                    property.SetValueConverter(decimalToCents);
                else if (property.ClrType == typeof(decimal?))
                    property.SetValueConverter(nullableDecimalToCents);
                else if (property.ClrType == typeof(TimeSpan))
                    property.SetValueConverter(timeSpanToTicks);
                else if (property.ClrType == typeof(TimeSpan?))
                    property.SetValueConverter(nullableTimeSpanToTicks);
            }
        }

    }
}
