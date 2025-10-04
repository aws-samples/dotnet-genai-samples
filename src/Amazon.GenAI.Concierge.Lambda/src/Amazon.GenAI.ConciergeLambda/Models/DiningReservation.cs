namespace Amazon.GenAI.ConciergeLambda.Models;

public class DiningReservation
{
    public required string ReservationId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string ReservationDateTime { get; set; } = string.Empty;
    public int NumberOfGuests { get; set; }
    public MealType MealType { get; set; }
    public string RestaurantName { get; set; } = "Octank Dine";
    public ReservationStatus Status { get; set; }
}
