using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using HotelDealsLambda;

// For local debugging
public class Program
{
    public static async Task Main(string[] args)
    {
        var function = new Function();
        var context = new TestLambdaContext();

        // Test GetAvailableRooms
        var testRequest = new BedrockFunctionRequest
        {
            Function = "GetAvailableRooms",
            Parameters = new List<AWS.Lambda.Powertools.EventHandler.Resolvers.Parameter>
            {
                new() { Name = "date", Value = "2024-12-25" }
            }
        };

        Console.WriteLine("Testing GetAvailableRooms...");
        var response = await function.FunctionHandler(testRequest, context);
        Console.WriteLine($"Response: {response.Response}");

        // Test BookHotelRoom
        var bookingRequest = new BedrockFunctionRequest
        {
            Function = "BookHotelRoom",
            Parameters = new List<AWS.Lambda.Powertools.EventHandler.Resolvers.Parameter>
            {
                new() { Name = "roomNumber", Value = "101" },
                new() { Name = "date", Value = "2024-12-25" },
                new() { Name = "price", Value = "100" }
            }
        };

        Console.WriteLine("\nTesting BookHotelRoom...");
        var bookingResponse = await function.FunctionHandler(bookingRequest, context);
        Console.WriteLine($"Response: {bookingResponse.Response}");
    }
}

public class TestLambdaContext : ILambdaContext
{
    public string RequestId => "test-request-id";
    public string AwsRequestId => "test-request-id";
    public string FunctionName => "HotelDealsFunction";
    public string FunctionVersion => "1.0";
    public string InvokedFunctionArn => "arn:aws:lambda:us-east-1:123456789012:function:HotelDealsFunction";
    public int MemoryLimitInMB => 512;
    public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);
    public ILambdaLogger Logger => new TestLogger();
    public string LogGroupName => "/aws/lambda/HotelDealsFunction";
    public string LogStreamName => "test-stream";
    public ICognitoIdentity Identity => null!;
    public IClientContext ClientContext => null!;
}

public class TestLogger : ILambdaLogger
{
    public void Log(string message) => Console.WriteLine(message);
    public void LogLine(string message) => Console.WriteLine(message);
}