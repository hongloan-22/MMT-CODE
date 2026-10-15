using System.ComponentModel.DataAnnotations;

namespace Ticket.Models
{
    public class Bus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string BusCode { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        public int SeatCount { get; set; } = 40;

        public bool IsActive { get; set; } = true;

        public ICollection<BusSeat> Seats { get; set; }
            = new List<BusSeat>();

        public ICollection<Trip> Trips { get; set; }
            = new List<Trip>();

        public ICollection<BusStaffAssignment> StaffAssignments { get; set; }
            = new List<BusStaffAssignment>();
    }
}