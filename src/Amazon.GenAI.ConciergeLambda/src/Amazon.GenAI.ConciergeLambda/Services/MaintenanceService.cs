using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.GenAI.ConciergeLambda.Models;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Amazon.GenAI.ConciergeLambda.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _tableName;
    private readonly string _snsTopicArn;

    public MaintenanceService(IAmazonDynamoDB dynamoDbClient, IAmazonSimpleNotificationService snsClient)
    {
        _dynamoDbClient = dynamoDbClient;
        _snsClient = snsClient;
        _tableName = Environment.GetEnvironmentVariable("MAINTENANCE_REQUEST") ?? "MaintenanceRequests";
        _snsTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN") ?? throw new InvalidOperationException("SNS_TOPIC_ARN environment variable is required");
    }

    public async Task<Guid> CreateMaintenanceRequestAsync(string guestName, string roomNumber, IssueType issueType, string issueDescription, Priority? priority = null)
    {
        // Validate room number (exactly 3 digits, 100-999)
        if (!Regex.IsMatch(roomNumber, @"^[1-9]\d{2}$"))
        {
            throw new ArgumentException("Room number must be exactly 3 digits (100-999)");
        }

        // Validate issue description (minimum 10 characters)
        if (string.IsNullOrWhiteSpace(issueDescription) || issueDescription.Length < 10)
        {
            throw new ArgumentException("Issue description must be at least 10 characters long");
        }

        // Auto-assign priority if not provided
        var finalPriority = priority ?? GetDefaultPriority(issueType);
        var requestId = Guid.NewGuid();
        var requestDate = DateTime.UtcNow;

        var request = new MaintenanceRequest
        {
            RequestId = requestId,
            GuestName = guestName,
            RoomNumber = roomNumber,
            IssueType = issueType,
            IssueDescription = issueDescription,
            Priority = finalPriority,
            RequestDate = requestDate,
            Status = RequestStatus.Submitted
        };

        // Insert into DynamoDB
        var putRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["RequestId"] = new AttributeValue { S = request.RequestId.ToString() },
                ["GuestName"] = new AttributeValue { S = request.GuestName },
                ["RoomNumber"] = new AttributeValue { S = request.RoomNumber },
                ["IssueType"] = new AttributeValue { S = request.IssueType.ToString() },
                ["IssueDescription"] = new AttributeValue { S = request.IssueDescription },
                ["Priority"] = new AttributeValue { S = request.Priority.ToString() },
                ["RequestDate"] = new AttributeValue { S = request.RequestDate.ToString("yyyy/MM/dd HH:mm:ss") },
                ["Status"] = new AttributeValue { S = request.Status.ToString() }
            }
        };

        await _dynamoDbClient.PutItemAsync(putRequest);

        // Send SNS notification
        var snsRequest = new PublishRequest
        {
            TopicArn = _snsTopicArn,
            Message = JsonSerializer.Serialize(request)
        };

        await _snsClient.PublishAsync(snsRequest);

        return requestId;
    }

    private static Priority GetDefaultPriority(IssueType issueType)
    {
        return issueType switch
        {
            IssueType.Plumbing or IssueType.Electrical => Priority.High,
            IssueType.AirConditioning => Priority.Medium,
            IssueType.Cleaning or IssueType.Furniture or IssueType.Other => Priority.Low,
            _ => Priority.Low
        };
    }
}
