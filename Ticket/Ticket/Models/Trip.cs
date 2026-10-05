using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Trip
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        public int TripId
        {
            get => Id;
            set => Id = value;
        }

        public int RouteId { get; set; }

        [ForeignKey(nameof(RouteId))]
        public BusRoute? Route { get; set; }

        // ==========================================
        // LIÊN KẾT XE (BUS) - US-52
        // ==========================================
        public int? BusId { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }

        [Required]
        [StringLength(50)]
        public string TripCode { get; set; } = string.Empty;

        public DateTime TripDate { get; set; }

        public TimeSpan DepartureTime { get; set; }

        public TimeSpan? ArrivalTime { get; set; }

        public string Status { get; set; } = "SCHEDULED";

        public int TotalSeats { get; set; } = 40;

        public int AvailableSeats { get; set; } = 40;

        public int BookedSeats { get; set; } = 0;

        public decimal Price { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public string? Note { get; set; }

        public int? OriginStationId { get; set; }
        public Station? OriginStation { get; set; }

        public int? DestinationStationId { get; set; }
        public Station? DestinationStation { get; set; }

        public ICollection<TripSchedule> Schedules { get; set; }
            = new List<TripSchedule>();

        // ==========================================
        // DANH SÁCH GIỮ CHỖ CỦA CHUYẾN - US-58
        // ==========================================
        public ICollection<SeatHold> SeatHolds { get; set; }
            = new List<SeatHold>();
    }

    public class Station
    {
        [Key]
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
    }

    public class Route
    {
        [Key]
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
    }
}