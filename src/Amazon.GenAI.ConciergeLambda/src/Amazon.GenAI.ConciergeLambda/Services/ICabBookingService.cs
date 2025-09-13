using Amazon.GenAI.ConciergeLambda.Models;

namespace Amazon.GenAI.ConciergeLambda.Services;

public interface ICabBookingService
{
    Task<Guid> CreateCabBookingAsync(string guestName, DateTime bookingFromDate, Seater seater, DateTime? bookingTillDate = null);
}