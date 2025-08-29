using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using HotelDealsLambda.Services;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HotelDealsLambda;

/// <summary>
/// Hotel Deals Lambda Function for Amazon Bedrock Agents
/// 
/// NOTE: This implementation uses in-memory data for demonstration purposes.
/// In a production environment, you would integrate with:
/// - Hotel reservation systems (e.g., Opera PMS, Amadeus, Sabre)
/// - Property Management Systems (PMS)
/// - Channel managers for real-time availability
/// - Payment processing systems
/// - Customer relationship management (CRM) systems
/// </summary>
public class Function
{
    private readonly BedrockAgentFunctionResolver _resolver;

    public Function()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHotelService, HotelService>();
        services.AddBedrockResolver();

        var serviceProvider = services.BuildServiceProvider();
        _resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

        RegisterTools();
    }

    private void RegisterTools()
    {
        _resolver.Tool("GetHotelSpecialDeals", "Gets current special deals and offers available for hotels",
            async (IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine("Retrieving current hotel special deals");
                return await hotelService.GetSpecialDealsAsync();
            });

        _resolver.Tool("GetAvailableRooms", "Gets available hotel rooms for a specific date and guest count",
            async (string date, int guests, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Getting available rooms for date: {date}, guests: {guests}");

                if (!DateTime.TryParse(date, out var bookingDate))
                {
                    return "Error: Invalid date format. Please use YYYY-MM-DD format.";
                }

                if (guests <= 0 || guests > 8)
                {
                    return "Error: Guest count must be between 1 and 8 guests.";
                }

                var rooms = await hotelService.GetAvailableRoomsAsync(bookingDate, guests);
                
                if (!rooms.Any())
                {
                    return "No rooms are currently available for the selected date and guest count.";
                }

                var roomList = string.Join(" — ", rooms.Select(r => 
                    $"{r.RoomType} (${r.Price}/night, sleeps {r.MaxGuests}): {r.Description}"));

                return $"Here are the available rooms on {date} for {guests} guest(s): — {roomList}";
            });

        _resolver.Tool("BookHotelRoom", "Books a hotel room for specific dates, room type, and guest count",
            async (string roomType, string checkInDate, string checkOutDate, int guests, string guestName, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Booking {roomType} room from {checkInDate} to {checkOutDate} for {guests} guests under name {guestName}");

                if (!DateTime.TryParse(checkInDate, out var checkIn))
                {
                    return "Error: Invalid check-in date format. Please use YYYY-MM-DD format.";
                }

                if (!DateTime.TryParse(checkOutDate, out var checkOut))
                {
                    return "Error: Invalid check-out date format. Please use YYYY-MM-DD format.";
                }

                if (checkOut <= checkIn)
                {
                    return "Error: Check-out date must be after check-in date.";
                }

                if (guests <= 0 || guests > 8)
                {
                    return "Error: Guest count must be between 1 and 8 guests.";
                }

                if (string.IsNullOrWhiteSpace(guestName))
                {
                    return "Error: Guest name is required for the reservation.";
                }

                try
                {
                    var nights = (checkOut - checkIn).Days;
                    var booking = await hotelService.BookRoomAsync(roomType, checkIn, checkOut, guests, guestName);
                    return $"Perfect! I have booked a {booking.RoomType.ToLower()} room for {guestName} from {checkInDate} to {checkOutDate} ({nights} night(s)) for {guests} guest(s). Your confirmation number is {booking.ConfirmationNumber}. The total cost is ${booking.TotalPrice}. Please let me know if you need anything else!";
                }
                catch (ArgumentException ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
    }

    public async Task<BedrockFunctionResponse> FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
    {
        context.Logger.LogLine($"Processing Hotel Deals request for function: {input.Function}");
        return await _resolver.ResolveAsync(input, context);
    }
}