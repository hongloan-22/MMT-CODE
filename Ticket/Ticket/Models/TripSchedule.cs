using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    public class TripSchedule
    {
        [Key]
        public int Id { get; set; }

        // Chuyến xe
        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }


        // Điểm dừng
        public int StopId { get; set; }

        [ForeignKey(nameof(StopId))]
        public BusStop? BusStop { get; set; }


        // Thứ tự điểm dừng
        public int StopOrder { get; set; }


        // Thời gian đến
        public TimeSpan ArrivalTime { get; set; }


        // Thời gian rời trạm
        public TimeSpan DepartureTime { get; set; }


        // Trạng thái
        public string Status { get; set; } = "SCHEDULED";
    }
}