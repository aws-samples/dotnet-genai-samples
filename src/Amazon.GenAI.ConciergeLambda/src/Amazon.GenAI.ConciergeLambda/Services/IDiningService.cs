using Amazon.GenAI.ConciergeLambda.Models;

namespace Amazon.GenAI.ConciergeLambda.Services;

public interface IDiningService
{
    Task<Guid> CreateDiningReservationAsync(string guestName, DateTime reservationDateTime, int numberOfGuests, MealType? mealType = null);
}
