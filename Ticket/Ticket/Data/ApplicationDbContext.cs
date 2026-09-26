using Microsoft.EntityFrameworkCore;
using Ticket.Models;

namespace Ticket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // CÁC BẢNG TUYẾN XE
        // =========================

        public DbSet<BusRoute> BusRoutes { get; set; }

        public DbSet<BusStop> BusStops { get; set; }

        public DbSet<RouteStop> RouteStops { get; set; }


        // =========================
        // CÁC BẢNG CHUYẾN XE
        // =========================

        public DbSet<Trip> Trips { get; set; }

        public DbSet<TripSchedule> TripSchedules { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // ==========================================
            // BusRoute -> RouteStop
            // Một tuyến có nhiều điểm dừng
            // ==========================================

            modelBuilder.Entity<RouteStop>()
                .HasOne(x => x.Route)
                .WithMany(x => x.RouteStops)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // BusStop -> RouteStop
            // Một điểm dừng thuộc nhiều tuyến
            // ==========================================

            modelBuilder.Entity<RouteStop>()
                .HasOne(x => x.BusStop)
                .WithMany(x => x.RouteStops)
                .HasForeignKey(x => x.StopId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // BusRoute -> Trip
            // Một tuyến có nhiều chuyến
            // ==========================================

            modelBuilder.Entity<Trip>()
                .HasOne(x => x.Route)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Trip -> TripSchedule
            // Một chuyến có nhiều lịch trình
            // ==========================================

            modelBuilder.Entity<TripSchedule>()
                .HasOne(x => x.Trip)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // BusStop -> TripSchedule
            // Một điểm dừng có thể xuất hiện
            // trong nhiều lịch trình
            // ==========================================

            modelBuilder.Entity<TripSchedule>()
                .HasOne(x => x.BusStop)
                .WithMany()
                .HasForeignKey(x => x.StopId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Không cho trùng thứ tự trạm trong một chuyến
            // ==========================================

            modelBuilder.Entity<TripSchedule>()
                .HasIndex(x => new
                {
                    x.TripId,
                    x.StopOrder
                })
                .IsUnique();


            // ==========================================
            // Không cho cùng một trạm xuất hiện 2 lần
            // trong cùng một chuyến
            // ==========================================

            modelBuilder.Entity<TripSchedule>()
                .HasIndex(x => new
                {
                    x.TripId,
                    x.StopId
                })
                .IsUnique();


            // ==========================================
            // Mã chuyến không được trùng
            // ==========================================

            modelBuilder.Entity<Trip>()
                .HasIndex(x => x.TripCode)
                .IsUnique();
        }
    }
}