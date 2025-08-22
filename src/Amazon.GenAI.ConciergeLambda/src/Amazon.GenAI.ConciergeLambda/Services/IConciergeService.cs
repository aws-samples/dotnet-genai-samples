using Amazon.GenAI.ConciergeLambda.Models;

namespace Amazon.GenAI.ConciergeLambda.Services;

public interface IConciergeService
{
    Task<Guid> CreateCabBookingAsync(string guestName, DateTime bookingFromDate, Seater seater, DateTime? bookingTillDate = null);
}
