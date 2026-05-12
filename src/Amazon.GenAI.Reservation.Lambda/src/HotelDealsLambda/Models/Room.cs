namespace HotelReservationLambda.Models;

public class Room
{
    public string RoomType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public bool IsAvailable { get; set; } = true;
    public List<string> Amenities { get; set; } = new();
    public string Location { get; set; } = string.Empty;
}

public class Booking
{
    public string BookingId { get; set; } = string.Empty;
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Guests { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}