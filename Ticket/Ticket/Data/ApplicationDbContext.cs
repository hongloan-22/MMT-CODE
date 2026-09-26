using Microsoft.EntityFrameworkCore;
using Ticket.Models;

namespace TuyenXeAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<BusRoute> BusRoutes { get; set; }

        public DbSet<BusStop> BusStops { get; set; }

        public DbSet<RouteStop> RouteStops { get; set; }

        public DbSet<Trip> Trips { get; set; }

        public DbSet<TripSchedule> TripSchedules { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // BUS ROUTE
            // =========================

            modelBuilder.Entity<BusRoute>(entity =>
            {
                entity.ToTable("BusRoutes");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.RouteName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.TotalDistanceKm)
                    .HasPrecision(10, 2);

                entity.HasMany(x => x.Trips)
                    .WithOne(x => x.Route)
                    .HasForeignKey(x => x.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // =========================
            // BUS STOP
            // =========================

            modelBuilder.Entity<BusStop>(entity =>
            {
                entity.ToTable("BusStops");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Address)
                    .HasMaxLength(255);
            });


            // =========================
            // ROUTE STOP
            // =========================

            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.ToTable("RouteStops");

                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Route)
                    .WithMany(x => x.RouteStops)
                    .HasForeignKey(x => x.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.BusStop)
                    .WithMany(x => x.RouteStops)
                    .HasForeignKey(x => x.StopId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.RouteId,
                    x.StopOrder
                })
                .IsUnique();
            });


            // =========================
            // TRIP
            // =========================

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("Trips");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.TripCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasOne(x => x.Route)
                    .WithMany(x => x.Trips)
                    .HasForeignKey(x => x.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.TripCode)
                    .IsUnique();
            });


            // =========================
            // TRIP SCHEDULE
            // =========================

            modelBuilder.Entity<TripSchedule>(entity =>
            {
                entity.ToTable("TripSchedules");

                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Trip)
                    .WithMany(x => x.Schedules)
                    .HasForeignKey(x => x.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.BusStop)
                    .WithMany()
                    .HasForeignKey(x => x.StopId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Status)
                    .HasMaxLength(30);

                // Một trạm chỉ xuất hiện một lần
                // trong cùng một chuyến
                entity.HasIndex(x => new
                {
                    x.TripId,
                    x.StopId
                })
                .IsUnique();

                // Thứ tự trạm không được trùng
                // trong cùng một chuyến
                entity.HasIndex(x => new
                {
                    x.TripId,
                    x.StopOrder
                })
                .IsUnique();
            });
        }
    }
}