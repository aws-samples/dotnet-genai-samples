using System.Text.Json;
using Amazon.BedrockAgentRuntime;
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using HotelReservationLambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HotelReservationLambda;

public class Function
{
    private readonly IHotelService _hotelService = new HotelService();

    public async Task<object> FunctionHandler(JsonElement input, ILambdaContext context)
    {
        // AgentCore Gateway sends tool name in ClientContext.Custom
        var toolName = GetToolName(context);
        context.Logger.LogLine($"Tool: {toolName}, Input: {input}");

        try
        {
            var result = toolName switch
            {
                "GetHotelSpecialDeals" => await _hotelService.GetSpecialDealsAsync(),
                "GetAvailableRooms" => await HandleGetAvailableRooms(input),
                "BookHotelRoom" => await HandleBookHotelRoom(input, context),
                "SearchHotelKnowledgeBase" => await HandleKBQuery(input, context),
                _ => $"Error: Unknown tool '{toolName}'"
            };

            context.Logger.LogLine($"Result: {result}");
            return new { content = new[] { new { type = "text", text = result } } };
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error: {ex.Message}");
            return new { content = new[] { new { type = "text", text = $"Error: {ex.Message}" } }, isError = true };
        }
    }

    private string GetToolName(ILambdaContext context)
    {
        const string delimiter = "___";
        try
        {
            var custom = context.ClientContext?.Custom;
            if (custom != null && custom.ContainsKey("bedrockAgentCoreToolName"))
            {
                var fullName = custom["bedrockAgentCoreToolName"];
                var idx = fullName.IndexOf(delimiter, StringComparison.Ordinal);
                return idx >= 0 ? fullName[(idx + delimiter.Length)..] : fullName;
            }
        }
        catch { }
        return "Unknown";
    }

    private string GetString(JsonElement input, string name, string? defaultValue = null)
    {
        if (input.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString()!;
        return defaultValue ?? "";
    }

    private int GetInt(JsonElement input, string name, int defaultValue = 0)
    {
        if (input.TryGetProperty(name, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number) return prop.GetInt32();
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var v)) return v;
        }
        return defaultValue;
    }

    private async Task<string> HandleGetAvailableRooms(JsonElement input)
    {
        var location = GetString(input, "location");
        var date = GetString(input, "date");
        var guests = GetInt(input, "guests", 2);

        if (string.IsNullOrWhiteSpace(location))
            return "Error: Hotel location is required. Please specify: Chicago, San Francisco, or London.";
        if (!DateTime.TryParse(date, out var bookingDate))
            return "Error: Invalid date format. Please use YYYY-MM-DD format.";
        if (guests <= 0 || guests > 8) guests = 2;

        var rooms = await _hotelService.GetAvailableRoomsAsync(bookingDate, guests, location);
        if (!rooms.Any())
            return $"No rooms available for {guests} guest(s) in {location} on {date}.";

        var roomList = string.Join(" — ", rooms.Select(r =>
            $"{r.RoomType} (${r.Price}/night, sleeps {r.MaxGuests}): {r.Description}"));
        return $"Available rooms on {date} for {guests} guest(s) in {location}: — {roomList}";
    }

    private async Task<string> HandleBookHotelRoom(JsonElement input, ILambdaContext context)
    {
        var location = GetString(input, "location");
        var checkInDate = GetString(input, "checkInDate");
        var checkOutDate = GetString(input, "checkOutDate");
        var roomType = GetString(input, "roomType");
        var guests = GetInt(input, "NumberOfGuests", 2);

        if (string.IsNullOrWhiteSpace(location))
            return "Error: Hotel location is required.";
        if (!DateTime.TryParse(checkInDate, out var checkIn))
            return "Error: Invalid check-in date. Use YYYY-MM-DD format.";
        if (!DateTime.TryParse(checkOutDate, out var checkOut))
            return "Error: Invalid check-out date. Use YYYY-MM-DD format.";
        if (checkOut <= checkIn)
            return "Error: Check-out date must be after check-in date.";
        if (string.IsNullOrWhiteSpace(roomType))
            return "Error: Room type is required.";
        if (guests <= 0) guests = 2;

        var validLocations = new[] { "Chicago", "San Francisco", "London" };
        if (!validLocations.Any(l => string.Equals(l, location, StringComparison.OrdinalIgnoreCase)))
            return $"Error: Invalid location '{location}'. Choose from: Chicago, San Francisco, or London.";

        var booking = await _hotelService.BookRoomAsync(roomType, checkIn, checkOut, guests, location);
        var nights = (checkOut - checkIn).Days;
        return $"Booked a {booking.RoomType.ToLower()} from {checkInDate} to {checkOutDate} ({nights} night(s)) for {guests} guest(s) at Octank Hotels {location}. Confirmation: {booking.ConfirmationNumber}. Total: ${booking.TotalPrice}.";
    }

    private async Task<string> HandleKBQuery(JsonElement input, ILambdaContext context)
    {
        var query = GetString(input, "query");
        if (string.IsNullOrWhiteSpace(query))
            return "Error: A search query is required.";

        var kbId = Environment.GetEnvironmentVariable("KB_ID");
        if (string.IsNullOrWhiteSpace(kbId))
            return "Error: Knowledge Base is not configured.";

        var client = new AmazonBedrockAgentRuntimeClient();
        var response = await client.RetrieveAsync(new RetrieveRequest
        {
            KnowledgeBaseId = kbId,
            RetrievalQuery = new KnowledgeBaseQuery { Text = query },
            RetrievalConfiguration = new KnowledgeBaseRetrievalConfiguration
            {
                VectorSearchConfiguration = new KnowledgeBaseVectorSearchConfiguration
                {
                    NumberOfResults = 5
                }
            }
        });

        if (!response.RetrievalResults.Any())
            return "No information found for that query.";

        return string.Join("\n\n", response.RetrievalResults.Select(r => r.Content.Text));
    }
}
