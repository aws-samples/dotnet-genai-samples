using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using HotelDealsLambda.Services;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HotelDealsLambda;

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

        _resolver.Tool("GetAvailableRooms", "Gets available hotel rooms for a specific date",
            async (string date, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Getting available rooms for date: {date}");

                if (!DateTime.TryParse(date, out var bookingDate))
                {
                    return "Error: Invalid date format. Please use YYYY-MM-DD format.";
                }

                var rooms = await hotelService.GetAvailableRoomsAsync(bookingDate);
                
                if (!rooms.Any())
                {
                    return "No rooms are currently available for the selected date.";
                }

                var roomList = string.Join(" — ", rooms.Select(r => 
                    $"Room {r.RoomNumber} ({r.RoomType}, ${r.Price}): {r.Description}"));

                return $"Here are the available rooms on {date}: — {roomList}";
            });

        _resolver.Tool("BookHotelRoom", "Books a hotel room for a specific date and price",
            async (string roomNumber, string date, decimal price, IHotelService hotelService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Booking room {roomNumber} for date: {date} at price: ${price}");

                if (!DateTime.TryParse(date, out var bookingDate))
                {
                    return "Error: Invalid date format. Please use YYYY-MM-DD format.";
                }

                try
                {
                    var booking = await hotelService.BookRoomAsync(roomNumber, bookingDate, price);
                    return $"I have booked room {booking.RoomNumber} for you on {date}. This is a {booking.RoomType.ToLower()} room with {booking.Description.ToLower()} for ${booking.Price}. Please let me know if you need anything else!";
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