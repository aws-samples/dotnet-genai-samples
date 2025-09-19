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
            async (string date, IHotelService hotelService, ILambdaContext context, string guests = "2", string location = "") =>
            {
                context.Logger.LogLine($"Getting available rooms for date: {date}, guests: '{guests}', location: {location}");

                if (!DateTime.TryParse(date, out var bookingDate))
                {
                    return "Error: Invalid date format. Please use YYYY-MM-DD format.";
                }

                // Enhanced guest count validation with debugging and default value
                if (string.IsNullOrWhiteSpace(guests))
                {
                    context.Logger.LogLine("Guest count is null or empty, using default value of 2");
                    guests = "2";
                }

                guests = guests.Trim(); // Remove any whitespace
                context.Logger.LogLine($"Trimmed guests value: '{guests}'");
                
                if (!int.TryParse(guests, out var guestCount))
                {
                    context.Logger.LogLine($"Failed to parse guest count: '{guests}', using default value of 2");
                    guestCount = 2;
                }
                
                context.Logger.LogLine($"Parsed guest count: {guestCount}");
                
                if (guestCount <= 0 || guestCount > 8)
                {
                    context.Logger.LogLine($"Guest count out of range: {guestCount}, using default value of 2");
                    guestCount = 2;
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
            async (string roomType, string checkInDate, string checkOutDate, string guestName, string location, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Booking {roomType} room from {checkInDate} to {checkOutDate} for guest {guestName} in {location}");

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

                // Use default guest count of 2
                var guestCount = 2;
                context.Logger.LogLine($"Using default guest count: {guestCount}");

                if (string.IsNullOrWhiteSpace(guestName))
                {
                    context.Logger.LogLine("Guest name is missing for booking");
                    return "Error: Guest name is required for the reservation.";
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    context.Logger.LogLine("Location is missing for booking");
                    return "Error: Hotel location is required. Please specify one of these locations: Chicago, San Francisco, or London.";
                }
                
                if (string.IsNullOrWhiteSpace(roomType))
                {
                    context.Logger.LogLine("Room type is missing for booking");
                    return "Error: Room type is required for the reservation.";
                }
                
                // Validate location
                var validLocations = new[] { "Chicago", "San Francisco", "London" };
                if (!validLocations.Any(loc => string.Equals(loc, location, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Logger.LogLine($"Invalid location provided: {location}");
                    return $"Error: Invalid location '{location}'. Please choose from: Chicago, San Francisco, or London.";
                }

                try
                {
                    context.Logger.LogLine($"Attempting to book room: {roomType}, {checkIn:yyyy-MM-dd} to {checkOut:yyyy-MM-dd}, {guestCount} guests, {guestName}, {location}");
                    
                    var nights = (checkOut - checkIn).Days;
                    context.Logger.LogLine($"Calculated nights: {nights}");
                    
                    if (nights <= 0)
                    {
                        context.Logger.LogLine($"Invalid nights calculation: {nights}");
                        return "Error: Check-out date must be after check-in date.";
                    }
                    
                    var booking = await hotelService.BookRoomAsync(roomType, checkIn, checkOut, guestCount, guestName, location);
                    context.Logger.LogLine($"Booking successful: {booking.ConfirmationNumber}");
                    
                    return $"Perfect! I have booked a {booking.RoomType.ToLower()} room for {guestName} from {checkInDate} to {checkOutDate} ({nights} night(s)) for {guestCount} guest(s) at Octank Hotels {location}. Your confirmation number is {booking.ConfirmationNumber}. The total cost is ${booking.TotalPrice}. Please let me know if you need anything else!";
                }
                catch (ArgumentException ex)
                {
                    context.Logger.LogLine($"ArgumentException during booking: {ex.Message}");
                    context.Logger.LogLine($"ArgumentException stack trace: {ex.StackTrace}");
                    return $"Error: {ex.Message}";
                }
                catch (Exception ex)
                {
                    context.Logger.LogLine($"Unexpected exception during booking: {ex.GetType().Name}: {ex.Message}");
                    context.Logger.LogLine($"Exception stack trace: {ex.StackTrace}");
                    return $"Error: An unexpected error occurred while booking the room. Please try again.";
                }
            });
    }

    public async Task<BedrockFunctionResponse> FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
    {
        try
        {
            context.Logger.LogLine($"Processing Hotel Deals request for function: {input.Function}");
            context.Logger.LogLine($"Input parameters: {System.Text.Json.JsonSerializer.Serialize(input.Parameters)}");
            context.Logger.LogLine($"Full input: {System.Text.Json.JsonSerializer.Serialize(input)}");
            
            var result = await _resolver.ResolveAsync(input, context);
            context.Logger.LogLine($"Function execution completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception in FunctionHandler: {ex.GetType().Name}: {ex.Message}");
            context.Logger.LogLine($"Exception stack trace: {ex.StackTrace}");
            
            // Return a simple error response since we can't construct the complex response object
            throw new Exception($"Function execution failed: {ex.Message}");
        }
    }
}