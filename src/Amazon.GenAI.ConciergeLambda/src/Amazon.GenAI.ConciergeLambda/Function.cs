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
        services.AddSingleton<IConciergeService, ConciergeService>();
        
        services.AddBedrockResolver();

        var serviceProvider = services.BuildServiceProvider();
        _resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

        // Register all tools using inline registration
        RegisterTools();
    }

    private void RegisterTools()
    {
        _resolver.Tool("CreateCabBooking", "Creates a new CAB booking for a guest",
            async (string guestName, string bookingFromDate, string seater, string? bookingTillDate, IConciergeService conciergeService, ILambdaContext context) =>
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

                var bookingId = await conciergeService.CreateCabBookingAsync(guestName, fromDate, seaterEnum, tillDate);
                
                var seaterDisplay = seaterEnum switch
                {
                    Seater.TwoSeater => "2-Seater",
                    Seater.FourSeater => "4-Seater",
                    Seater.SevenSeater => "7-Seater",
                    Seater.NineSeater => "9-Seater",
                    _ => seaterEnum.ToString()
                };

                return $"CAB booking created successfully!\n" +
                       $"Booking ID: {bookingId}\n" +
                       $"Guest Name: {guestName}\n" +
                       $"From Date: {fromDate:yyyy/MM/dd HH:mm:ss}\n" +
                       $"Till Date: {(tillDate ?? fromDate):yyyy/MM/dd}\n" +
                       $"Seater Type: {seaterDisplay}";
            });

            //TODO: Dining Reservation - Real API call to create a dining reservation
            
            //TODO: Maintenance Request - Real API call to create a maintenance request
    }

    public async Task<BedrockFunctionResponse> FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
    {
        context.Logger.LogLine($"Processing Concierge Agent request for function: {input.Function}");
        context.Logger.LogLine($"Full request: {System.Text.Json.JsonSerializer.Serialize(input)}");
        return await _resolver.ResolveAsync(input, context);
    }
}
