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

        _resolver.Tool("GetAvailableRooms", "Gets available hotel rooms for a specific date, number of guests, and hotel location. Always ask for the hotel location (Chicago, San Francisco, or London).",
            async (string date, string guests, string location, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Getting available rooms for date: {date}, guests: {guests}, location: {location}");

                if (!DateTime.TryParse(date, out var bookingDate))
                {
                    return "Error: Invalid date format. Please use YYYY-MM-DD format.";
                }

                if (!int.TryParse(guests, out var guestCount) || guestCount <= 0 || guestCount > 8)
                {
                    return "Error: Guest count must be a number between 1 and 8 guests.";
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    return "Error: Hotel location is required. Please specify one of these locations: Chicago, San Francisco, or London.";
                }

                var rooms = await hotelService.GetAvailableRoomsAsync(bookingDate, guestCount, location);
                
                if (!rooms.Any())
                {
                    return $"No rooms are currently available for the selected date and guest count in {location}.";
                }

                var roomList = string.Join(" — ", rooms.Select(r => 
                    $"{r.RoomType} (${r.Price}/night, sleeps {r.MaxGuests}): {r.Description}"));

                return $"Here are the available rooms on {date} for {guestCount} guest(s) in {location}: — {roomList}";
            });

        _resolver.Tool("BookHotelRoom", "Books, reserves, or makes a reservation for a hotel room for specific dates, room type, number of guests, guest name, and hotel location. Always ask for the hotel location (Chicago, San Francisco, or London).",
            async (string roomType, string checkInDate, string checkOutDate, string guests, string guestName, string location, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Booking {roomType} room from {checkInDate} to {checkOutDate} for {guests} guests under name {guestName} in {location}");

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

                if (!int.TryParse(guests, out var guestCount) || guestCount <= 0 || guestCount > 8)
                {
                    return "Error: Guest count must be a number between 1 and 8 guests.";
                }

                if (string.IsNullOrWhiteSpace(guestName))
                {
                    return "Error: Guest name is required for the reservation.";
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    return "Error: Hotel location is required. Please specify one of these locations: Chicago, San Francisco, or London.";
                }

                try
                {
                    var nights = (checkOut - checkIn).Days;
                    var booking = await hotelService.BookRoomAsync(roomType, checkIn, checkOut, guestCount, guestName, location);
                    return $"Perfect! I have booked a {booking.RoomType.ToLower()} room for {guestName} from {checkInDate} to {checkOutDate} ({nights} night(s)) for {guestCount} guest(s) at Octank Hotels {location}. Your confirmation number is {booking.ConfirmationNumber}. The total cost is ${booking.TotalPrice}. Please let me know if you need anything else!";
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