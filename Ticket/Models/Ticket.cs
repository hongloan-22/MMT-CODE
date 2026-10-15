using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        [Required]
        public int RouteId { get; set; }

        [ForeignKey(nameof(RouteId))]
        public BusRoute? Route { get; set; }

        [Required]
        public int DepartureStopId { get; set; }

        [ForeignKey(nameof(DepartureStopId))]
        public BusStop? DepartureStop { get; set; }

        [Required]
        public int ArrivalStopId { get; set; }

        [ForeignKey(nameof(ArrivalStopId))]
        public BusStop? ArrivalStop { get; set; }

        [Required]
        [StringLength(50)]
        public string TicketCode { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string SeatCode { get; set; } = string.Empty;
        public decimal Fare { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "VALID";

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }

        public TicketQR? QR { get; set; }
    }
}