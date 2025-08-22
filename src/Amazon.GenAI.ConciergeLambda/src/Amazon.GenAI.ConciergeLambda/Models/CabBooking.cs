namespace Amazon.GenAI.ConciergeLambda.Models;

public class CabBooking
{
    public Guid BookingId { get; set; }
    public string BookingDate { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string BookingFromDate { get; set; } = string.Empty;
    public string BookingTillDate { get; set; } = string.Empty;
    public Seater Seater { get; set; }
}
