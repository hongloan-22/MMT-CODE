public enum TicketStatus { Pending, Booked, Cancelled, Completed }
public enum RefundStatus { Pending, Completed, Failed }

public class RefundTransaction
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public RefundStatus Status { get; set; }
}