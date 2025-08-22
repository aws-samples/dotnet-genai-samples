using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.GenAI.ConciergeLambda.Models;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;

namespace Amazon.GenAI.ConciergeLambda.Services;

public class ConciergeService : IConciergeService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _tableName;
    private readonly string _snsTopicArn;

    public ConciergeService(IAmazonDynamoDB dynamoDbClient, IAmazonSimpleNotificationService snsClient)
    {
        _dynamoDbClient = dynamoDbClient;
        _snsClient = snsClient;
        _tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME") ?? "CabBookings";
        _snsTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN") ?? throw new InvalidOperationException("SNS_TOPIC_ARN environment variable is required");
    }

    public async Task<Guid> CreateCabBookingAsync(string guestName, DateTime bookingFromDate, Seater seater, DateTime? bookingTillDate = null)
    {
        var bookingId = Guid.NewGuid();
        var currentDateTime = DateTime.UtcNow;
        var tillDate = bookingTillDate ?? bookingFromDate;

        var booking = new CabBooking
        {
            BookingId = bookingId,
            BookingDate = currentDateTime.ToString("yyyy/MM/dd"),
            Status = BookingStatus.Submitted,
            GuestName = guestName,
            BookingFromDate = bookingFromDate.ToString("yyyy/MM/dd HH:mm:ss"),
            BookingTillDate = tillDate.ToString("yyyy/MM/dd HH:mm:ss"),
            Seater = seater
        };

        // Insert into DynamoDB
        var putRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["BookingId"] = new AttributeValue { S = booking.BookingId.ToString() },
                ["BookingDate"] = new AttributeValue { S = booking.BookingDate },
                ["Status"] = new AttributeValue { S = booking.Status.ToString() },
                ["GuestName"] = new AttributeValue { S = booking.GuestName },
                ["BookingFromDate"] = new AttributeValue { S = booking.BookingFromDate },
                ["BookingTillDate"] = new AttributeValue { S = booking.BookingTillDate },
                ["Seater"] = new AttributeValue { S = booking.Seater.ToString() }
            }
        };

        await _dynamoDbClient.PutItemAsync(putRequest);

        // Send SNS notification
        var snsRequest = new PublishRequest
        {
            TopicArn = _snsTopicArn,
            Message = JsonSerializer.Serialize(booking)
        };

        await _snsClient.PublishAsync(snsRequest);

        return bookingId;
    }
}
