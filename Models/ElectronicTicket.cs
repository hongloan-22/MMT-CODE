using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticket.Models
{
    // Bảng Cơ sở dữ liệu Vé điện tử (Mỹ-70)
    public class ElectronicTicket
    {
        [Key]
        public int Id { get; set; }

        // Mã vé điện tử duy nhất
        [Required]
        [StringLength(50)]
        public string TicketCode { get; set; } = string.Empty;

        // Chuỗi dữ liệu mã QR gắn liền với vé điện tử
        [Required]
        public string QrCodeData { get; set; } = string.Empty;

        // Liên kết với giao dịch thanh toán
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        // Liên kết với Chuyến xe / Lịch trình
        public int BusScheduleId { get; set; }

        // ID Người mua vé (User.cs)
        public string UserId { get; set; } = string.Empty;

        // Số tiền giao dịch
        [Column(TypeName = "decimal(18,2)")]
        public decimal TransactionAmount { get; set; }

        // Thời gian thực hiện giao dịch
        public DateTime TransactionTime { get; set; } = DateTime.Now;

        // Trạng thái giao dịch/vé: Paid (Đã thanh toán), Used (Đã soát vé)
        [StringLength(20)]
        public string TransactionStatus { get; set; } = "Paid";

        // Trạng thái nhận diện / quét mã QR
        public bool IsQrScanned { get; set; } = false;

        public DateTime? QrScannedAt { get; set; }

        // Navigation Properties
        [ForeignKey("BusScheduleId")]
        public virtual BusSchedule? BusSchedule { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}