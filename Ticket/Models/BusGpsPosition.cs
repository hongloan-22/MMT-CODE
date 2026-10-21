using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models;

public class BusGpsPosition
{
    [Key] public int Id { get; set; }
    [Required] public int TripId { get; set; }
    [ForeignKey(nameof(TripId))] public Trip? Trip { get; set; }
    [Range(-90, 90)] public double Latitude { get; set; }
    [Range(-180, 180)] public double Longitude { get; set; }
    public double? SpeedKmh { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
