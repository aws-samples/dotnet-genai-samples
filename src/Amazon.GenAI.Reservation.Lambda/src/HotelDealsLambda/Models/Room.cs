namespace HotelDealsLambda.Models;

public class Room
{
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
}

public class Booking
{
    public string BookingId { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime BookingDate { get; set; }
    public string Description { get; set; } = string.Empty;
}