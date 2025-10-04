using Amazon;
using Amazon.DynamoDBv2;
using Amazon.GenAI.ConciergeLambda.Models;
using Amazon.GenAI.ConciergeLambda.Services;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleNotificationService;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Amazon.GenAI.ConciergeLambda;

public class Function
{
    private readonly BedrockAgentFunctionResolver _resolver;

    public Function()
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Get configuration from environment variables
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? RegionEndpoint.USEast1.SystemName;
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        
        // Register AWS services with configuration
        services.AddSingleton<IAmazonDynamoDB>(provider => new AmazonDynamoDBClient(regionEndpoint));
        services.AddSingleton<IAmazonSimpleNotificationService>(provider => new AmazonSimpleNotificationServiceClient(regionEndpoint));
        
        // Register application services
        services.AddSingleton<ICabBookingService, CabBookingService>();
        services.AddSingleton<IDiningService, DiningService>();
        services.AddSingleton<IMaintenanceService, MaintenanceService>();
        
        services.AddBedrockResolver();

        var serviceProvider = services.BuildServiceProvider();
        _resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

        // Register all tools using inline registration
        RegisterTools();
    }

    private void RegisterTools()
    {
        _resolver.Tool("CreateCabBooking", "Creates a new CAB booking for a guest",
            async (string guestName, string bookingFromDate, string seater, string? bookingTillDate, ICabBookingService cabBookingService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Creating CAB booking for guest: {guestName}");

                if (!DateTime.TryParse(bookingFromDate, out var fromDate))
                {
                    return "Error: Invalid booking from date format";
                }

                if (!Enum.TryParse<Seater>(seater, true, out var seaterEnum))
                {
                    return "Error: Invalid seater type. Valid options are: TwoSeater, FourSeater, SevenSeater, NineSeater";
                }

                DateTime? tillDate = null;
                if (!string.IsNullOrEmpty(bookingTillDate) && DateTime.TryParse(bookingTillDate, out var parsedTillDate))
                {
                    tillDate = parsedTillDate;
                }

                var bookingId = await cabBookingService.CreateCabBookingAsync(guestName, fromDate, seaterEnum, tillDate);
                
                var seaterDisplay = seaterEnum switch
                {
                    Seater.TwoSeater => "2-Seater",
                    Seater.FourSeater => "4-Seater",
                    Seater.SevenSeater => "7-Seater",
                    Seater.NineSeater => "9-Seater",
                    _ => seaterEnum.ToString()
                };

                return $"Perfect! I have booked a {seaterDisplay} CAB starting from {fromDate} to {(tillDate ?? fromDate)} for {guestName}. Your confirmation number is {bookingId}. Please let me know if you need anything else!";
            });

        _resolver.Tool("CreateDiningReservation", "Creates a new dining reservation at Octank Dine",
            async (string guestName, string reservationDateTime, int numberOfGuests, string? mealType, IDiningService diningService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Creating dining reservation for guest: {guestName}");

                if (!DateTime.TryParse(reservationDateTime, out var dateTime))
                {
                    return "Error: Invalid reservation date/time format";
                }

                MealType? mealTypeEnum = null;
                if (!string.IsNullOrEmpty(mealType))
                {
                    if (!Enum.TryParse<MealType>(mealType, true, out var parsedMealType))
                    {
                        return "Error: Invalid meal type. Valid options are: Lunch, Dinner";
                    }
                    mealTypeEnum = parsedMealType;
                }

                try
                {
                    var reservationId = await diningService.CreateDiningReservationAsync(guestName, dateTime, numberOfGuests, mealTypeEnum);
                    
                    var finalMealType = mealTypeEnum?.ToString() ?? (dateTime.Hour >= 12 && dateTime.Hour < 16 ? "Lunch" : "Dinner");

                    return $"Perfect! I have reserved a table for {guestName} with {numberOfGuests} guests on {dateTime}. Enjoy your {finalMealType}. Your reservation number is {reservationId}. Please let me know if you need anything else!";
                }
                catch (ArgumentException ex)
                {
                    return $"Error: {ex.Message}";
                }
            });

        _resolver.Tool("CreateMaintenanceRequest", "Creates a new room maintenance request",
            async (string guestName, string roomNumber, string issueType, string issueDescription, string? priority, IMaintenanceService maintenanceService, ILambdaContext context) =>
            {
                context.Logger.LogLine($"Creating maintenance request for guest: {guestName}");

                if (!Enum.TryParse<IssueType>(issueType, true, out var issueTypeEnum))
                {
                    return "Error: Invalid issue type. Valid options are: Plumbing, Electrical, AirConditioning, Cleaning, Furniture, Other";
                }

                Priority? priorityEnum = null;
                if (!string.IsNullOrEmpty(priority))
                {
                    if (!Enum.TryParse<Priority>(priority, true, out var parsedPriority))
                    {
                        return "Error: Invalid priority. Valid options are: Low, Medium, High, Urgent";
                    }
                    priorityEnum = parsedPriority;
                }

                try
                {
                    var requestId = await maintenanceService.CreateMaintenanceRequestAsync(guestName, roomNumber, issueTypeEnum, issueDescription, priorityEnum);
                    
                    var finalPriority = priorityEnum?.ToString() ?? GetDefaultPriorityDisplay(issueTypeEnum);

                    return $"Perfect! I have create maintenance request for {guestName} in room number {roomNumber} having {issueTypeEnum} with {finalPriority}. We will address your issue at earliest. You can note your maintenace request number: {requestId}. Please let me know if you need anything else!";
                }
                catch (ArgumentException ex)
                {
                    return $"Error: {ex.Message}";
                }
            });

            //TODO: Maintenance Request - Real API call to create a maintenance request
    }

    private static string GetDefaultPriorityDisplay(IssueType issueType)
    {
        return issueType switch
        {
            IssueType.Plumbing or IssueType.Electrical => "High",
            IssueType.AirConditioning => "Medium",
            IssueType.Cleaning or IssueType.Furniture or IssueType.Other => "Low",
            _ => "Low"
        };
    }

    public async Task<BedrockFunctionResponse> FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
    {
        context.Logger.LogLine($"Processing Concierge Agent request for function: {input.Function}");
        context.Logger.LogLine($"Full request: {System.Text.Json.JsonSerializer.Serialize(input)}");
        return await _resolver.ResolveAsync(input, context);
    }
}
